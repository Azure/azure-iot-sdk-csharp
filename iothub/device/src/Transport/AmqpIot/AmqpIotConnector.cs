// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;
using Microsoft.Azure.Devices.Shared;

#if NET451
using System.Configuration;
#endif

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpIotConnector : IDisposable
    {
#if NET451
        private const string DisableServerCertificateValidationKeyName = "Microsoft.Azure.Devices.DisableServerCertificateValidation";
#endif
        private static readonly AmqpVersion s_amqpVersion_1_0_0 = new AmqpVersion(1, 0, 0);
        private static readonly bool s_disableServerCertificateValidation = InitializeDisableServerCertificateValidation();

        private readonly AmqpTransportSettings _amqpTransportSettings;
        private readonly string _hostName;

        private AmqpIotTransport _amqpIotTransport;

        internal AmqpIotConnector(AmqpTransportSettings amqpTransportSettings, string hostName)
        {
            _amqpTransportSettings = amqpTransportSettings;
            _hostName = hostName;
        }

        public async Task<AmqpIotConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(OpenConnectionAsync));

            var amqpTransportProvider = new AmqpTransportProvider();
            amqpTransportProvider.Versions.Add(s_amqpVersion_1_0_0);

            var amqpSettings = new AmqpSettings();
            amqpSettings.TransportProviders.Add(amqpTransportProvider);

            var amqpConnectionSettings = new AmqpConnectionSettings
            {
                MaxFrameSize = AmqpConstants.DefaultMaxFrameSize,
                ContainerId = CommonResources.GetNewStringGuid(),
                HostName = _hostName,
                IdleTimeOut = Convert.ToUInt32(_amqpTransportSettings.IdleTimeout.TotalMilliseconds),
            };

            _amqpIotTransport = new AmqpIotTransport(amqpSettings, _amqpTransportSettings, _hostName, s_disableServerCertificateValidation);

            TransportBase transportBase = await _amqpIotTransport.InitializeAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var amqpConnection = new AmqpConnection(transportBase, amqpSettings, amqpConnectionSettings);
                var amqpIotConnection = new AmqpIotConnection(amqpConnection);
                amqpConnection.Closed += amqpIotConnection.AmqpConnectionClosed;
                await amqpConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(OpenConnectionAsync)}");

                return amqpIotConnection;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                transportBase?.Close();
                _amqpIotTransport?.Dispose();
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(OpenConnectionAsync));
            }
        }

        private static bool InitializeDisableServerCertificateValidation()
        {
#if !NET451
            return AppContext.TryGetSwitch("DisableServerCertificateValidationKeyName", out bool flag) && flag;
#else
            string value = ConfigurationManager.AppSettings[DisableServerCertificateValidationKeyName];
            return !string.IsNullOrEmpty(value) && bool.Parse(value);
#endif
        }

        public void Dispose()
        {
            _amqpIotTransport?.Dispose();
            _amqpIotTransport = null;
        }
    }
}
