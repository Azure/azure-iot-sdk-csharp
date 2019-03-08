using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpConnectionHolder : IAmqpConnectionHolder, IDisposable
    {
        private readonly DeviceIdentity DeviceIdentity;
        private readonly Action<IAmqpConnectionHolder, DeviceIdentity> OnConnectionIdle;
        private readonly IAmqpConnector Connector;
        private readonly SemaphoreSlim Lock;
        private readonly AmqpSessionSettings AmqpSessionSettings;
        private AmqpConnection AmqpConnection;
        private IDictionary<DeviceIdentity, IAmqpSessionHolder> AmqpSessionHolders;
        private AmqpAuthenticationRefresher AmqpAuthenticationRefresher;
        private AmqpCbsLink AmqpCbsLink;
        private bool IdleNotified;

        public AmqpConnectionHolder(DeviceIdentity deviceIdentity, Action<IAmqpConnectionHolder, DeviceIdentity> onConnectionIdle)
        {
            DeviceIdentity = deviceIdentity;
            OnConnectionIdle = onConnectionIdle;
            Connector = new AmqpConnector(deviceIdentity.AmqpTransportSettings, deviceIdentity.IotHubConnectionString.HostName);
            Lock = new SemaphoreSlim(1, 1);
            IdleNotified = false;
            AmqpSessionHolders = new ConcurrentDictionary<DeviceIdentity, IAmqpSessionHolder>();
            AmqpSessionSettings = new AmqpSessionSettings()
            {
                Properties = new Fields()
            };
            if (Logging.IsEnabled) Logging.Associate(this, DeviceIdentity, $"{nameof(DeviceIdentity)}");
        }

        public bool DisposeOnIdle()
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(DisposeOnIdle)}");
            bool result = false;
            AmqpConnection amqpConnection = null;
            AmqpAuthenticationRefresher amqpAuthenticationRefresher = null;
            AmqpCbsLink amqpCbsLink = null;
            Lock.Wait();
            if (IdleNotified && AmqpSessionHolders.Count == 0)
            {
                amqpCbsLink = AmqpCbsLink;
                amqpAuthenticationRefresher = AmqpAuthenticationRefresher;
                amqpConnection = AmqpConnection;
                AmqpCbsLink = null;
                AmqpAuthenticationRefresher = null;
                AmqpConnection = null;
                result = true;
            }
            IdleNotified = false;
            Lock.Release();
            amqpCbsLink?.Close();
            amqpAuthenticationRefresher?.StopLoop();
            amqpConnection?.SafeClose();
            return result;
        }

        
        private async Task<AmqpAuthenticationRefresher> StartAmqpAuthenticationRefresher(DeviceIdentity deviceIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(StartAmqpAuthenticationRefresher)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            await Lock.WaitAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            AmqpCbsLink = AmqpCbsLink ?? new AmqpCbsLink(AmqpConnection);
            Lock.Release();
            AmqpAuthenticationRefresher amqpAuthenticator = new AmqpAuthenticationRefresher(deviceIdentity, AmqpCbsLink);
            await amqpAuthenticator.InitLoopAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(StartAmqpAuthenticationRefresher)}");
            return amqpAuthenticator;
        }

        public IAmqpSessionHolder CreateAmqpSessionHolder(
            DeviceIdentity deviceIdentity, 
            Action onAmqpSessionClosed, 
            Func<MethodRequestInternal, Task> methodHandler, 
            Action<AmqpMessage> twinMessageListener, 
            Func<string, Message, Task> eventListener)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(CreateAmqpSessionHolder)}");
            Lock.Wait();
            AmqpSessionHolders.TryGetValue(deviceIdentity, out IAmqpSessionHolder amqpSessionHolder);
            if (amqpSessionHolder == null)
            {
                amqpSessionHolder = new AmqpSessionHolder(
                    deviceIdentity, 
                    onAmqpSessionClosed, 
                    OpenAmqpSession, 
                    StartAmqpAuthenticationRefresher,
                    RemoveDevice, 
                    methodHandler,
                    twinMessageListener, 
                    eventListener);
                AmqpSessionHolders.Add(deviceIdentity, amqpSessionHolder);
            }
            Lock.Release();
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(CreateAmqpSessionHolder)}");
            return amqpSessionHolder;
        }

        private async Task<AmqpSession> OpenAmqpSession(DeviceIdentity deviceIdentity, ILinkFactory linkFactory, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(OpenAmqpSession)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            AmqpConnection amqpConnection = await EnsureConnection(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            await Lock.WaitAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            AmqpSessionHolders.TryGetValue(deviceIdentity, out IAmqpSessionHolder amqpSessionHolder);
            Lock.Release();
            AmqpSession amqpSession = null;
            if (amqpSessionHolder != null)
            {
                amqpSession = new AmqpSession(amqpConnection, AmqpSessionSettings, linkFactory);
                amqpConnection.AddSession(amqpSession, new ushort?());
                await amqpSession.OpenAsync(timeout).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(OpenAmqpSession)}");
            return amqpSession;
        }

        public int GetNumberOfSessions()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(GetNumberOfSessions)}");
            Lock.Wait();
            int count = AmqpSessionHolders.Count;
            Lock.Release();
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(GetNumberOfSessions)}");
            return count;
        }
        private async Task<AmqpConnection> EnsureConnection(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnsureConnection)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            AmqpConnection amqpConnection = null;
            AmqpAuthenticationRefresher amqpAuthenticationRefresher = null;
            AmqpCbsLink amqpCbsLink = null;
            await Lock.WaitAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            if (AmqpConnection == null)
            {
                try
                {
                    // Create AmqpConnection
                    amqpConnection = await Connector.OpenConnectionAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

                    if (DeviceIdentity.AuthenticationModel != AuthenticationModel.X509)
                    {
                        amqpCbsLink = amqpCbsLink ?? new AmqpCbsLink(amqpConnection);
                        if (DeviceIdentity.AuthenticationModel == AuthenticationModel.SasGrouped)
                        {
                            // refresh
                            amqpAuthenticationRefresher = new AmqpAuthenticationRefresher(DeviceIdentity, amqpCbsLink);
                            await amqpAuthenticationRefresher.InitLoopAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                        }
                    }
                    AmqpConnection = amqpConnection;
                    AmqpCbsLink = amqpCbsLink;
                    AmqpAuthenticationRefresher = amqpAuthenticationRefresher;
                    AmqpConnection.Closed += OnConnectionClosed;
                    if (Logging.IsEnabled) Logging.Associate(this, AmqpConnection, $"{nameof(AmqpConnection)}");
                    if (Logging.IsEnabled) Logging.Associate(this, AmqpCbsLink, $"{nameof(AmqpCbsLink)}");
                }
                catch (Exception) // when (!ex.IsFatal())
                {
                    amqpCbsLink?.Close();
                    amqpAuthenticationRefresher?.StopLoop();
                    amqpConnection?.SafeClose();
                    throw;
                }
                finally
                {
                    Lock.Release();
                }
            }
            else
            {
                amqpConnection = AmqpConnection;
            }
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnsureConnection)}");
            return amqpConnection;
        }

        private void OnConnectionClosed(object o, EventArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, o, $"{nameof(OnConnectionClosed)}");
            ICollection<IAmqpSessionHolder> amqpSessionHolders = null;
            Lock.Wait();
            if (AmqpConnection != null && ReferenceEquals(AmqpConnection, o))
            {
                AmqpCbsLink?.Close();
                AmqpAuthenticationRefresher?.StopLoop();
                AmqpCbsLink = null;
                AmqpAuthenticationRefresher = null;
                AmqpConnection = null;
                amqpSessionHolders = AmqpSessionHolders.Values;
                AmqpSessionHolders.Clear();
                IdleNotified = false;
            }
            Lock.Release();
            foreach (IAmqpSessionHolder amqpSessionHolder in amqpSessionHolders)
            {
                amqpSessionHolder.OnConnectionClosed();
            }
            if (Logging.IsEnabled) Logging.Exit(this, o, $"{nameof(OnConnectionClosed)}");
        }

        private void RemoveDevice(DeviceIdentity deviceIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(RemoveDevice)}");
            bool idle = false;
            Lock.Wait();
            bool removed = AmqpSessionHolders.Remove(deviceIdentity);
            if (removed && !IdleNotified)
            {
                IdleNotified = AmqpSessionHolders.Count == 0 && IdleNotified;
                idle = IdleNotified;
            }
            Lock.Release();
            if (idle)
            {
                OnConnectionIdle(this, deviceIdentity);
            }
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(RemoveDevice)}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (Logging.IsEnabled) Logging.Info(this, disposing, $"{nameof(Dispose)}");
            if (disposing)
            {
                Lock?.Dispose();
                Connector?.Dispose();
                AmqpSessionHolders?.Clear();
                AmqpAuthenticationRefresher?.Dispose();
                AmqpSessionHolders = null;
            }
        }
    }
}
