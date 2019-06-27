// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTConnection
    {
        static readonly AmqpVersion amqpVersion_1_0_0 = new AmqpVersion(1, 0, 0);

        public event EventHandler Closed;
        private AmqpConnection _amqpConnection;
        private AmqpConnectionSettings _amqpConnectionSettings;

        private AmqpTransportSettings _amqpTransportSettings;
        private AmqpIoTTransport _amqpIoTTransport;

        private AmqpSettings _amqpSettings;

        internal AmqpIoTConnection(AmqpTransportSettings amqpTransportSettings, string hostName, bool disableServerCertificateValidation)
        {
            _amqpTransportSettings = amqpTransportSettings;

            _amqpSettings = new AmqpSettings();
            var amqpTransportProvider = new AmqpTransportProvider();
            amqpTransportProvider.Versions.Add(amqpVersion_1_0_0);
            _amqpSettings.TransportProviders.Add(amqpTransportProvider);

            _amqpConnectionSettings = new AmqpConnectionSettings()
            {
                MaxFrameSize = AmqpConstants.DefaultMaxFrameSize,
                ContainerId = CommonResources.GetNewStringGuid(),
                HostName = hostName
            };

            _amqpIoTTransport = new AmqpIoTTransport(_amqpSettings, _amqpTransportSettings, hostName, disableServerCertificateValidation);
        }

        private void OnAmqpConnectionClosed(object sender, EventArgs e)
        {
            Closed.Invoke(sender, e);
        }

        internal async Task<AmqpIoTConnection> OpenConnectionAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(OpenConnectionAsync)}");

            TransportBase transportBase = await _amqpIoTTransport.Initialize(timeout).ConfigureAwait(false);
            try
            {
                _amqpConnection = new AmqpConnection(transportBase, _amqpSettings, _amqpConnectionSettings);
                _amqpConnection.Closed += OnAmqpConnectionClosed;
                await _amqpConnection.OpenAsync(timeout).ConfigureAwait(false);

                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(OpenConnectionAsync)}");
                return this;
            }
            catch (Exception ex)
            {
                transportBase?.Close();
                AmqpIoTExceptionAdapter.HandleAmqpException(ex, "AMQP connection open failed.");
                throw;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(OpenConnectionAsync)}");
            }
        }

        internal AmqpIoTCbsLink CreateCbsLink(DeviceIdentity deviceIdentity, TimeSpan timeout)
        {
            return new AmqpIoTCbsLink(_amqpConnection);
        }

        internal AmqpIoTSession AddSession()
        {
            return new AmqpIoTSession(_amqpConnection);
        }

        internal bool IsClosing()
        {
            return _amqpConnection.IsClosing();
        }

        internal void Abort()
        {
            _amqpConnection.SafeClose();
        }
    }
}
