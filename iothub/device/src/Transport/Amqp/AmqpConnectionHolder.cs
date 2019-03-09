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
        private AmqpConnection AmqpConnection;
        private IDictionary<DeviceIdentity, IAmqpDevice> AmqpDevices;
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
            AmqpDevices = new ConcurrentDictionary<DeviceIdentity, IAmqpDevice>();
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
            if (IdleNotified && AmqpDevices.Count == 0)
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
            amqpConnection?.Abort();
            return result;
        }

        
        private async Task<AmqpAuthenticationRefresher> AuthenticationRefresherCreator(DeviceIdentity deviceIdentity, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, timeout, $"{nameof(AuthenticationRefresherCreator)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            if (AmqpConnection == null)
            {
                throw new IotHubCommunicationException();
            }
            AmqpCbsLink = AmqpCbsLink ?? new AmqpCbsLink(AmqpConnection);
            
            AmqpAuthenticationRefresher amqpAuthenticator = new AmqpAuthenticationRefresher(deviceIdentity, AmqpCbsLink);
            await amqpAuthenticator.InitLoopAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, timeout, $"{nameof(AuthenticationRefresherCreator)}");
            return amqpAuthenticator;
        }

        public IAmqpDevice CreateAmqpDevice(
            DeviceIdentity deviceIdentity, 
            Action OnAmqpDeviceDisconnected, 
            Func<MethodRequestInternal, Task> methodHandler, 
            Action<AmqpMessage> twinMessageListener, 
            Func<string, Message, Task> eventListener)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(CreateAmqpDevice)}");
            Lock.Wait();
            AmqpDevices.TryGetValue(deviceIdentity, out IAmqpDevice amqpDevice);
            if (amqpDevice == null)
            {
                amqpDevice = new AmqpDevice(
                    deviceIdentity, 
                    OnAmqpDeviceDisconnected, 
                    AmqpSessionCreator, 
                    AuthenticationRefresherCreator,
                    RemoveDevice, 
                    methodHandler,
                    twinMessageListener, 
                    eventListener);
                AmqpDevices.Add(deviceIdentity, amqpDevice);
            }
            Lock.Release();
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(CreateAmqpDevice)}");
            return amqpDevice;
        }

        private async Task<AmqpSession> AmqpSessionCreator(DeviceIdentity deviceIdentity, ILinkFactory linkFactory, AmqpSessionSettings amqpSessionSettings, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, timeout, $"{nameof(AmqpSessionCreator)}");
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            AmqpConnection amqpConnection = await EnsureConnection(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            AmqpSession amqpSession = new AmqpSession(amqpConnection, amqpSessionSettings, linkFactory);
            amqpConnection.AddSession(amqpSession, new ushort?());
            if (Logging.IsEnabled) Logging.Associate(amqpConnection, amqpSession, $"{nameof(AmqpSessionCreator)}");
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, timeout, $"{nameof(AmqpSessionCreator)}");
            return amqpSession;
        }

        public int GetNumberOfDevices()
        {
            int count = AmqpDevices.Count;
            if (Logging.IsEnabled) Logging.Info(this, count, $"{nameof(GetNumberOfDevices)}");
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
                        amqpCbsLink = AmqpCbsLink ?? new AmqpCbsLink(amqpConnection);
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
            Lock.Wait();
            if (AmqpConnection != null && ReferenceEquals(AmqpConnection, o))
            {
                AmqpCbsLink?.Close();
                AmqpAuthenticationRefresher?.StopLoop();
                AmqpCbsLink = null;
                AmqpAuthenticationRefresher = null;
                AmqpConnection = null;
                AmqpDevices.Clear();
                IdleNotified = false;
            }
            Lock.Release();
            if (Logging.IsEnabled) Logging.Exit(this, o, $"{nameof(OnConnectionClosed)}");
        }

        private void RemoveDevice(DeviceIdentity deviceIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(RemoveDevice)}");
            bool removed = AmqpDevices.Remove(deviceIdentity);
            if (removed && !IdleNotified)
            {
                IdleNotified = AmqpDevices.Count == 0 && IdleNotified;
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
                AmqpDevices?.Clear();
                AmqpAuthenticationRefresher?.Dispose();
            }
        }
    }
}
