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
using Microsoft.Azure.Devices.Common.Data;
using Microsoft.Azure.Devices.Shared;

#if NET451
using System.Configuration;
#endif

namespace Microsoft.Azure.Devices
{
    internal sealed class IotHubConnection : IDisposable
    {
#if NET451
        private const string DisableServerCertificateValidationKeyName = "Microsoft.Azure.Devices.DisableServerCertificateValidation";
#endif
        private static readonly AmqpVersion s_amqpVersion_1_0_0 = new AmqpVersion(1, 0, 0);
        private static readonly TimeSpan s_refreshTokenBuffer = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan s_refreshTokenRetryInterval = TimeSpan.FromSeconds(30);
        private static readonly Lazy<bool> s_shouldDisableServerCertificateValidation = new Lazy<bool>(InitializeDisableServerCertificateValidation);
        internal static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);
        internal static readonly TimeSpan DefaultOpenTimeout = TimeSpan.FromMinutes(1);

        private readonly bool _useWebSocketOnly;
        private readonly ServiceClientTransportSettings _transportSettings;

        // Disposables
        private FaultTolerantAmqpObject<AmqpSession> _faultTolerantSession;

        private ClientWebSocketTransport _clientWebSocketTransport;
#if !NET451
        private IOThreadTimerSlim _refreshTokenTimer;
#else
        private IOThreadTimer _refreshTokenTimer;
#endif

        public IotHubConnection(IotHubConnectionProperties credential, bool useWebSocketOnly, ServiceClientTransportSettings transportSettings)
        {
#if !NET451
            _refreshTokenTimer = new IOThreadTimerSlim(s => ((IotHubConnection)s).OnRefreshTokenAsync(), this);
#else
            _refreshTokenTimer = new IOThreadTimer(s => ((IotHubConnection)s).OnRefreshTokenAsync(), this, false);
#endif

            Credential = credential;
            _faultTolerantSession = new FaultTolerantAmqpObject<AmqpSession>(CreateSessionAsync, CloseConnection);
            _useWebSocketOnly = useWebSocketOnly;
            _transportSettings = transportSettings;
        }

        internal IotHubConnection(Func<TimeSpan, Task<AmqpSession>> onCreate, Action<AmqpSession> onClose)
        {
            _faultTolerantSession = new FaultTolerantAmqpObject<AmqpSession>(onCreate, onClose);
        }

        internal IotHubConnectionProperties Credential { get; private set; }

        public Task OpenAsync(TimeSpan timeout)
        {
            Logging.Enter(this, timeout, nameof(OpenAsync));

            try
            {
                return _faultTolerantSession.GetOrCreateAsync(timeout);
            }
            finally
            {
                Logging.Exit(this, timeout, nameof(OpenAsync));
            }
        }

        public Task CloseAsync()
        {
            Logging.Enter(this, nameof(CloseAsync));

            try
            {
                return _faultTolerantSession.CloseAsync();
            }
            finally
            {
                Logging.Exit(this, nameof(CloseAsync));
            }
        }

        public async Task<SendingAmqpLink> CreateSendingLinkAsync(string path, TimeSpan timeout)
        {
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

                Logging.Info(this, $"Creating sending link with target={linkSettings.Target}, link name={linkSettings.LinkName}, total link creadit={linkSettings.TotalLinkCredit}");

                var link = new SendingAmqpLink(linkSettings);
                link.AttachTo(session);

                await OpenLinkAsync(link, timeoutHelper.RemainingTime()).ConfigureAwait(false);

                return link;
            }
            finally
            {
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
            Logging.Enter(this, link.Name, nameof(CloseAsync));

            link.SafeClose();

            Logging.Exit(this, link.Name, nameof(CloseAsync));
        }

        public static bool OnRemoteCertificateValidation(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return sslPolicyErrors == SslPolicyErrors.None
                || (s_shouldDisableServerCertificateValidation.Value
                    && sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch);
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
#if NET451
            string value = ConfigurationManager.AppSettings[DisableServerCertificateValidationKeyName];
            if (!string.IsNullOrEmpty(value))
            {
                return bool.Parse(value);
            }
#endif
            return false;
        }

        private async Task<AmqpSession> CreateSessionAsync(TimeSpan timeout)
        {
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

                Logging.Info(this, $"Initialized {nameof(TransportBase)}, ws={_useWebSocketOnly}");

                var amqpConnectionSettings = new AmqpConnectionSettings
                {
                    MaxFrameSize = AmqpConstants.DefaultMaxFrameSize,
                    ContainerId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture), // Use a human readable link name to help with debugging
                    HostName = Credential.AmqpEndpoint.Host,
                };

                var amqpConnection = new AmqpConnection(transport, amqpSettings, amqpConnectionSettings);
                await amqpConnection.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

                Logging.Info(this, $"{nameof(AmqpConnection)} opened.");

                var sessionSettings = new AmqpSessionSettings
                {
                    Properties = new Fields(),
                };

                try
                {
                    AmqpSession amqpSession = amqpConnection.CreateSession(sessionSettings);
                    await amqpSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

                    Logging.Info(this, $"{nameof(AmqpSession)} opened.");

                    // This adds itself to amqpConnection.Extensions
                    var cbsLink = new AmqpCbsLink(amqpConnection);
                    await SendCbsTokenAsync(cbsLink, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    return amqpSession;
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
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
                Logging.Exit(this, timeout, nameof(CreateSessionAsync));
            }
        }

        private void CloseConnection(AmqpSession amqpSession)
        {
            Logging.Enter(this, nameof(CloseConnection));

            // Closing the connection also closes any sessions.
            amqpSession.Connection.SafeClose();

            Logging.Exit(this, nameof(CloseConnection));
        }

        private async Task<ClientWebSocket> CreateClientWebSocketAsync(Uri websocketUri, TimeSpan timeout)
        {
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
                        Logging.Info(this, $"{nameof(CreateClientWebSocketAsync)} Setting ClientWebSocket.Options.Proxy");
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    // .NET Core 2.0 doesn't support proxy. Ignore this setting.
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
                Logging.Exit(this, websocketUri, timeout, nameof(CreateClientWebSocketAsync));
            }
        }

        private static async Task<IotHubClientWebSocket> CreateLegacyClientWebSocketAsync(Uri webSocketUri, TimeSpan timeout)
        {
            Logging.Enter(webSocketUri, timeout, nameof(CreateLegacyClientWebSocketAsync));

            try
            {
                var websocket = new IotHubClientWebSocket(WebSocketConstants.SubProtocols.Amqpwsb10);
                await websocket
                    .ConnectAsync(webSocketUri.Host, webSocketUri.Port, WebSocketConstants.Scheme, timeout)
                    .ConfigureAwait(false);
                return websocket;
            }
            finally
            {
                Logging.Exit(webSocketUri, timeout, nameof(CreateLegacyClientWebSocketAsync));
            }
        }

        private async Task<TransportBase> CreateClientWebSocketTransportAsync(TimeSpan timeout)
        {
            Logging.Enter(this, timeout, nameof(CreateClientWebSocketTransportAsync));

            try
            {
                var timeoutHelper = new TimeoutHelper(timeout);
                var websocketUri = new Uri($"{ WebSocketConstants.Scheme }{ Credential.HostName}:{ WebSocketConstants.SecurePort}{WebSocketConstants.UriSuffix}");

                Logging.Info(this, websocketUri, nameof(CreateClientWebSocketTransportAsync));

#if NET451
                // Use Legacy WebSocket if it is running on Windows 7 or older. Windows 7/Windows 2008 R2 is version 6.1
                if (Environment.OSVersion.Version.Major < 6
                    || (Environment.OSVersion.Version.Major == 6
                        && Environment.OSVersion.Version.Minor <= 1))
                {
                    IotHubClientWebSocket websocket = await CreateLegacyClientWebSocketAsync(
                            websocketUri,
                            timeoutHelper.RemainingTime())
                        .ConfigureAwait(false);

                    return new LegacyClientWebSocketTransport(
                        websocket,
                        DefaultOperationTimeout,
                        null,
                        null);
                }
                else
                {
#endif
                ClientWebSocket websocket = await CreateClientWebSocketAsync(websocketUri, timeoutHelper.RemainingTime()).ConfigureAwait(false);
                return new ClientWebSocketTransport(websocket, null, null);
#if NET451
                }
#endif
            }
            finally
            {
                Logging.Exit(this, timeout, nameof(CreateClientWebSocketTransportAsync));
            }
        }

        private static AmqpSettings CreateAmqpSettings()
        {
            var amqpSettings = new AmqpSettings();

            var amqpTransportProvider = new AmqpTransportProvider();
            amqpTransportProvider.Versions.Add(s_amqpVersion_1_0_0);
            amqpSettings.TransportProviders.Add(amqpTransportProvider);

            Logging.Info(s_amqpVersion_1_0_0, nameof(CreateAmqpSettings));

            return amqpSettings;
        }

        private static AmqpLinkSettings SetLinkSettingsCommonProperties(AmqpLinkSettings linkSettings, TimeSpan timeSpan)
        {
            string clientVersion = Utils.GetClientVersion();
            linkSettings.AddProperty(IotHubAmqpProperty.TimeoutName, timeSpan.TotalMilliseconds);
            linkSettings.AddProperty(IotHubAmqpProperty.ClientVersion, clientVersion);

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

            Logging.Info($"host={tcpTransportSettings.Host}, port={tcpTransportSettings.Port}", nameof(CreateTlsTransportSettings));

            return tlsTransportSettings;
        }

        private static async Task OpenLinkAsync(AmqpObject link, TimeSpan timeout)
        {
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
                Logging.Exit(link, link.State, timeout, nameof(OpenLinkAsync));
            }
        }

        private async Task SendCbsTokenAsync(AmqpCbsLink cbsLink, TimeSpan timeout)
        {
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

            Logging.Exit(this, cbsLink, timeout, nameof(SendCbsTokenAsync));
        }

        private async void OnRefreshTokenAsync()
        {
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
                        Logging.Error(this, ex, nameof(OnRefreshTokenAsync));

                        if (Fx.IsFatal(ex))
                        {
                            throw;
                        }

                        _refreshTokenTimer.Set(s_refreshTokenRetryInterval);
                    }
                }
            }

            Logging.Exit(this, nameof(OnRefreshTokenAsync));
        }

        private void ScheduleTokenRefresh(DateTime expiresAtUtc)
        {
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

                Logging.Info(this, timeFromNow, nameof(ScheduleTokenRefresh));
            }
            finally
            {
                Logging.Exit(this, expiresAtUtc, nameof(ScheduleTokenRefresh));
            }
        }
    }
}
