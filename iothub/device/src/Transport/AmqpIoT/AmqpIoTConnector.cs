// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Transport.AmqpIoT;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Client.Extensions;
using System.Configuration;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpIoTConnector : IDisposable
    {
        #region Members-Constructor

#if NET451
        const string DisableServerCertificateValidationKeyName = "Microsoft.Azure.Devices.DisableServerCertificateValidation";
#endif
        private static readonly AmqpVersion s_amqpVersion_1_0_0 = new AmqpVersion(1, 0, 0);
        private static readonly bool s_disableServerCertificateValidation = InitializeDisableServerCertificateValidation();

        private readonly AmqpTransportSettings _amqpTransportSettings;
        private readonly string _hostName;

        private bool _disposed;

        internal AmqpIoTConnector(AmqpTransportSettings amqpTransportSettings, string hostName)
        {
            _amqpTransportSettings = amqpTransportSettings;
            _hostName = hostName;
        }

        #endregion Members-Constructor

        #region Open-Close

        public async Task<AmqpIoTConnection> OpenConnectionAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, timeout, $"{nameof(OpenConnectionAsync)}");
            }

            var amqpSettings = new AmqpSettings();
            var amqpTransportProvider = new AmqpTransportProvider();
            amqpTransportProvider.Versions.Add(s_amqpVersion_1_0_0);
            amqpSettings.TransportProviders.Add(amqpTransportProvider);

            var amqpConnectionSettings = new AmqpConnectionSettings()
            {
                MaxFrameSize = AmqpConstants.DefaultMaxFrameSize,
                ContainerId = CommonResources.GetNewStringGuid(),
                HostName = _hostName
            };

            TimeSpan idleTimeout = _amqpTransportSettings.IdleTimeout;
            if (idleTimeout != null)
            {
                amqpConnectionSettings.IdleTimeOut = Convert.ToUInt32(idleTimeout.TotalMilliseconds);
            }

            var amqpIoTTransport = new AmqpIoTTransport(amqpSettings, _amqpTransportSettings, _hostName, s_disableServerCertificateValidation);

            TransportBase transportBase = await amqpIoTTransport.InitializeAsync(timeout).ConfigureAwait(false);
            try
            {
                var amqpConnection = new AmqpConnection(transportBase, amqpSettings, amqpConnectionSettings);
                AmqpIoTConnection amqpIoTConnection = new AmqpIoTConnection(amqpConnection);
                amqpConnection.Closed += amqpIoTConnection.AmqpConnectionClosed;
                await amqpConnection.OpenAsync(timeout).ConfigureAwait(false);

                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, timeout, $"{nameof(OpenConnectionAsync)}");
                }

                return amqpIoTConnection;
            }
            catch (Exception e) when (!e.IsFatal())
            {
                transportBase?.Close();
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, $"{nameof(OpenConnectionAsync)}");
                }
            }
        }

        #endregion Open-Close

        #region Authentication

        private static bool InitializeDisableServerCertificateValidation()
        {
#if !NET451
            bool flag;
            if (!AppContext.TryGetSwitch("DisableServerCertificateValidationKeyName", out flag))
            {
                return false;
            }
            return flag;
#else
            string value = ConfigurationManager.AppSettings[DisableServerCertificateValidationKeyName];
            if (!string.IsNullOrEmpty(value))
            {
                return bool.Parse(value);
            }
            return false;
#endif
        }

        #endregion Authentication

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (Logging.IsEnabled)
            {
                Logging.Info(this, disposing, $"{nameof(Dispose)}");
            }

            _disposed = true;
        }
    }
}
