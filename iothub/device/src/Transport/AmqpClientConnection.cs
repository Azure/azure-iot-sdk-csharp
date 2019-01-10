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

    /// <summary>
    ///
    /// 
    /// </summary>
    public enum AmqpClientConnectionState
    {
        /// <summary>
        /// 
        /// </summary>
        NotStarted,
        /// <summary>
        /// 
        /// </summary>
        Opened,
        /// <summary>
        /// 
        /// </summary>
        Closed
    }

    /// <summary>
    /// 
    /// </summary>
    internal abstract class AmqpClientConnection
    {
        protected readonly AmqpVersion amqpVersion_1_0_0 = new AmqpVersion(1, 0, 0);

        internal DeviceClientEndpointIdentity deviceClientEndpointIdentity { get; private set; }

        internal IotHubConnectionString iotHubConnectionString { get; private set; }

        internal AmqpSettings amqpSettings { get; private set; }
        internal AmqpTransportSettings amqpTransportSettings { get; private set; }
        internal AmqpConnectionSettings amqpConnectionSettings { get; private set; }

        internal TlsTransportSettings tlsTransportSettings { get; private set; }

        public AmqpConnection amqpConnection { get; protected set; }

        internal AmqpClientConnectionState amqpClientConnectionState { get; private set; }

        internal RemoveClientConnectionFromPool removeFromPoolDelegate { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceClientEndpointIdentity"></param>
        /// <param name="removeDelegate"></param>
        protected AmqpClientConnection(DeviceClientEndpointIdentity deviceClientEndpointIdentity, RemoveClientConnectionFromPool removeDelegate)
        {
            this.deviceClientEndpointIdentity = deviceClientEndpointIdentity;
            this.amqpTransportSettings = deviceClientEndpointIdentity.amqpTransportSettings;
            this.iotHubConnectionString = deviceClientEndpointIdentity.iotHubConnectionString;
            this.removeFromPoolDelegate = removeDelegate;
            this.amqpClientConnectionState = AmqpClientConnectionState.NotStarted;

            this.amqpSettings = CreateAmqpSettings();
            this.amqpConnectionSettings = CreateAmqpConnectionSettings();
            this.tlsTransportSettings = CreateTlsTransportSettings();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal abstract Task OpenAsync(TimeSpan timeout);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="operationTimeout"></param>
        /// <returns></returns>
        internal abstract Task<Outcome> SendEventAsync(AmqpMessage message, TimeSpan timeout);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal abstract Task<Message> ReceiveAsync(TimeSpan timeout);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal abstract Task EnableMethodAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal abstract Task DisableMethodsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal abstract Task EnableTwinPatchAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal abstract Task DisableTwinAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal abstract Task EnableEventReceiveAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodResponse"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal abstract Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lockToken"></param>
        /// <param name="acceptedOutcome"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal abstract Task DisposeMessageAsync(string lockToken, Accepted acceptedOutcome, CancellationToken cancellationToken);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="amqpMessage"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal abstract Task<Twin> RoundTripTwinMessage(object amqpMessage, CancellationToken cancellationToken);

        internal abstract event EventHandler OnAmqpClientConnectionClosed;

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
                ContainerId = Guid.NewGuid().ToString("N"),
                HostName = iotHubConnectionString.HostName
            };
        }

        protected TlsTransportSettings CreateTlsTransportSettings()
        {
            var tcpTransportSettings = new TcpTransportSettings()
            {
                Host = iotHubConnectionString.HostName,
                Port = AmqpConstants.DefaultSecurePort
            };

            var tlsTransportSettings = new TlsTransportSettings(tcpTransportSettings)
            {
                TargetHost = iotHubConnectionString.HostName,
                Certificate = amqpTransportSettings.ClientCertificate,
                CertificateValidationCallback = this.amqpTransportSettings.RemoteCertificateValidationCallback
            };

            if (this.amqpTransportSettings.ClientCertificate != null)
            {
                tlsTransportSettings.Certificate = this.amqpTransportSettings.ClientCertificate;
            }

            return tlsTransportSettings;
        }

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

#if NET451
                        static async Task<IotHubClientWebSocket> CreateLegacyClientWebSocketAsync(Uri webSocketUri, X509Certificate2 clientCertificate, TimeSpan timeout)
                        {
                            var websocket = new IotHubClientWebSocket(WebSocketConstants.SubProtocols.Amqpwsb10);
                            await websocket.ConnectAsync(webSocketUri.Host, webSocketUri.Port, WebSocketConstants.Scheme, clientCertificate, timeout).ConfigureAwait(false);
                            return websocket;
                        }
#endif
    }
}