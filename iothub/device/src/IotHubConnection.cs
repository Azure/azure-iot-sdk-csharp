// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Net.Security;
    using System.Threading;
    using System.Threading.Tasks;

#if !NETSTANDARD1_3
    using System.Configuration;
#endif
    using System.Net.WebSockets;
    using System.Security.Cryptography.X509Certificates;
    using System.Net;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Amqp.Framing;
    using Microsoft.Azure.Amqp.Transport;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Devices.Client.Transport;
    using Microsoft.Azure.Devices.Shared;

    abstract class IotHubConnection
    {
        readonly string hostName;
        readonly int port;

        static readonly AmqpVersion AmqpVersion_1_0_0 = new AmqpVersion(1, 0, 0);

        const string DisableServerCertificateValidationKeyName =
            "Microsoft.Azure.Devices.DisableServerCertificateValidation";

        static readonly Lazy<bool> DisableServerCertificateValidation =
            new Lazy<bool>(InitializeDisableServerCertificateValidation);

        private SemaphoreSlim sessionSemaphore = new SemaphoreSlim(1, 1);

        readonly string twinConnectionCorrelationId = Guid.NewGuid().ToString("N");

        public enum SendingLinkType
        {
            TelemetryEvents,
            Methods,
            Twin
        };

        public enum ReceivingLinkType
        {
            C2DMessages,
            Methods,
            Twin,
            Events
        };

        protected IotHubConnection(string hostName, int port, AmqpTransportSettings amqpTransportSettings)
        {
            this.hostName = hostName;
            this.port = port;
            this.AmqpTransportSettings = amqpTransportSettings;
        }

        protected FaultTolerantAmqpObject<AmqpSession> FaultTolerantSession { get; set; }

        protected AmqpTransportSettings AmqpTransportSettings { get; }

        public abstract Task CloseAsync();

        public abstract void SafeClose(Exception exception);

        public async Task<SendingAmqpLink> CreateSendingLinkAsync(
            string path,
            IotHubConnectionString connectionString,
            string corrId,
            SendingLinkType linkType,
            TimeSpan timeout,
            ProductInfo productInfo,
            CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, corrId, linkType, $"{nameof(IotHubConnection)}.{nameof(CreateSendingLinkAsync)}");

                this.OnCreateSendingLink(connectionString);

                var timeoutHelper = new TimeoutHelper(timeout);

                AmqpSession session = await this.GetSessionAsync(timeoutHelper, cancellationToken).ConfigureAwait(false);

                var linkAddress = this.BuildLinkAddress(connectionString, path);

                var linkSettings = new AmqpLinkSettings()
                {
                    Role = false,
                    InitialDeliveryCount = 0,
                    Target = new Target() { Address = linkAddress.AbsoluteUri },
                    LinkName = Guid.NewGuid().ToString("N") // Use a human readable link name to help with debugging
                };

                switch (linkType)
                {
                    case SendingLinkType.TelemetryEvents:
                        linkSettings.SndSettleMode = null; // SenderSettleMode.Unsettled (null as it is the default and to avoid bytes on the wire)
                        linkSettings.RcvSettleMode = null; // (byte)ReceiverSettleMode.First (null as it is the default and to avoid bytes on the wire)
                        break;
                    case SendingLinkType.Methods:
                    case SendingLinkType.Twin:
                        linkSettings.SndSettleMode = (byte)SenderSettleMode.Settled;
                        linkSettings.RcvSettleMode = (byte)ReceiverSettleMode.First;
                        break;
                }

                SetLinkSettingsCommonProperties(linkSettings, timeoutHelper.RemainingTime(), productInfo);
                if (linkType == SendingLinkType.Methods)
                {
                    SetLinkSettingsCommonPropertiesForMethod(linkSettings, corrId);
                }
                else if (linkType == SendingLinkType.Twin)
                {
                    SetLinkSettingsCommonPropertiesForTwin(linkSettings, corrId);
                }

                var link = new SendingAmqpLink(linkSettings);
                link.AttachTo(session);

                var audience = this.BuildAudience(connectionString, path);
                await this.OpenLinkAsync(link, connectionString, audience, timeoutHelper.RemainingTime(), cancellationToken).ConfigureAwait(false);

                return link;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, corrId, linkType, $"{nameof(IotHubConnection)}.{nameof(CreateSendingLinkAsync)}");
            }
        }

        public async Task<ReceivingAmqpLink> CreateReceivingLinkAsync(
            string path,
            IotHubConnectionString connectionString,
            string corrId,
            ReceivingLinkType linkType,
            uint prefetchCount,
            TimeSpan timeout,
            ProductInfo productInfo,
            CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, corrId, linkType, $"{nameof(IotHubConnection)}.{nameof(CreateReceivingLinkAsync)}");

                this.OnCreateReceivingLink(connectionString);

                var timeoutHelper = new TimeoutHelper(timeout);

                AmqpSession session = await this.GetSessionAsync(timeoutHelper, cancellationToken).ConfigureAwait(false);

                var linkAddress = this.BuildLinkAddress(connectionString, path);

                var linkSettings = new AmqpLinkSettings()
                {
                    Role = true,
                    TotalLinkCredit = prefetchCount,
                    AutoSendFlow = prefetchCount > 0,
                    Source = new Source() { Address = linkAddress.AbsoluteUri },
                    LinkName = Guid.NewGuid().ToString("N") // Use a human readable link name to help with debuggin
                };

                switch (linkType)
                {
                    // Exactly once
                    case ReceivingLinkType.C2DMessages:
                        linkSettings.SndSettleMode = null; // SenderSettleMode.Unsettled (null as it is the default and to avoid bytes on the wire)
                        linkSettings.RcvSettleMode = (byte)ReceiverSettleMode.Second;
                        break;

                    // At least once
                    case ReceivingLinkType.Events:
                        linkSettings.SndSettleMode = null; // SenderSettleMode.Unsettled (null as it is the default and to avoid bytes on the wire)
                        linkSettings.RcvSettleMode = (byte)ReceiverSettleMode.First;
                        break;

                    // At most once
                    case ReceivingLinkType.Methods:
                    case ReceivingLinkType.Twin:
                        linkSettings.SndSettleMode = (byte)SenderSettleMode.Settled;
                        linkSettings.RcvSettleMode = (byte)ReceiverSettleMode.First;
                        break;
                }

                SetLinkSettingsCommonProperties(linkSettings, timeoutHelper.RemainingTime(), productInfo);
                if (linkType == ReceivingLinkType.Methods)
                {
                    SetLinkSettingsCommonPropertiesForMethod(linkSettings, corrId);
                }
                else if (linkType == ReceivingLinkType.Twin)
                {
                    SetLinkSettingsCommonPropertiesForTwin(linkSettings, corrId);
                }

                var link = new ReceivingAmqpLink(linkSettings);
                link.AttachTo(session);

                var audience = this.BuildAudience(connectionString, path);
                await this.OpenLinkAsync(link, connectionString, audience, timeoutHelper.RemainingTime(), cancellationToken).ConfigureAwait(false);

                return link;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, corrId, linkType, $"{nameof(IotHubConnection)}.{nameof(CreateReceivingLinkAsync)}");
            }
        }

        private async Task<AmqpSession> GetSessionAsync(TimeoutHelper timeoutHelper, CancellationToken token)
        {
            if (Logging.IsEnabled) Logging.Enter(this, timeoutHelper, token, $"{nameof(IotHubConnection)}.{nameof(GetSessionAsync)}");

            AmqpSession session;
            try
            {
                await sessionSemaphore.WaitAsync().ConfigureAwait(false);

                session = await this.FaultTolerantSession.GetOrCreateAsync(timeoutHelper.RemainingTime(), token).ConfigureAwait(false);

                Fx.Assert(session != null, "Amqp Session cannot be null.");
                if (session.State != AmqpObjectState.Opened)
                {
                    if (session.State == AmqpObjectState.End)
                    {
                        this.FaultTolerantSession.TryRemove();
                    }
                    session = await this.FaultTolerantSession.GetOrCreateAsync(timeoutHelper.RemainingTime(), token).ConfigureAwait(false);
                }
            }
            finally
            {
                sessionSemaphore.Release();
                if (Logging.IsEnabled) Logging.Exit(this, timeoutHelper, token, $"{nameof(IotHubConnection)}.{nameof(GetSessionAsync)}");
            }

            return session;
        }

        public void CloseLink(AmqpLink link)
        {
            link.SafeClose();
        }

        public abstract void Release(string deviceId);

        protected abstract Uri BuildLinkAddress(IotHubConnectionString iotHubConnectionString, string path);

        protected abstract string BuildAudience(IotHubConnectionString iotHubConnectionString, string path);

        protected abstract Task OpenLinkAsync(AmqpObject link, IotHubConnectionString connectionString, string audience, TimeSpan timeout, CancellationToken cancellationToken);

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

        protected virtual void OnCreateSendingLink(IotHubConnectionString connectionString)
        {
            // do nothing. Override in derived classes if necessary
        }

        protected virtual void OnCreateReceivingLink(IotHubConnectionString connectionString)
        {
            // do nothing. Override in derived classes if necessary
        }

        protected virtual async Task<AmqpSession> CreateSessionAsync(TimeSpan timeout, CancellationToken token)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, timeout, token, $"{nameof(IotHubConnection)}.{nameof(CreateSessionAsync)}");

                this.OnCreateSession();

                var timeoutHelper = new TimeoutHelper(timeout);

                AmqpSettings amqpSettings = CreateAmqpSettings();
                TransportBase transport;

                token.ThrowIfCancellationRequested();

                switch (this.AmqpTransportSettings.GetTransportType())
                {
                    case TransportType.Amqp_WebSocket_Only:
                        transport = await this.CreateClientWebSocketTransportAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                        break;
                    case TransportType.Amqp_Tcp_Only:
                        TlsTransportSettings tlsTransportSettings = this.CreateTlsTransportSettings();
                        var amqpTransportInitiator = new AmqpTransportInitiator(amqpSettings, tlsTransportSettings);
                        transport = await amqpTransportInitiator.ConnectTaskAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                        break;
                    default:
                        throw new InvalidOperationException("AmqpTransportSettings must specify WebSocketOnly or TcpOnly");
                }

                var amqpConnectionSettings = new AmqpConnectionSettings()
                {
                    MaxFrameSize = AmqpConstants.DefaultMaxFrameSize,
                    ContainerId = Guid.NewGuid().ToString("N"),
                    HostName = this.hostName
                };

                var amqpConnection = new AmqpConnection(transport, amqpSettings, amqpConnectionSettings);
                try
                {
                    token.ThrowIfCancellationRequested();
                    await amqpConnection.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

                    var sessionSettings = new AmqpSessionSettings()
                    {
                        Properties = new Fields()
                    };

                    AmqpSession amqpSession = amqpConnection.CreateSession(sessionSettings);
                    token.ThrowIfCancellationRequested();
                    await amqpSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

                    // This adds itself to amqpConnection.Extensions
                    var cbsLink = new AmqpCbsLink(amqpConnection);
                    return amqpSession;
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    if (amqpConnection.TerminalException != null)
                    {
                        throw AmqpClientHelper.ToIotHubClientContract(amqpConnection.TerminalException);
                    }

                    amqpConnection.SafeClose(ex);
                    throw;
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, token, $"{nameof(IotHubConnection)}.{nameof(CreateSessionAsync)}");
            }
        }

        protected virtual void OnCreateSession()
        {
            // do nothing. Override in derived classes if necessary
        }

        async Task<ClientWebSocket> CreateClientWebSocketAsync(Uri websocketUri, TimeSpan timeout)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, websocketUri, timeout, $"{nameof(IotHubConnection)}.{nameof(CreateClientWebSocketAsync)}");

                var websocket = new ClientWebSocket();

                // Set SubProtocol to AMQPWSB10
                websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Amqpwsb10);

                // Check if we're configured to use a proxy server
                IWebProxy webProxy = this.AmqpTransportSettings.Proxy;

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

                if (this.AmqpTransportSettings.ClientCertificate != null)
                {
                    websocket.Options.ClientCertificates.Add(this.AmqpTransportSettings.ClientCertificate);
                }

                using (var cancellationTokenSource = new CancellationTokenSource(timeout))
                {
                    await websocket.ConnectAsync(websocketUri, cancellationTokenSource.Token).ConfigureAwait(false);
                }

                return websocket;
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, websocketUri, timeout, $"{nameof(IotHubConnection)}.{nameof(CreateClientWebSocketAsync)}");
            }
        }

        async Task<TransportBase> CreateClientWebSocketTransportAsync(TimeSpan timeout)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, timeout, $"{nameof(IotHubConnection)}.{nameof(CreateClientWebSocketTransportAsync)}");

                var timeoutHelper = new TimeoutHelper(timeout);
                string additionalQueryParams = "";
#if NETSTANDARD1_3
            // NETSTANDARD1_3 implementation doesn't set client certs, so we want to tell the IoT Hub to not ask for them
            additionalQueryParams = "?iothub-no-client-cert=true";
#endif
                Uri websocketUri = new Uri(WebSocketConstants.Scheme + this.hostName + ":" + WebSocketConstants.SecurePort + WebSocketConstants.UriSuffix + additionalQueryParams);
                // Use Legacy WebSocket if it is running on Windows 7 or older. Windows 7/Windows 2008 R2 is version 6.1
#if NET451
            if (Environment.OSVersion.Version.Major < 6 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor <= 1))
            {
                var websocket = await CreateLegacyClientWebSocketAsync(websocketUri, this.AmqpTransportSettings.ClientCertificate, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                return new LegacyClientWebSocketTransport(
                    websocket,
                    this.AmqpTransportSettings.OperationTimeout,
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
                if (Logging.IsEnabled) Logging.Exit(this, timeout, $"{nameof(IotHubConnection)}.{nameof(CreateClientWebSocketTransportAsync)}");
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

        static AmqpSettings CreateAmqpSettings()
        {
            var amqpSettings = new AmqpSettings();

            var amqpTransportProvider = new AmqpTransportProvider();
            amqpTransportProvider.Versions.Add(AmqpVersion_1_0_0);
            amqpSettings.TransportProviders.Add(amqpTransportProvider);

            return amqpSettings;
        }

        protected static AmqpLinkSettings SetLinkSettingsCommonProperties(AmqpLinkSettings linkSettings, TimeSpan timeSpan, ProductInfo productInfo)
        {
            linkSettings.AddProperty(IotHubAmqpProperty.TimeoutName, timeSpan.TotalMilliseconds);

            linkSettings.AddProperty(IotHubAmqpProperty.ClientVersion, productInfo.ToString());

            return linkSettings;
        }

        protected static AmqpLinkSettings SetLinkSettingsCommonPropertiesForMethod(AmqpLinkSettings linkSettings, string corrId)
        {
            linkSettings.AddProperty(IotHubAmqpProperty.ApiVersion, ClientApiVersionHelper.ApiVersionString);
            linkSettings.AddProperty(IotHubAmqpProperty.ChannelCorrelationId, "methods:" + corrId);
            return linkSettings;
        }

        AmqpLinkSettings SetLinkSettingsCommonPropertiesForTwin(AmqpLinkSettings linkSettings, string corrId)
        {
            linkSettings.AddProperty(IotHubAmqpProperty.ApiVersion, ClientApiVersionHelper.ApiVersionString);
            linkSettings.AddProperty(IotHubAmqpProperty.ChannelCorrelationId, "twin:" + corrId);
            return linkSettings;
        }

        TlsTransportSettings CreateTlsTransportSettings()
        {
            var tcpTransportSettings = new TcpTransportSettings()
            {
                Host = this.hostName,
                Port = this.port
            };

            var tlsTransportSettings = new TlsTransportSettings(tcpTransportSettings)
            {
                TargetHost = this.hostName,
                Certificate = null,
                CertificateValidationCallback = this.AmqpTransportSettings.RemoteCertificateValidationCallback ?? OnRemoteCertificateValidation
            };

            if (this.AmqpTransportSettings.ClientCertificate != null)
            {
                tlsTransportSettings.Certificate = this.AmqpTransportSettings.ClientCertificate;
            }

            return tlsTransportSettings;
        }

        public static bool OnRemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
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

        public static ArraySegment<byte> GetNextDeliveryTag(ref int deliveryTag)
        {
            int nextDeliveryTag = Interlocked.Increment(ref deliveryTag);
            return new ArraySegment<byte>(BitConverter.GetBytes(nextDeliveryTag));
        }

        public static ArraySegment<byte> ConvertToDeliveryTag(string lockToken)
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
    }
}
