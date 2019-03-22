// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Shared;

#if !NETSTANDARD1_3
using System.Configuration;
#endif

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpConnector : IAmqpConnector
    {
        #region Members-Constructor
        const string DisableServerCertificateValidationKeyName = "Microsoft.Azure.Devices.DisableServerCertificateValidation";

        static readonly AmqpVersion amqpVersion_1_0_0 = new AmqpVersion(1, 0, 0);
        static readonly bool DisableServerCertificateValidation = InitializeDisableServerCertificateValidation();

        private readonly AmqpSettings AmqpSettings;
        private readonly AmqpTransportSettings AmqpTransportSettings;
        private readonly AmqpConnectionSettings AmqpConnectionSettings;
        private readonly TlsTransportSettings TlsTransportSettings;
        
        private TaskCompletionSource<TransportBase> TaskCompletionSource;
        private ProtocolHeader SentProtocolHeader;
        private bool _disposed;

        internal AmqpConnector(AmqpTransportSettings amqpTransportSettings, string hostName)
        {
            AmqpTransportSettings = amqpTransportSettings;
            AmqpSettings = new AmqpSettings();
            var amqpTransportProvider = new AmqpTransportProvider();
            amqpTransportProvider.Versions.Add(amqpVersion_1_0_0);
            AmqpSettings.TransportProviders.Add(amqpTransportProvider);
            AmqpConnectionSettings = new AmqpConnectionSettings()
            {
                MaxFrameSize = AmqpConstants.DefaultMaxFrameSize,
                ContainerId = CommonResources.GetNewStringGuid(),
                HostName = hostName
            };
            var tcpTransportSettings = new TcpTransportSettings()
            {
                Host = hostName,
                Port = AmqpConstants.DefaultSecurePort
            };

            TlsTransportSettings = new TlsTransportSettings(tcpTransportSettings)
            {
                TargetHost = hostName,
                Certificate = null,
                CertificateValidationCallback = AmqpTransportSettings.RemoteCertificateValidationCallback ?? OnRemoteCertificateValidation
            };

            if (AmqpTransportSettings.ClientCertificate != null)
            {
                TlsTransportSettings.Certificate = AmqpTransportSettings.ClientCertificate;
            }
        }
        #endregion

        #region Open-Close
        public async Task<AmqpConnection> OpenConnectionAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(OpenConnectionAsync)}");
            TransportBase transportBase = null;

            try
            {
                transportBase = await InitializeTransport(timeout).ConfigureAwait(false);
                AmqpConnection amqpConnection = new AmqpConnection(transportBase, AmqpSettings, AmqpConnectionSettings);
                await amqpConnection.OpenAsync(timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(OpenConnectionAsync)}");
                return amqpConnection;
            }
            catch(Exception)
            {
                transportBase?.Close();
                throw;
            }
        }

        private async Task<TransportBase> InitializeTransport(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(InitializeTransport)}");
            TransportBase transport;

            switch (AmqpTransportSettings.GetTransportType())
            {
                case TransportType.Amqp_WebSocket_Only:
                    transport = await CreateClientWebSocketTransportAsync(timeout).ConfigureAwait(false);
                    SaslTransportProvider provider = AmqpSettings.GetTransportProvider<SaslTransportProvider>();
                    if (provider != null)
                    {
                        if (Logging.IsEnabled) Logging.Info(this, $"{nameof(InitializeTransport)}: Using SaslTransport");
                        SentProtocolHeader = new ProtocolHeader(provider.ProtocolId, provider.DefaultVersion);
                        ByteBuffer buffer = new ByteBuffer(new byte[AmqpConstants.ProtocolHeaderSize]);
                        SentProtocolHeader.Encode(buffer);

                        TaskCompletionSource = new TaskCompletionSource<TransportBase>();

                        var args = new TransportAsyncCallbackArgs();
                        args.SetBuffer(buffer.Buffer, buffer.Offset, buffer.Length);
                        args.CompletedCallback = OnWriteHeaderComplete;
                        args.Transport = transport;
                        bool operationPending = transport.WriteAsync(args);

                        if (Logging.IsEnabled) Logging.Info(this, $"{nameof(InitializeTransport)}: Sent Protocol Header: {SentProtocolHeader.ToString()} operationPending: {operationPending} completedSynchronously: {args.CompletedSynchronously}");

                        if (!operationPending)
                        {
                            args.CompletedCallback(args);
                        }

                        transport = await TaskCompletionSource.Task.ConfigureAwait(false);
                        await transport.OpenAsync(timeout).ConfigureAwait(false);
                    }
                    break;
                case TransportType.Amqp_Tcp_Only:
                    var amqpTransportInitiator = new AmqpTransportInitiator(AmqpSettings, TlsTransportSettings);
                    transport = await amqpTransportInitiator.ConnectTaskAsync(timeout).ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidOperationException("AmqpTransportSettings must specify WebSocketOnly or TcpOnly");
            }
            if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(InitializeTransport)}");
            return transport;
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
                if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(CreateClientWebSocketTransportAsync)}");

                string additionalQueryParams = "";
#if NETSTANDARD1_3
                            // NETSTANDARD1_3 implementation doesn't set client certs, so we want to tell the IoT Hub to not ask for them
                            additionalQueryParams = "?iothub-no-client-cert=true";
#endif
                Uri websocketUri = new Uri(WebSocketConstants.Scheme + AmqpConnectionSettings.HostName + ":" + WebSocketConstants.SecurePort + WebSocketConstants.UriSuffix + additionalQueryParams);
                // Use Legacy WebSocket if it is running on Windows 7 or older. Windows 7/Windows 2008 R2 is version 6.1
#if NET451
                            if (Environment.OSVersion.Version.Major < 6 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor <= 1))
                            {
                                var websocket = await CreateLegacyClientWebSocketAsync(websocketUri, this.AmqpTransportSettings.ClientCertificate, timeout).ConfigureAwait(false);
                                return new LegacyClientWebSocketTransport(
                                    websocket,
                                    this.AmqpTransportSettings.OperationTimeout,
                                    null,
                                    null);
                            }
                            else
                            {
#endif
                var websocket = await CreateClientWebSocketAsync(websocketUri, timeout).ConfigureAwait(false);
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
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(CreateClientWebSocketTransportAsync)}");
            }
        }

        private async Task<ClientWebSocket> CreateClientWebSocketAsync(Uri websocketUri, TimeSpan timeout)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(CreateClientWebSocketAsync)}");

                var websocket = new ClientWebSocket();

                // Set SubProtocol to AMQPWSB10
                websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Amqpwsb10);

                // Check if we're configured to use a proxy server
                IWebProxy webProxy = AmqpTransportSettings.Proxy;

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

                if (AmqpTransportSettings.ClientCertificate != null)
                {
                    websocket.Options.ClientCertificates.Add(AmqpTransportSettings.ClientCertificate);
                }

                using (var cancellationTokenSource = new CancellationTokenSource(timeout))
                {
                    await websocket.ConnectAsync(websocketUri, cancellationTokenSource.Token).ConfigureAwait(false);
                }

                return websocket;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(CreateClientWebSocketAsync)}");
            }
        }


        private void OnReadHeaderComplete(TransportAsyncCallbackArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(OnReadHeaderComplete)}");

            if (args.Exception != null)
            {
                CompleteOnException(args);
                return;
            }

            try
            {
                ProtocolHeader receivedHeader = new ProtocolHeader();
                receivedHeader.Decode(new ByteBuffer(args.Buffer, args.Offset, args.Count));

                if (Logging.IsEnabled) Logging.Info(this, $"{nameof(OnReadHeaderComplete)}: Received Protocol Header: {receivedHeader.ToString()}");

                if (!receivedHeader.Equals(SentProtocolHeader))
                {
                    throw new AmqpException(AmqpErrorCode.NotImplemented, $"The requested protocol version {SentProtocolHeader} is not supported. The supported version is {receivedHeader}");
                }

                SaslTransportProvider provider = AmqpSettings.GetTransportProvider<SaslTransportProvider>();
                var transport = provider.CreateTransport(args.Transport, true);
                if (Logging.IsEnabled) Logging.Info(this, $"{nameof(OnReadHeaderComplete)}: Created SaslTransportHandler ");
                TaskCompletionSource.TrySetResult(transport);
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(OnReadHeaderComplete)}");
            }
            catch (Exception ex)
            {
                args.Exception = ex;
                CompleteOnException(args);
            }
        }

        private void CompleteOnException(TransportAsyncCallbackArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(CompleteOnException)}");

            if (args.Exception != null && args.Transport != null)
            {
                if (Logging.IsEnabled) Logging.Error(this, $"{nameof(CompleteOnException)}: Exception thrown {args.Exception.Message}");

                args.Transport.SafeClose(args.Exception);
                args.Transport = null;
                TaskCompletionSource.TrySetException(args.Exception);
            }
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(CompleteOnException)}");
        }

        private void OnWriteHeaderComplete(TransportAsyncCallbackArgs args)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(OnWriteHeaderComplete)}");

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
            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(OnWriteHeaderComplete)}");
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

            if (DisableServerCertificateValidation && sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
            {
                return true;
            }

            return false;
        }
        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                TaskCompletionSource?.SetCanceled();
                TaskCompletionSource = null;
                SentProtocolHeader = null;
            }

            _disposed = true;
        }
    }
}
