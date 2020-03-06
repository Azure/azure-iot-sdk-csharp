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
        private const string DisableServerCertificateValidationKeyName = "Microsoft.Azure.Devices.DisableServerCertificateValidation";
        private static readonly AmqpVersion s_amqpVersion_1_0_0 = new AmqpVersion(1, 0, 0);
        private static readonly TimeSpan s_refreshTokenBuffer = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan s_refreshTokenRetryInterval = TimeSpan.FromSeconds(30);
        private static readonly Lazy<bool> s_disableServerCertificateValidation = new Lazy<bool>(InitializeDisableServerCertificateValidation);

        private readonly AccessRights _accessRights;
        private readonly FaultTolerantAmqpObject<AmqpSession> _faultTolerantSession;
#if !NET451
        private readonly IOThreadTimerSlim _refreshTokenTimer;
#else
        private readonly IOThreadTimer _refreshTokenTimer;
#endif
        private readonly bool _useWebSocketOnly;
        private readonly ServiceClientTransportSettings _transportSettings;

        internal static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);
        internal static readonly TimeSpan DefaultOpenTimeout = TimeSpan.FromMinutes(1);

        public IotHubConnection(IotHubConnectionString connectionString, AccessRights accessRights, bool useWebSocketOnly, ServiceClientTransportSettings transportSettings)
        {
            ConnectionString = connectionString;
            _accessRights = accessRights;
            _faultTolerantSession = new FaultTolerantAmqpObject<AmqpSession>(CreateSessionAsync, CloseConnection);
#if !NET451
            _refreshTokenTimer = new IOThreadTimerSlim(s => ((IotHubConnection)s).OnRefreshToken(), this, false);
#else
            _refreshTokenTimer = new IOThreadTimer(s => ((IotHubConnection)s).OnRefreshToken(), this, false);
#endif
            _useWebSocketOnly = useWebSocketOnly;
            _transportSettings = transportSettings;
        }

        internal IotHubConnection(Func<TimeSpan, Task<AmqpSession>> onCreate, Action<AmqpSession> onClose)
        {
            _faultTolerantSession = new FaultTolerantAmqpObject<AmqpSession>(onCreate, onClose);
        }

        internal IotHubConnectionString ConnectionString { get; private set; }

        public Task OpenAsync(TimeSpan timeout)
        {
            return _faultTolerantSession.GetOrCreateAsync(timeout);
        }

        public Task CloseAsync()
        {
            return _faultTolerantSession.CloseAsync();
        }

        public void SafeClose(Exception exception)
        {
            _faultTolerantSession.Close();
        }

        public async Task<SendingAmqpLink> CreateSendingLinkAsync(string path, TimeSpan timeout)
        {
            var timeoutHelper = new TimeoutHelper(timeout);

            AmqpSession session;
            if (!_faultTolerantSession.TryGetOpenedObject(out session))
            {
                session = await _faultTolerantSession.GetOrCreateAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }

            var linkAddress = ConnectionString.BuildLinkAddress(path);

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

            var link = new SendingAmqpLink(linkSettings);
            link.AttachTo(session);

            await OpenLinkAsync(link, timeoutHelper.RemainingTime()).ConfigureAwait(false);

            return link;
        }

        public async Task<ReceivingAmqpLink> CreateReceivingLink(string path, TimeSpan timeout, uint prefetchCount)
        {
            var timeoutHelper = new TimeoutHelper(timeout);

            AmqpSession session;
            if (!_faultTolerantSession.TryGetOpenedObject(out session))
            {
                session = await _faultTolerantSession.GetOrCreateAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }

            var linkAddress = ConnectionString.BuildLinkAddress(path);

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

            var link = new ReceivingAmqpLink(linkSettings);
            link.AttachTo(session);

            await OpenLinkAsync(link, timeoutHelper.RemainingTime()).ConfigureAwait(false);

            return link;
        }

        public void CloseLink(AmqpLink link)
        {
            link.SafeClose();
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
            var timeoutHelper = new TimeoutHelper(timeout);
            _refreshTokenTimer.Cancel();

            AmqpSettings amqpSettings = CreateAmqpSettings();
            TransportBase transport;
            if (_useWebSocketOnly)
            {
                // Try only Amqp transport over WebSocket
                transport = await CreateClientWebSocketTransport(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }
            else
            {
                TlsTransportSettings tlsTransportSettings = CreateTlsTransportSettings();
                var amqpTransportInitiator = new AmqpTransportInitiator(amqpSettings, tlsTransportSettings);
                try
                {
                    transport = await amqpTransportInitiator.ConnectTaskAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                }
                catch (AuthenticationException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    // Amqp transport over TCP failed. Retry Amqp transport over WebSocket
                    if (timeoutHelper.RemainingTime() != TimeSpan.Zero)
                    {
                        transport = await CreateClientWebSocketTransport(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var amqpConnectionSettings = new AmqpConnectionSettings
            {
                MaxFrameSize = AmqpConstants.DefaultMaxFrameSize,
                ContainerId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture), // Use a human readable link name to help with debugging
                HostName = ConnectionString.AmqpEndpoint.Host,
            };

            var amqpConnection = new AmqpConnection(transport, amqpSettings, amqpConnectionSettings);
            await amqpConnection.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

            var sessionSettings = new AmqpSessionSettings
            {
                Properties = new Fields(),
            };

            try
            {
                AmqpSession amqpSession = amqpConnection.CreateSession(sessionSettings);
                await amqpSession.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);

                // This adds itself to amqpConnection.Extensions
                var cbsLink = new AmqpCbsLink(amqpConnection);
                await SendCbsTokenAsync(cbsLink, timeoutHelper.RemainingTime()).ConfigureAwait(false);
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

        private void CloseConnection(AmqpSession amqpSession)
        {
            // Closing the connection also closes any sessions.
            amqpSession.Connection.SafeClose();
        }

        private async Task<ClientWebSocket> CreateClientWebSocketAsync(Uri websocketUri, TimeSpan timeout)
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

            using (var cancellationTokenSource = new CancellationTokenSource(timeout))
            {
                await websocket.ConnectAsync(websocketUri, cancellationTokenSource.Token).ConfigureAwait(false);
            }

            return websocket;
        }

        private static async Task<IotHubClientWebSocket> CreateLegacyClientWebSocketAsync(Uri webSocketUri, TimeSpan timeout)
        {
            var websocket = new IotHubClientWebSocket(WebSocketConstants.SubProtocols.Amqpwsb10);
            await websocket.ConnectAsync(webSocketUri.Host, webSocketUri.Port, WebSocketConstants.Scheme, timeout).ConfigureAwait(false);
            return websocket;
        }

        private async Task<TransportBase> CreateClientWebSocketTransport(TimeSpan timeout)
        {
            var timeoutHelper = new TimeoutHelper(timeout);
            var websocketUri = new Uri(WebSocketConstants.Scheme + ConnectionString.HostName + ":" + WebSocketConstants.SecurePort + WebSocketConstants.UriSuffix);

#if NET451
            // Use Legacy WebSocket if it is running on Windows 7 or older. Windows 7/Windows 2008 R2 is version 6.1
            if (Environment.OSVersion.Version.Major < 6 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor <= 1))
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
            return new ClientWebSocketTransport(
                websocket,
                null,
                null);
#if NET451
            }
#endif
        }

        private static AmqpSettings CreateAmqpSettings()
        {
            var amqpSettings = new AmqpSettings();

            var amqpTransportProvider = new AmqpTransportProvider();
            amqpTransportProvider.Versions.Add(s_amqpVersion_1_0_0);
            amqpSettings.TransportProviders.Add(amqpTransportProvider);

            return amqpSettings;
        }

        private static AmqpLinkSettings SetLinkSettingsCommonProperties(AmqpLinkSettings linkSettings, TimeSpan timeSpan)
        {
            linkSettings.AddProperty(IotHubAmqpProperty.TimeoutName, timeSpan.TotalMilliseconds);
            linkSettings.AddProperty(IotHubAmqpProperty.ClientVersion, Utils.GetClientVersion());

            return linkSettings;
        }

        private TlsTransportSettings CreateTlsTransportSettings()
        {
            var tcpTransportSettings = new TcpTransportSettings
            {
                Host = ConnectionString.HostName,
                Port = ConnectionString.AmqpEndpoint.Port,
            };

            var tlsTransportSettings = new TlsTransportSettings(tcpTransportSettings)
            {
                TargetHost = ConnectionString.HostName,
                Certificate = null, // TODO: add client cert support
                CertificateValidationCallback = OnRemoteCertificateValidation
            };

            return tlsTransportSettings;
        }

        private static async Task OpenLinkAsync(AmqpObject link, TimeSpan timeout)
        {
            var timeoutHelper = new TimeoutHelper(timeout);
            try
            {
                await link.OpenAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (exception.IsFatal())
                {
                    throw;
                }

                link.SafeClose(exception);

                throw;
            }
        }

        private async Task SendCbsTokenAsync(AmqpCbsLink cbsLink, TimeSpan timeout)
        {
            string audience = ConnectionString.AmqpEndpoint.AbsoluteUri;
            string resource = ConnectionString.AmqpEndpoint.AbsoluteUri;
            DateTime expiresAtUtc = await cbsLink.SendTokenAsync(
                ConnectionString,
                ConnectionString.AmqpEndpoint,
                audience,
                resource,
                AccessRightsHelper.AccessRightsToStringArray(_accessRights),
                timeout).ConfigureAwait(false);
            ScheduleTokenRefresh(expiresAtUtc);
        }

        private async void OnRefreshToken()
        {
            if (_faultTolerantSession.TryGetOpenedObject(out AmqpSession amqpSession) && amqpSession != null && !amqpSession.IsClosing())
            {
                AmqpCbsLink cbsLink = amqpSession.Connection.Extensions.Find<AmqpCbsLink>();
                if (cbsLink != null)
                {
                    try
                    {
                        await SendCbsTokenAsync(cbsLink, DefaultOperationTimeout).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        if (Fx.IsFatal(exception))
                        {
                            throw;
                        }

                        _refreshTokenTimer.Set(s_refreshTokenRetryInterval);
                    }
                }
            }
        }

        public static bool OnRemoteCertificateValidation(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (s_disableServerCertificateValidation.Value && sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
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
                throw new ArgumentNullException(nameof(lockToken));
            }

            if (!Guid.TryParse(lockToken, out Guid lockTokenGuid))
            {
                throw new ArgumentException("Should be a valid Guid", nameof(lockToken));
            }

            var deliveryTag = new ArraySegment<byte>(lockTokenGuid.ToByteArray());
            return deliveryTag;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _faultTolerantSession.Dispose();
        }

        private void ScheduleTokenRefresh(DateTime expiresAtUtc)
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
        }
    }
}
