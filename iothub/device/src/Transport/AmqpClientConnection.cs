// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Amqp.Transport;
    using System.Threading.Tasks;
    using System.Threading;
    using Microsoft.Azure.Amqp.Framing;
    using Microsoft.Azure.Devices.Shared;
    using System.Net.WebSockets;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Globalization;
    using System.Net.Security;
#if !NETSTANDARD1_3
    using System.Configuration;
#endif

    internal abstract class AmqpClientConnection
    {
        #region Members-Constructor
        protected readonly AmqpVersion amqpVersion_1_0_0 = new AmqpVersion(1, 0, 0);

        const string DisableServerCertificateValidationKeyName =
            "Microsoft.Azure.Devices.DisableServerCertificateValidation";

        static readonly Lazy<bool> DisableServerCertificateValidation =
            new Lazy<bool>(InitializeDisableServerCertificateValidation);

        internal DeviceClientEndpointIdentity deviceClientEndpointIdentity { get; private set; }

        internal IotHubConnectionString iotHubConnectionString { get; private set; }

        internal AmqpSettings amqpSettings { get; private set; }
        internal AmqpTransportSettings amqpTransportSettings { get; private set; }
        internal AmqpConnectionSettings amqpConnectionSettings { get; private set; }

        internal TlsTransportSettings tlsTransportSettings { get; private set; }

        public AmqpConnection amqpConnection { get; protected set; }

        internal AmqpClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnection)}");

            this.deviceClientEndpointIdentity = deviceClientEndpointIdentity;
            this.amqpTransportSettings = deviceClientEndpointIdentity.amqpTransportSettings;
            this.iotHubConnectionString = deviceClientEndpointIdentity.iotHubConnectionString;

            this.amqpSettings = CreateAmqpSettings();
            this.amqpConnectionSettings = CreateAmqpConnectionSettings();
            this.tlsTransportSettings = CreateTlsTransportSettings();

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnection)}");
        }
        #endregion

        #region Open-Close
        internal abstract Task OpenAsync(TimeSpan timeout);
        internal abstract Task CloseAsync(TimeSpan timeout);
        internal abstract event EventHandler OnAmqpClientConnectionClosed;
        #endregion

        #region Authentication
        protected static bool InitializeDisableServerCertificateValidation()
        {
#if NETSTANDARD1_3 // No System.Configuration.ConfigurationManager in NetStandard1.3
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

        protected static bool OnRemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (DisableServerCertificateValidation.Value && sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
            {
                return true;
            }

            return false;
        }
        #endregion

        #region Telemetry
        internal abstract Task EnableTelemetryAndC2DAsync(TimeSpan timeout);
        internal abstract Task DisableTelemetryAndC2DAsync(TimeSpan timeout);
        internal abstract Task<Outcome> SendTelemetrMessageAsync(AmqpMessage message, TimeSpan timeout);
        #endregion

        #region Methods
        internal abstract Task EnableMethodsAsync(string correlationid, Func<MethodRequestInternal, Task> methodReceivedListener, TimeSpan timeout);
        internal abstract Task DisableMethodsAsync(TimeSpan timeout);
        internal abstract Task<Outcome> SendMethodResponseAsync(AmqpMessage methodResponse, TimeSpan timeout);
        #endregion

        #region Twin
        internal abstract Task EnableTwinPatchAsync(string correlationid, Action<AmqpMessage> onTwinPathReceivedListener, TimeSpan timeout);
        internal abstract Task DisableTwinAsync(TimeSpan timeout);
        internal abstract Task<Outcome> SendTwinMessageAsync(AmqpMessage twinMessage, TimeSpan timeout);
        #endregion

        #region Events
        internal abstract Task EnableEventsReceiveAsync(Action<AmqpMessage> onEventsReceivedListener, TimeSpan timeout);
        #endregion

        #region Receive
        internal abstract Task<Message> ReceiveAsync(TimeSpan timeout);
        #endregion

        #region Accept-Dispose
        internal abstract Task<Outcome> DisposeMessageAsync(string lockToken, Outcome outcome, TimeSpan timeout);
        internal abstract void DisposeTwinPatchDelivery(AmqpMessage amqpMessage);
        #endregion

        #region Helpers
        private AmqpSettings CreateAmqpSettings()
        {
            var amqpSettings = new AmqpSettings();

            var amqpTransportProvider = new AmqpTransportProvider();
            amqpTransportProvider.Versions.Add(amqpVersion_1_0_0);
            amqpSettings.TransportProviders.Add(amqpTransportProvider);

            return amqpSettings;
        }

        private AmqpConnectionSettings CreateAmqpConnectionSettings()
        {
            return new AmqpConnectionSettings()
            {
                MaxFrameSize = AmqpConstants.DefaultMaxFrameSize,
                ContainerId = CommonResources.GetNewStringGuid(""),
                HostName = iotHubConnectionString.HostName
            };
        }

        private TlsTransportSettings CreateTlsTransportSettings()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnection)}.{nameof(CreateTlsTransportSettings)}");

            var tcpTransportSettings = new TcpTransportSettings()
            {
                Host = iotHubConnectionString.HostName,
                Port = AmqpConstants.DefaultSecurePort
            };

            var tlsTransportSettings = new TlsTransportSettings(tcpTransportSettings)
            {
                TargetHost = iotHubConnectionString.HostName,
                Certificate = null,
                CertificateValidationCallback = this.amqpTransportSettings.RemoteCertificateValidationCallback ?? OnRemoteCertificateValidation
            };

            if (this.amqpTransportSettings.ClientCertificate != null)
            {
                tlsTransportSettings.Certificate = this.amqpTransportSettings.ClientCertificate;
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnection)}.{nameof(CreateTlsTransportSettings)}");

            return tlsTransportSettings;
        }

#if NET451
        private static async Task<IotHubClientWebSocket> CreateLegacyClientWebSocketAsync(Uri webSocketUri, X509Certificate2 clientCertificate, TimeSpan timeout)
        {
            var websocket = new IotHubClientWebSocket(WebSocketConstants.SubProtocols.Amqpwsb10);
            await websocket.ConnectAsync(webSocketUri.Host, webSocketUri.Port, WebSocketConstants.Scheme, clientCertificate, timeout).ConfigureAwait(false);
            return websocket;
        }
#endif

        protected async Task<TransportBase> CreateClientWebSocketTransportAsync(TimeSpan timeout)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(AmqpClientConnection)}.{nameof(CreateClientWebSocketTransportAsync)}");

                var timeoutHelper = new TimeoutHelper(timeout);
                string additionalQueryParams = "";
#if NETSTANDARD1_3
                            // NETSTANDARD1_3 implementation doesn't set client certs, so we want to tell the IoT Hub to not ask for them
                            additionalQueryParams = "?iothub-no-client-cert=true";
#endif
                Uri websocketUri = new Uri(WebSocketConstants.Scheme + iotHubConnectionString.HostName + ":" + WebSocketConstants.SecurePort + WebSocketConstants.UriSuffix + additionalQueryParams);
                // Use Legacy WebSocket if it is running on Windows 7 or older. Windows 7/Windows 2008 R2 is version 6.1
#if NET451
                            if (Environment.OSVersion.Version.Major < 6 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor <= 1))
                            {
                                var websocket = await CreateLegacyClientWebSocketAsync(websocketUri, this.amqpTransportSettings.ClientCertificate, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                                return new LegacyClientWebSocketTransport(
                                    websocket,
                                    this.amqpTransportSettings.OperationTimeout,
                                    null,
                                    null);
                            }
                            else
                            {
#endif
                var websocket = await this.CreateClientWebSocketAsync(websocketUri, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                return new ClientWebSocketTransport(
                    websocket,
                    null,
                    null);
#if NET451
                            }
#endif
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(AmqpClientConnection)}.{nameof(CreateClientWebSocketTransportAsync)}");
            }
        }

        protected async Task<ClientWebSocket> CreateClientWebSocketAsync(Uri websocketUri, TimeSpan timeout)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, websocketUri, timeout, $"{nameof(AmqpClientConnection)}.{nameof(CreateClientWebSocketAsync)}");

                var websocket = new ClientWebSocket();

                // Set SubProtocol to AMQPWSB10
                websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Amqpwsb10);

                // Check if we're configured to use a proxy server
                IWebProxy webProxy = this.amqpTransportSettings.Proxy;

                try
                {
                    if (webProxy != DefaultWebProxySettings.Instance)
                    {
                        // Configure proxy server
                        websocket.Options.Proxy = webProxy;
                        if (Logging.IsEnabled)
                        {
                            Logging.Info(this, $"{nameof(CreateClientWebSocketAsync)} Setting ClientWebSocket.Options.Proxy");
                        }
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    // .NET Core 2.0 doesn't support proxy. Ignore this setting.
                    if (Logging.IsEnabled)
                    {
                        Logging.Error(this, $"{nameof(CreateClientWebSocketAsync)} PlatformNotSupportedException thrown as .NET Core 2.0 doesn't support proxy");
                    }
                }

                if (this.amqpTransportSettings.ClientCertificate != null)
                {
                    websocket.Options.ClientCertificates.Add(this.amqpTransportSettings.ClientCertificate);
                }

                using (var cancellationTokenSource = new CancellationTokenSource(timeout))
                {
                    await websocket.ConnectAsync(websocketUri, cancellationTokenSource.Token).ConfigureAwait(false);
                }

                return websocket;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, websocketUri, timeout, $"{nameof(AmqpClientConnection)}.{nameof(CreateClientWebSocketAsync)}");
            }
        }

        protected static ArraySegment<byte> ConvertToDeliveryTag(string lockToken)
        {
            if (lockToken == null)
            {
                throw new ArgumentNullException("lockToken");
            }

            Guid lockTokenGuid;
            if (!Guid.TryParse(lockToken, out lockTokenGuid))
            {
                throw new ArgumentException("Should be a valid Guid", "lockToken");
            }

            var deliveryTag = new ArraySegment<byte>(lockTokenGuid.ToByteArray());
            return deliveryTag;
        }
        #endregion
    }
}