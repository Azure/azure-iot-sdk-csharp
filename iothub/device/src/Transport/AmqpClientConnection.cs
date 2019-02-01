// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Amqp.Framing;
    using Microsoft.Azure.Amqp.Sasl;
    using Microsoft.Azure.Amqp.Transport;
    using Microsoft.Azure.Devices.Shared;
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Net.WebSockets;
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

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

        internal AmqpSettings amqpSettings { get; private set; }
        internal AmqpTransportSettings amqpTransportSettings { get; private set; }
        internal AmqpConnectionSettings amqpConnectionSettings { get; private set; }

        internal TlsTransportSettings tlsTransportSettings { get; private set; }

        public AmqpConnection amqpConnection { get; protected set; }

        private TaskCompletionSource<TransportBase> taskCompletionSource;
        private ProtocolHeader sentProtocolHeader;

        string hostName;

        internal AmqpClientConnection(AmqpTransportSettings amqpTransportSettings, string hostName)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnection)}");

            this.amqpTransportSettings = amqpTransportSettings;
            this.hostName = hostName;

            this.amqpSettings = CreateAmqpSettings();
            this.amqpConnectionSettings = CreateAmqpConnectionSettings();
            this.tlsTransportSettings = CreateTlsTransportSettings();

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientConnection)}");
        }

        internal abstract bool AddToMux(DeviceClientEndpointIdentity deviceClientEndpointIdentity);
        #endregion

        #region Open-Close
        internal abstract Task OpenAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout);
        internal abstract Task CloseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout);
        internal abstract event EventHandler OnAmqpClientConnectionClosed;

        protected async Task<TransportBase> InitializeTransport(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
        {
            TransportBase transport;

            var timeoutHelper = new TimeoutHelper(timeout);

            switch (this.amqpTransportSettings.GetTransportType())
            {
                case Microsoft.Azure.Devices.Client.TransportType.Amqp_WebSocket_Only:
                    transport = await CreateClientWebSocketTransportAsync(deviceClientEndpointIdentity, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    SaslTransportProvider provider = amqpSettings.GetTransportProvider<SaslTransportProvider>();
                    if (provider != null)
                    {
                        if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientConnection)}.{nameof(OpenAsync)}: Using SaslTransport");
                        sentProtocolHeader = new ProtocolHeader(provider.ProtocolId, provider.DefaultVersion);
                        ByteBuffer buffer = new ByteBuffer(new byte[AmqpConstants.ProtocolHeaderSize]);
                        sentProtocolHeader.Encode(buffer);

                        taskCompletionSource = new TaskCompletionSource<TransportBase>();

                        var args = new TransportAsyncCallbackArgs();
                        args.SetBuffer(buffer.Buffer, buffer.Offset, buffer.Length);
                        args.CompletedCallback = OnWriteHeaderComplete;
                        args.Transport = transport;
                        bool operationPending = transport.WriteAsync(args);

                        if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientConnection)}.{nameof(OpenAsync)}: Sent Protocol Header: {sentProtocolHeader.ToString()} operationPending: {operationPending} completedSynchronously: {args.CompletedSynchronously}");

                        if (!operationPending)
                        {
                            args.CompletedCallback(args);
                        }

                        transport = await taskCompletionSource.Task.ConfigureAwait(false);
                        await transport.OpenAsync(timeout).ConfigureAwait(false);
                    }
                    break;
                case Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only:
                    var amqpTransportInitiator = new AmqpTransportInitiator(amqpSettings, tlsTransportSettings);
                    transport = await amqpTransportInitiator.ConnectTaskAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidOperationException("AmqpTransportSettings must specify WebSocketOnly or TcpOnly");
            }

            return transport;
        }

        private void OnReadHeaderComplete(TransportAsyncCallbackArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnection)}.{nameof(OnReadHeaderComplete)}");

            if (args.Exception != null)
            {
                CompleteOnException(args);
                return;
            }

            try
            {
                ProtocolHeader receivedHeader = new ProtocolHeader();
                receivedHeader.Decode(new ByteBuffer(args.Buffer, args.Offset, args.Count));

                if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientConnection)}.{nameof(OnReadHeaderComplete)}: Received Protocol Header: {receivedHeader.ToString()}");

                if (!receivedHeader.Equals(sentProtocolHeader))
                {
                    throw new AmqpException(AmqpErrorCode.NotImplemented, $"The requested protocol version {sentProtocolHeader} is not supported. The supported version is {receivedHeader}");
                }

                SaslTransportProvider provider = amqpSettings.GetTransportProvider<SaslTransportProvider>();
                var transport = provider.CreateTransport(args.Transport, true);
                if (Logging.IsEnabled) Logging.Info(this, $"{nameof(AmqpClientConnection)}.{nameof(OnReadHeaderComplete)}: Created SaslTransportHandler ");
                taskCompletionSource.TrySetResult(transport);
            }
            catch (Exception ex)
            {
                args.Exception = ex;
                CompleteOnException(args);
            }
        }

        private void CompleteOnException(TransportAsyncCallbackArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnection)}.{nameof(CompleteOnException)}");

            if (args.Exception != null && args.Transport != null)
            {
                if (Logging.IsEnabled) Logging.Error(this, $"{nameof(AmqpClientConnection)}.{nameof(CompleteOnException)}: Exception thrown {args.Exception.Message}");

                args.Transport.SafeClose(args.Exception);
                args.Transport = null;
                taskCompletionSource.TrySetException(args.Exception);
            }
        }

        private void OnWriteHeaderComplete(TransportAsyncCallbackArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnection)}.{nameof(OnWriteHeaderComplete)}");

            if (args.Exception != null)
            {
                CompleteOnException(args);
                return;
            }

            byte[] headerBuffer = new byte[AmqpConstants.ProtocolHeaderSize];
            args.SetBuffer(headerBuffer, 0, headerBuffer.Length);
            args.CompletedCallback = OnReadHeaderComplete;
            bool operationPending = args.Transport.ReadAsync(args);

            if (!operationPending)
            {
                args.CompletedCallback(args);
            }
        }
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
        internal abstract Task EnableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout);
        internal abstract Task DisableTelemetryAndC2DAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout);
        internal abstract Task<Outcome> SendTelemetrMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage message, TimeSpan timeout);
        #endregion

        #region Methods
        internal abstract Task EnableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Func<MethodRequestInternal, Task> methodReceivedListener, TimeSpan timeout);
        internal abstract Task DisableMethodsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout);
        internal abstract Task<Outcome> SendMethodResponseAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage methodResponse, TimeSpan timeout);
        #endregion

        #region Twin
        internal abstract Task EnableTwinPatchAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string correlationid, Action<AmqpMessage> onTwinPathReceivedListener, TimeSpan timeout);
        internal abstract Task DisableTwinAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout);
        internal abstract Task<Outcome> SendTwinMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage twinMessage, TimeSpan timeout);
        #endregion

        #region Events
        internal abstract Task EnableEventsReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, Action<AmqpMessage> onEventsReceivedListener, TimeSpan timeout);
        #endregion

        #region Receive
        internal abstract Task<Message> ReceiveAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout);
        #endregion

        #region Accept-Dispose
        internal abstract Task<Outcome> DisposeMessageAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string lockToken, Outcome outcome, TimeSpan timeout);
        internal abstract void DisposeTwinPatchDelivery(DeviceClientEndpointIdentity deviceClientEndpointIdentity, AmqpMessage amqpMessage);
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
                ContainerId = CommonResources.GetNewStringGuid(),
                HostName = hostName
            };
        }

        private TlsTransportSettings CreateTlsTransportSettings()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnection)}.{nameof(CreateTlsTransportSettings)}");

            var tcpTransportSettings = new TcpTransportSettings()
            {
                Host = hostName,
                Port = AmqpConstants.DefaultSecurePort
            };

            var tlsTransportSettings = new TlsTransportSettings(tcpTransportSettings)
            {
                TargetHost = hostName,
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

        protected async Task<TransportBase> CreateClientWebSocketTransportAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout)
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
                Uri websocketUri = new Uri(WebSocketConstants.Scheme + deviceClientEndpointIdentity.iotHubConnectionString.HostName + ":" + WebSocketConstants.SecurePort + WebSocketConstants.UriSuffix + additionalQueryParams);
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
