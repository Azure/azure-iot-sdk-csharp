// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Client;

namespace Microsoft.Azure.Devices
{
    internal sealed class IotHubConnection : IDisposable
    {
        private static readonly AmqpVersion s_amqpVersion_1_0_0 = new(1, 0, 0);
        private static readonly TimeSpan s_refreshTokenBuffer = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan s_refreshTokenRetryInterval = TimeSpan.FromSeconds(30);
        private static readonly Lazy<bool> s_shouldDisableServerCertificateValidation = new(InitializeDisableServerCertificateValidation);
        internal static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);
        internal static readonly TimeSpan DefaultOpenTimeout = TimeSpan.FromMinutes(1);

        private readonly bool _useWebSocketOnly;
        private readonly ServiceClientTransportSettings _transportSettings;
        private readonly IotHubServiceClientOptions _options;

        // Disposables
        private FaultTolerantAmqpObject<AmqpSession> _faultTolerantSession;

        private ClientWebSocketTransport _clientWebSocketTransport;
        private IOThreadTimerSlim _refreshTokenTimer;

        public IotHubConnection(IotHubConnectionProperties credential, bool useWebSocketOnly, ServiceClientTransportSettings transportSettings, IotHubServiceClientOptions options)
        {
            _refreshTokenTimer = new IOThreadTimerSlim(s => ((IotHubConnection)s).OnRefreshTokenAsync(), this);

            Credential = credential;
            _faultTolerantSession = new FaultTolerantAmqpObject<AmqpSession>(CreateSessionAsync, CloseConnection);
            _useWebSocketOnly = useWebSocketOnly;
            _transportSettings = transportSettings;
            _options = options;
        }

        internal IotHubConnection(Func<TimeSpan, Task<AmqpSession>> onCreate, Action<AmqpSession> onClose)
        {
            _faultTolerantSession = new FaultTolerantAmqpObject<AmqpSession>(onCreate, onClose);
        }

        internal IotHubConnectionProperties Credential { get; private set; }

        public Task OpenAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, timeout, nameof(OpenAsync));

            try
            {
                return _faultTolerantSession.GetOrCreateAsync(timeout);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, timeout, nameof(OpenAsync));
            }
        }

        public Task CloseAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(CloseAsync));

            try
            {
                return _faultTolerantSession.CloseAsync();
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(CloseAsync));
            }
        }

        public async Task<SendingAmqpLink> CreateSendingLinkAsync(string path, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, path, timeout, nameof(CreateSendingLinkAsync));

            try
            {
                var timeoutHelper = new TimeoutHelper(timeout);

                if (!_faultTolerantSession.TryGetOpenedObject(out AmqpSession session))
                {
                    session = await _faultTolerantSession.GetOrCreateAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                }

                Uri linkAddress = Credential.BuildLinkAddress(path);

                var linkSettings = new AmqpLinkSettings
                {
                    Role = false,
                    InitialDeliveryCount = 0,
                    Target = new Target { Address = linkAddress.AbsoluteUri },
                    SndSettleMode = null, // SenderSettleMode.Unsettled (null as it is the default and to avoid bytes on the wire)
                    RcvSettleMode = null, // (byte)ReceiverSettleMode.First (null as it is the default and to avoid bytes on the wire)
                    LinkName = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture), // Use a human readable link name to help with debugging
                };

                SetLinkSettingsCommonProperties(linkSettings, timeoutHelper.RemainingTime());

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Creating sending link with target={linkSettings.Target}, link name={linkSettings.LinkName}, total link creadit={linkSettings.TotalLinkCredit}");

                var link = new SendingAmqpLink(linkSettings);
                link.AttachTo(session);

                await OpenLinkAsync(link, timeoutHelper.RemainingTime()).ConfigureAwait(false);

                return link;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, path, timeout, nameof(CreateSendingLinkAsync));
            }
        }

        public async Task<ReceivingAmqpLink> CreateReceivingLinkAsync(string path, TimeSpan timeout, uint prefetchCount)
        {
            Logging.Enter(this, path, timeout, prefetchCount, nameof(CreateReceivingLinkAsync));

            try
            {
                var timeoutHelper = new TimeoutHelper(timeout);

                if (!_faultTolerantSession.TryGetOpenedObject(out AmqpSession session))
                {
                    session = await _faultTolerantSession.GetOrCreateAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                }

                Uri linkAddress = Credential.BuildLinkAddress(path);

                var linkSettings = new AmqpLinkSettings
                {
                    Role = true,
                    TotalLinkCredit = prefetchCount,
                    AutoSendFlow = prefetchCount > 0,
                    Source = new Source { Address = linkAddress.AbsoluteUri },
                    SndSettleMode = null, // SenderSettleMode.Unsettled (null as it is the default and to avoid bytes on the wire)
                    RcvSettleMode = (byte)ReceiverSettleMode.Second,
                    LinkName = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture), // Use a human readable link name to help with debugging
                };

                SetLinkSettingsCommonProperties(linkSettings, timeoutHelper.RemainingTime());

                Logging.Info(this, $"Creating receiving link with source={linkSettings.Source}, link name={linkSettings.LinkName}, total link creadit={linkSettings.TotalLinkCredit}");

                var link = new ReceivingAmqpLink(linkSettings);
                link.AttachTo(session);

                await OpenLinkAsync(link, timeoutHelper.RemainingTime()).ConfigureAwait(false);

                return link;
            }
            finally
            {
                Logging.Exit(this, path, timeout, prefetchCount, nameof(CreateReceivingLinkAsync));
            }
        }

        public void CloseLink(AmqpLink link)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, link.Name, nameof(CloseAsync));

            link.SafeClose();

            if (Logging.IsEnabled)
                Logging.Exit(this, link.Name, nameof(CloseAsync));
        }

        public static bool OnRemoteCertificateValidation(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return sslPolicyErrors == SslPolicyErrors.None
                || s_shouldDisableServerCertificateValidation.Value
                && sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch;
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
                throw new ArgumentNullException(nameof(lockToken));
            }

            if (!Guid.TryParse(lockToken, out Guid lockTokenGuid))
            {
                throw new ArgumentException("Should be a valid GUID", nameof(lockToken));
            }

            var deliveryTag = new ArraySegment<byte>(lockTokenGuid.ToByteArray());
            return deliveryTag;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _faultTolerantSession?.Dispose();
            _faultTolerantSession = null;

            _refreshTokenTimer?.Dispose();
            _refreshTokenTimer = null;

            _clientWebSocketTransport?.Dispose();
            _clientWebSocketTransport = null;
        }

        private static bool InitializeDisableServerCertificateValidation()
        {
            return false;
        }

        private async Task<AmqpSession> CreateSessionAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, timeout, nameof(CreateSessionAsync));

            TransportBase transport = null;

            try
            {
                var timeoutHelper = new TimeoutHelper(timeout);
                _refreshTokenTimer.Cancel();

                AmqpSettings amqpSettings = CreateAmqpSettings();
                if (_useWebSocketOnly)
                {
                    // Try only AMQP transport over WebSocket
                    transport = _clientWebSocketTransport = (ClientWebSocketTransport)await CreateClientWebSocketTransportAsync(timeoutHelper.RemainingTime())
                        .ConfigureAwait(false);
                }
                else
                {
                    TlsTransportSettings tlsTransportSettings = CreateTlsTransportSettings();
                    var amqpTransportInitiator = new AmqpTransportInitiator(amqpSettings, tlsTransportSettings);
                    try
                    {
                        transport = await amqpTransportInitiator.ConnectTaskAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    }
                    catch (Exception e) when (!(e is AuthenticationException))
                    {
                        if (Logging.IsEnabled)
                            Logging.Error(this, e, nameof(CreateSessionAsync));

                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }

                        // AMQP transport over TCP failed. Retry AMQP transport over WebSocket
                        if (timeoutHelper.RemainingTime() != TimeSpan.Zero)
                        {
                            transport = _clientWebSocketTransport = (ClientWebSocketTransport)await CreateClientWebSocketTransportAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Initialized {nameof(TransportBase)}, ws={_useWebSocketOnly}");

                var amqpConnectionSettings = new AmqpConnectionSettings
                {
                    MaxFrameSize = AmqpConstants.DefaultMaxFrameSize,
                    ContainerId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture), // Use a human readable link name to help with debugging
                    HostName = Credential.AmqpEndpoint.Host,
                };

                var amqpConnection = new AmqpConnection(transport, amqpSettings, amqpConnectionSettings);
                await amqpConnection.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"{nameof(AmqpConnection)} opened.");

                var sessionSettings = new AmqpSessionSettings
                {
                    Properties = new Fields(),
                };

                try
                {
                    AmqpSession amqpSession = amqpConnection.CreateSession(sessionSettings);
                    await amqpSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

                    if (Logging.IsEnabled)
                        Logging.Info(this, $"{nameof(AmqpSession)} opened.");

                    // This adds itself to amqpConnection.Extensions
                    var cbsLink = new AmqpCbsLink(amqpConnection);
                    await SendCbsTokenAsync(cbsLink, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    return amqpSession;
                }
                catch (Exception ex) when (!Fx.IsFatal(ex))
                {
                    if (Logging.IsEnabled)
                        Logging.Error(this, ex, nameof(CreateSessionAsync));

                    _clientWebSocketTransport?.Dispose();
                    _clientWebSocketTransport = null;

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
                if (Logging.IsEnabled)
                    Logging.Exit(this, timeout, nameof(CreateSessionAsync));
            }
        }

        private void CloseConnection(AmqpSession amqpSession)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(CloseConnection));

            // Closing the connection also closes any sessions.
            amqpSession.Connection.SafeClose();

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(CloseConnection));
        }

        private async Task<ClientWebSocket> CreateClientWebSocketAsync(Uri websocketUri, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, websocketUri, timeout, nameof(CreateClientWebSocketAsync));

            try
            {
                var websocket = new ClientWebSocket();

                // Set SubProtocol to AMQPWSB10
                websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Amqpwsb10);

                // Check if we're configured to use a proxy server
                IWebProxy webProxy = _transportSettings.AmqpProxy;

                try
                {
                    if (webProxy != DefaultWebProxySettings.Instance)
                    {
                        // Configure proxy server
                        websocket.Options.Proxy = webProxy;
                        if (Logging.IsEnabled)
                            Logging.Info(this, $"{nameof(CreateClientWebSocketAsync)} Setting ClientWebSocket.Options.Proxy");
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    // .NET Core 2.0 doesn't support proxy. Ignore this setting.
                    if (Logging.IsEnabled)
                        Logging.Error(this, $"{nameof(CreateClientWebSocketAsync)} PlatformNotSupportedException thrown as .NET Core 2.0 doesn't support proxy");
                }

                using (var cancellationTokenSource = new CancellationTokenSource(timeout))
                {
                    await websocket.ConnectAsync(websocketUri, cancellationTokenSource.Token).ConfigureAwait(false);
                }

                return websocket;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, websocketUri, timeout, nameof(CreateClientWebSocketAsync));
            }
        }

        private async Task<IotHubClientWebSocket> CreateLegacyClientWebSocketAsync(Uri webSocketUri, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
                Logging.Enter(webSocketUri, timeout, nameof(CreateLegacyClientWebSocketAsync));

            try
            {
                var websocket = new IotHubClientWebSocket(WebSocketConstants.SubProtocols.Amqpwsb10, _options);
                await websocket
                    .ConnectAsync(webSocketUri.Host, webSocketUri.Port, WebSocketConstants.Scheme, timeout)
                    .ConfigureAwait(false);
                return websocket;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(webSocketUri, timeout, nameof(CreateLegacyClientWebSocketAsync));
            }
        }

        private async Task<TransportBase> CreateClientWebSocketTransportAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, timeout, nameof(CreateClientWebSocketTransportAsync));

            try
            {
                var timeoutHelper = new TimeoutHelper(timeout);
                var websocketUri = new Uri($"{WebSocketConstants.Scheme}{Credential.HostName}:{WebSocketConstants.SecurePort}{WebSocketConstants.UriSuffix}");

                if (Logging.IsEnabled)
                    Logging.Info(this, websocketUri, nameof(CreateClientWebSocketTransportAsync));

                ClientWebSocket websocket = await CreateClientWebSocketAsync(websocketUri, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                return new ClientWebSocketTransport(websocket, null, null);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, timeout, nameof(CreateClientWebSocketTransportAsync));
            }
        }

        private static AmqpSettings CreateAmqpSettings()
        {
            var amqpSettings = new AmqpSettings();

            var amqpTransportProvider = new AmqpTransportProvider();
            amqpTransportProvider.Versions.Add(s_amqpVersion_1_0_0);
            amqpSettings.TransportProviders.Add(amqpTransportProvider);

            if (Logging.IsEnabled)
                Logging.Info(s_amqpVersion_1_0_0, nameof(CreateAmqpSettings));

            return amqpSettings;
        }

        private static AmqpLinkSettings SetLinkSettingsCommonProperties(AmqpLinkSettings linkSettings, TimeSpan timeSpan)
        {
            string clientVersion = Utils.GetClientVersion();
            linkSettings.AddProperty(IotHubAmqpProperty.TimeoutName, timeSpan.TotalMilliseconds);
            linkSettings.AddProperty(IotHubAmqpProperty.ClientVersion, clientVersion);

            if (Logging.IsEnabled)
                Logging.Info(clientVersion, nameof(SetLinkSettingsCommonProperties));

            return linkSettings;
        }

        private TlsTransportSettings CreateTlsTransportSettings()
        {
            var tcpTransportSettings = new TcpTransportSettings
            {
                Host = Credential.HostName,
                Port = Credential.AmqpEndpoint.Port,
            };

            var tlsTransportSettings = new TlsTransportSettings(tcpTransportSettings)
            {
                TargetHost = Credential.HostName,
                Certificate = null, // TODO: add client cert support
                CertificateValidationCallback = OnRemoteCertificateValidation
            };

            if (Logging.IsEnabled)
                Logging.Info($"host={tcpTransportSettings.Host}, port={tcpTransportSettings.Port}", nameof(CreateTlsTransportSettings));

            return tlsTransportSettings;
        }

        private static async Task OpenLinkAsync(AmqpObject link, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
                Logging.Enter(link, link.State, timeout, nameof(OpenLinkAsync));

            try
            {
                var timeoutHelper = new TimeoutHelper(timeout);
                try
                {
                    await link.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(link, exception, nameof(OpenLinkAsync));

                    if (exception.IsFatal())
                    {
                        throw;
                    }

                    link.SafeClose(exception);

                    throw;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(link, link.State, timeout, nameof(OpenLinkAsync));
            }
        }

        private async Task SendCbsTokenAsync(AmqpCbsLink cbsLink, TimeSpan timeout)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cbsLink, timeout, nameof(SendCbsTokenAsync));

            string audience = Credential.AmqpEndpoint.AbsoluteUri;
            string resource = Credential.AmqpEndpoint.AbsoluteUri;
            DateTime expiresAtUtc = await cbsLink
                .SendTokenAsync(
                    Credential,
                    Credential.AmqpEndpoint,
                    audience,
                    resource,
                    Credential.AmqpAudience.ToArray(),
                    timeout)
                .ConfigureAwait(false);
            ScheduleTokenRefresh(expiresAtUtc);

            if (Logging.IsEnabled)
                Logging.Exit(this, cbsLink, timeout, nameof(SendCbsTokenAsync));
        }

        private async void OnRefreshTokenAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(OnRefreshTokenAsync));

            if (_faultTolerantSession.TryGetOpenedObject(out AmqpSession amqpSession)
                && amqpSession != null
                && !amqpSession.IsClosing())
            {
                AmqpCbsLink cbsLink = amqpSession.Connection.Extensions.Find<AmqpCbsLink>();
                if (cbsLink != null)
                {
                    try
                    {
                        await SendCbsTokenAsync(cbsLink, DefaultOperationTimeout).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (Logging.IsEnabled)
                            Logging.Error(this, ex, nameof(OnRefreshTokenAsync));

                        if (Fx.IsFatal(ex))
                        {
                            throw;
                        }

                        _refreshTokenTimer.Set(s_refreshTokenRetryInterval);
                    }
                }
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(OnRefreshTokenAsync));
        }

        private void ScheduleTokenRefresh(DateTime expiresAtUtc)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, expiresAtUtc, nameof(ScheduleTokenRefresh));

            try
            {
                if (expiresAtUtc == DateTime.MaxValue)
                {
                    return;
                }

                TimeSpan timeFromNow = expiresAtUtc.Subtract(s_refreshTokenBuffer).Subtract(DateTime.UtcNow);
                if (timeFromNow > TimeSpan.Zero)
                {
                    _refreshTokenTimer.Set(timeFromNow);
                }

                if (Logging.IsEnabled)
                    Logging.Info(this, timeFromNow, nameof(ScheduleTokenRefresh));
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, expiresAtUtc, nameof(ScheduleTokenRefresh));
            }
        }
    }
}
