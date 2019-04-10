using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpSessionHolder : AbstractStatusWatcher, IAmqpSessionHolder
    {
        private readonly IAmqpConnectionHolder _amqpConnectionHolder;
        private readonly DeviceIdentity _deviceIdentity;
        private readonly SemaphoreSlim _sessionLock;

        private IAmqpAuthenticationRefresher _amqpAuthenticationRefresher;
        private AmqpSession _amqpSession;
        private HashSet<AmqpLink> _amqpLinks;

        internal AmqpSessionHolder(DeviceIdentity deviceIdentity, IAmqpConnectionHolder amqpConnectionHolder) : base()
        {
            if (Logging.IsEnabled) Logging.Enter(this, deviceIdentity, $"{nameof(AmqpSessionHolder)}");
            _deviceIdentity = deviceIdentity;
            _amqpConnectionHolder = amqpConnectionHolder;

            _amqpLinks = new HashSet<AmqpLink>();
            _sessionLock = new SemaphoreSlim(1, 1);

            if (Logging.IsEnabled) Logging.Associate(amqpConnectionHolder, deviceIdentity, $"{nameof(AmqpSessionHolder)}");
            if (Logging.IsEnabled) Logging.Associate(amqpConnectionHolder, this, $"{nameof(AmqpSessionHolder)}");
            if (Logging.IsEnabled) Logging.Associate(deviceIdentity, this, $"{nameof(AmqpSessionHolder)}");
            if (Logging.IsEnabled) Logging.Exit(this, deviceIdentity, $"{nameof(AmqpSessionHolder)}");
        }

        public async Task<ReceivingAmqpLink> OpenReceivingAmqpLinkAsync(
            byte? senderSettleMode, 
            byte? receiverSettleMode, 
            string deviceTemplate, 
            string moduleTemplate, 
            string linkSuffix, 
            string CorrelationId, 
            TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, _deviceIdentity, $"{nameof(OpenReceivingAmqpLinkAsync)}");

            AmqpSession amqpSession = await EnsureSessionAsync(timeout).ConfigureAwait(false);
            if (_disposed)
            {
                amqpSession.Abort();
                throw new ObjectDisposedException("AmqpSessionHolder is disposed.");
            }
            uint prefetchCount = _deviceIdentity.AmqpTransportSettings.PrefetchCount;
            AmqpLinkSettings amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = CommonResources.GetNewStringGuid(linkSuffix),
                Role = true,
                TotalLinkCredit = prefetchCount,
                AutoSendFlow = prefetchCount > 0,
                Source = new Source() { Address = AmqpHelper.BuildLinkAddress(_deviceIdentity, deviceTemplate, moduleTemplate) },
                Target = new Target() { Address = _deviceIdentity.IotHubConnectionString.DeviceId }
            };

            amqpLinkSettings.SndSettleMode = senderSettleMode;
            amqpLinkSettings.RcvSettleMode = receiverSettleMode;
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.TimeoutName, timeout.TotalMilliseconds);
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.ClientVersion, _deviceIdentity.ProductInfo.ToString());
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.ApiVersion, ClientApiVersionHelper.ApiVersionString);
            if (CorrelationId != null)
            {
                amqpLinkSettings.AddProperty(IotHubAmqpProperty.ChannelCorrelationId, CorrelationId);
            }

            ReceivingAmqpLink receivingLink = new ReceivingAmqpLink(amqpLinkSettings);
            try
            {
                receivingLink.AttachTo(amqpSession);
                await receivingLink.OpenAsync(timeout).ConfigureAwait(false);
                lock(_stateLock)
                {
                    if (_disposed)
                    {
                        amqpSession.Abort();
                        throw new ObjectDisposedException("AmqpSessionHolder is disposed.");
                    }
                    else if (receivingLink.IsClosing())
                    {
                        throw new IotHubCommunicationException("Amqp link is closed.");
                    }
                    else
                    {
                        _amqpLinks.Add(receivingLink);
                        receivingLink.Closed += OnLinkDisconnected;
                    }
                }
            }
            catch (Exception exception) when (!exception.IsFatal())
            {
                if (amqpSession.IsClosing())
                {
                    throw new IotHubCommunicationException("Amqp session is closed.");
                }
                else
                {
                    throw;
                }
            }

            ThrowExceptionIfClosedOrDisposed();
            if (Logging.IsEnabled) Logging.Exit(this, _deviceIdentity, $"{nameof(OpenReceivingAmqpLinkAsync)}");
            return receivingLink;
        }

        public async Task<SendingAmqpLink> OpenSendingAmqpLinkAsync(
            byte? senderSettleMode, 
            byte? receiverSettleMode, 
            string deviceTemplate, 
            string moduleTemplate,
            string linkSuffix, 
            string CorrelationId, 
            TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, _deviceIdentity, $"{nameof(OpenSendingAmqpLinkAsync)}");
            AmqpSession amqpSession = await EnsureSessionAsync(timeout).ConfigureAwait(false);

            AmqpLinkSettings amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = CommonResources.GetNewStringGuid(linkSuffix),
                Role = false,
                InitialDeliveryCount = 0,
                Target = new Target() { Address = AmqpHelper.BuildLinkAddress(_deviceIdentity, deviceTemplate, moduleTemplate) },
                Source = new Source() { Address = _deviceIdentity.IotHubConnectionString.DeviceId }
            };

            amqpLinkSettings.SndSettleMode = senderSettleMode;
            amqpLinkSettings.RcvSettleMode = receiverSettleMode;
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.TimeoutName, timeout.TotalMilliseconds);
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.ClientVersion, _deviceIdentity.ProductInfo.ToString());
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.ApiVersion, ClientApiVersionHelper.ApiVersionString);
            if (CorrelationId != null)
            {
                amqpLinkSettings.AddProperty(IotHubAmqpProperty.ChannelCorrelationId, CorrelationId);
            }

            SendingAmqpLink sendingLink = new SendingAmqpLink(amqpLinkSettings);
            try
            {
                sendingLink.AttachTo(amqpSession);
                await sendingLink.OpenAsync(timeout).ConfigureAwait(false);
                lock (_stateLock)
                {
                    if (_disposed)
                    {
                        amqpSession.Abort();
                        throw new ObjectDisposedException("AmqpSessionHolder is disposed.");
                    }
                    else if (sendingLink.IsClosing())
                    {
                        throw new IotHubCommunicationException("Amqp link is closed.");
                    }
                    else
                    {
                        _amqpLinks.Add(sendingLink);
                        sendingLink.Closed += OnLinkDisconnected;
                    }
                }
            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                if (amqpSession.IsClosing())
                {
                    throw new IotHubCommunicationException("Amqp session is closed.");
                }
                else
                {
                    throw;
                }
            }

            ThrowExceptionIfClosedOrDisposed();
            if (Logging.IsEnabled) Logging.Exit(this, _deviceIdentity, $"{nameof(OpenSendingAmqpLinkAsync)}");
            return sendingLink;
        }

        private async Task<AmqpSession> EnsureSessionAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(EnsureSessionAsync)}");
            bool gain = await _sessionLock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }

            AmqpSession amqpSession = GetAmqpSession();
            try
            {
                if (amqpSession?.IsClosing() ?? true)
                {
                    if (_deviceIdentity.AuthenticationModel == AuthenticationModel.SasIndividual)
                    {
                        _amqpAuthenticationRefresher = await _amqpConnectionHolder.StartAmqpAuthenticationRefresherAsync(_deviceIdentity, timeout).ConfigureAwait(false);
                        if (Logging.IsEnabled) Logging.Associate(this, _amqpAuthenticationRefresher, $"{nameof(_amqpAuthenticationRefresher)}");
                    }

                    amqpSession = await _amqpConnectionHolder.CreateAmqpSessionAsync(_deviceIdentity, timeout).ConfigureAwait(false);

                    lock (_stateLock)
                    {
                        if (_disposed)
                        {
                            // release resource
                            amqpSession?.Abort();
                            throw new ObjectDisposedException("AmqpConnectionHolder is disposed.");
                        }
                        else if (amqpSession.IsClosing())
                        {
                            throw new IotHubCommunicationException("Amqp session is closed.");
                        }
                        else
                        {
                            if (Logging.IsEnabled) Logging.Associate(_deviceIdentity, amqpSession, $"{nameof(EnsureSessionAsync)}");
                            _amqpSession = amqpSession;
                            _amqpSession.Closed += OnSessionDisconnected;
                        }

                    }
                    if (Logging.IsEnabled) Logging.Associate(this, _amqpSession, $"{nameof(_amqpSession)}");
                }
            }
            finally
            {
                _sessionLock.Release();
            }

            ThrowExceptionIfClosedOrDisposed();
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(EnsureSessionAsync)}");
            return amqpSession;
        }

        private void OnSessionDisconnected(object amqpSession, EventArgs eventArgs)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpSession, $"{nameof(OnSessionDisconnected)}");
            lock (_stateLock)
            {
                if (ReferenceEquals(amqpSession, _amqpSession))
                {
                    ChangeStatus(Status.Closed);
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, amqpSession, $"{nameof(OnSessionDisconnected)}");
        }

        private void OnLinkDisconnected(object amqpLink, EventArgs eventArgs)
        {
            if (Logging.IsEnabled) Logging.Enter(this, amqpLink, $"{nameof(OnLinkDisconnected)}");
            lock (_stateLock)
            {
                if (_amqpLinks.Contains(amqpLink as AmqpLink))
                {   
                    ChangeStatus(Status.Closed);
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, amqpLink, $"{nameof(OnLinkDisconnected)}");
        }

        private AmqpSession GetAmqpSession()
        {
            lock (_stateLock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("AmqpSessionHolder is disposed.");
                }
                return _amqpSession;
            }
        }


        public async Task CloseAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(CloseAsync)}");
            bool gain = await _sessionLock.WaitAsync(timeout).ConfigureAwait(false);
            if (!gain)
            {
                throw new TimeoutException();
            }
            try
            {
                ChangeStatus(Status.Closed);
            }
            finally
            {
                _sessionLock.Release();
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(CloseAsync)}");
            }
        }

        public void Close()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(Close)}");
            ChangeStatus(Status.Closed);
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(Close)}");
        }

        protected override void CleanupResource()
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(CleanupResource)}");
            if (!_amqpSession?.IsClosing() ?? true)
            {
                _amqpLinks.Clear();
                AmqpHelper.AbortAmqpObject(_amqpSession);
            }
        }

        protected override void DisposeResource()
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{nameof(DisposeResource)}");
            CleanupResource();
        }
    }
}
