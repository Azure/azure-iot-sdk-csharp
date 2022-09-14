// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotSession
    {
        public event EventHandler Closed;

        private readonly AmqpSession _amqpSession;

        public AmqpIotSession(AmqpSession amqpSession)
        {
            _amqpSession = amqpSession;
            _amqpSession.Closed += AmqpSessionClosed;
        }

        private void AmqpSessionClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(AmqpSessionClosed));

            Closed?.Invoke(this, e);

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(AmqpSessionClosed));
        }

        internal Task CloseAsync(CancellationToken cancellationToken)
        {
            return _amqpSession.CloseAsync(cancellationToken);
        }

        internal void SafeClose()
        {
            _amqpSession.SafeClose();
        }

        internal bool IsClosing()
        {
            return _amqpSession.IsClosing();
        }

        #region Telemetry links

        internal async Task<AmqpIotSendingLink> OpenTelemetrySenderLinkAsync(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation clientInformation,
            IotHubClientAmqpSettings amqpSettings,
            CancellationToken cancellationToken)
        {
            return await OpenSendingAmqpLinkAsync(
                    connectionCredentials,
                    clientInformation,
                    amqpSettings,
                    _amqpSession,
                    null,
                    null,
                    CommonConstants.DeviceEventPathTemplate,
                    CommonConstants.ModuleEventPathTemplate,
                    AmqpIotConstants.TelemetrySenderLinkSuffix,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        internal async Task<AmqpIotReceivingLink> OpenMessageReceiverLinkAsync(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation clientInformation,
            IotHubClientAmqpSettings amqpSettings,
            CancellationToken cancellationToken)
        {
            return await OpenReceivingAmqpLinkAsync(
                    connectionCredentials,
                    clientInformation,
                    amqpSettings,
                    _amqpSession,
                    null,
                    (byte)ReceiverSettleMode.Second,
                    CommonConstants.DeviceBoundPathTemplate,
                    CommonConstants.ModuleBoundPathTemplate,
                    AmqpIotConstants.TelemetryReceiveLinkSuffix,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        #endregion Telemetry links

        #region EventLink

        internal async Task<AmqpIotReceivingLink> OpenEventsReceiverLinkAsync(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation clientInformation,
            IotHubClientAmqpSettings amqpSettings,
            CancellationToken cancellationToken)
        {
            return await OpenReceivingAmqpLinkAsync(
                    connectionCredentials,
                    clientInformation,
                    amqpSettings,
                    _amqpSession,
                    null,
                    (byte)ReceiverSettleMode.First,
                    CommonConstants.DeviceEventPathTemplate,
                    CommonConstants.ModuleEventPathTemplate,
                    AmqpIotConstants.EventsReceiverLinkSuffix,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        #endregion EventLink

        #region MethodLink

        internal async Task<AmqpIotSendingLink> OpenMethodsSenderLinkAsync(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation clientInformation,
            IotHubClientAmqpSettings amqpSettings,
            string correlationIdSuffix,
            CancellationToken cancellationToken)
        {
            return await OpenSendingAmqpLinkAsync(
                    connectionCredentials,
                    clientInformation,
                    amqpSettings,
                    _amqpSession,
                    (byte)SenderSettleMode.Settled,
                    (byte)ReceiverSettleMode.First,
                    CommonConstants.DeviceMethodPathTemplate,
                    CommonConstants.ModuleMethodPathTemplate,
                    AmqpIotConstants.MethodsSenderLinkSuffix,
                    AmqpIotConstants.MethodCorrelationIdPrefix + correlationIdSuffix,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        internal async Task<AmqpIotReceivingLink> OpenMethodsReceiverLinkAsync(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation clientInformation,
            IotHubClientAmqpSettings amqpSettings,
            string correlationIdSuffix,
            CancellationToken cancellationToken)
        {
            return await OpenReceivingAmqpLinkAsync(
                    connectionCredentials,
                    clientInformation,
                    amqpSettings,
                    _amqpSession,
                    (byte)SenderSettleMode.Settled,
                    (byte)ReceiverSettleMode.First,
                    CommonConstants.DeviceMethodPathTemplate,
                    CommonConstants.ModuleMethodPathTemplate,
                    AmqpIotConstants.MethodsReceiverLinkSuffix,
                    AmqpIotConstants.MethodCorrelationIdPrefix + correlationIdSuffix,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        #endregion MethodLink

        #region TwinLink

        internal async Task<AmqpIotReceivingLink> OpenTwinReceiverLinkAsync(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation clientInformation,
            IotHubClientAmqpSettings amqpSettings,
            string correlationIdSuffix,
            CancellationToken cancellationToken)
        {
            return await OpenReceivingAmqpLinkAsync(
                    connectionCredentials,
                    clientInformation,
                    amqpSettings,
                    _amqpSession,
                    (byte)SenderSettleMode.Settled,
                    (byte)ReceiverSettleMode.First,
                    CommonConstants.DeviceTwinPathTemplate,
                    CommonConstants.ModuleTwinPathTemplate,
                    AmqpIotConstants.TwinReceiverLinkSuffix,
                    AmqpIotConstants.TwinCorrelationIdPrefix + correlationIdSuffix,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        internal async Task<AmqpIotSendingLink> OpenTwinSenderLinkAsync(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation clientInformation,
            IotHubClientAmqpSettings amqpSettings,
            string correlationIdSuffix,
            CancellationToken cancellationToken)
        {
            return await OpenSendingAmqpLinkAsync(
                    connectionCredentials,
                    clientInformation,
                    amqpSettings,
                    _amqpSession,
                    (byte)SenderSettleMode.Settled,
                    (byte)ReceiverSettleMode.First,
                    CommonConstants.DeviceTwinPathTemplate,
                    CommonConstants.ModuleTwinPathTemplate,
                    AmqpIotConstants.TwinSenderLinkSuffix,
                    AmqpIotConstants.TwinCorrelationIdPrefix + correlationIdSuffix,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        #endregion TwinLink

        #region Common link handling

        private static async Task<AmqpIotSendingLink> OpenSendingAmqpLinkAsync(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation clientInformation,
            IotHubClientAmqpSettings amqpSettings,
            AmqpSession amqpSession,
            byte? senderSettleMode,
            byte? receiverSettleMode,
            string deviceTemplate,
            string moduleTemplate,
            string linkSuffix,
            string correlationId,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(typeof(AmqpIotSession), connectionCredentials, nameof(OpenSendingAmqpLinkAsync));
            }

            var amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = linkSuffix,
                Role = false,
                InitialDeliveryCount = 0,
                Target = new Target { Address = BuildLinkAddress(connectionCredentials, deviceTemplate, moduleTemplate) },
                Source = new Source { Address = connectionCredentials.DeviceId },
                SndSettleMode = senderSettleMode,
                RcvSettleMode = receiverSettleMode,
            };

            amqpLinkSettings.AddProperty(AmqpIotConstants.ClientVersion, clientInformation.ProductInfo?.ToString());

            if (correlationId != null)
            {
                amqpLinkSettings.AddProperty(AmqpIotConstants.ChannelCorrelationId, correlationId);
            }

            if (!amqpSettings.AuthenticationChain.IsNullOrWhiteSpace())
            {
                amqpLinkSettings.AddProperty(AmqpIotConstants.AuthChain, amqpSettings.AuthenticationChain);
            }

            // This check is added to enable the device or module client to available plug and play features. For devices or modules that pass in the model Id,
            // the SDK will enable plug and play features by setting the modelId to AMQP link settings.
            if (!clientInformation.ModelId.IsNullOrWhiteSpace())
            {
                amqpLinkSettings.AddProperty(AmqpIotConstants.ModelId, clientInformation.ModelId);
            }

            amqpLinkSettings.AddProperty(AmqpIotConstants.ApiVersion, ClientApiVersionHelper.ApiVersionString);

            try
            {
                var sendingLink = new SendingAmqpLink(amqpLinkSettings);
                sendingLink.AttachTo(amqpSession);
                await sendingLink.OpenAsync(cancellationToken).ConfigureAwait(false);
                return new AmqpIotSendingLink(sendingLink);
            }
            catch (Exception ex) when (!Fx.IsFatal(ex))
            {
                Exception iotEx = AmqpIotExceptionAdapter.ConvertToIotHubException(ex, amqpSession);
                if (ReferenceEquals(ex, iotEx))
                {
                    throw;
                }

                if (iotEx is IotHubClientException hubEx && hubEx.InnerException is AmqpException)
                {
                    amqpSession.SafeClose();
                    hubEx.ErrorCode = IotHubErrorCode.NetworkErrors;
                    throw hubEx;
                }

                throw iotEx;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(typeof(AmqpIotSession), connectionCredentials, nameof(OpenSendingAmqpLinkAsync));
            }
        }

        private static async Task<AmqpIotReceivingLink> OpenReceivingAmqpLinkAsync(
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation clientInformation,
            IotHubClientAmqpSettings amqpSettings,
            AmqpSession amqpSession,
            byte? senderSettleMode,
            byte? receiverSettleMode,
            string deviceTemplate,
            string moduleTemplate,
            string linkSuffix,
            string correlationId,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(typeof(AmqpIotSession), connectionCredentials, $"{nameof(OpenReceivingAmqpLinkAsync)}");

            uint prefetchCount = amqpSettings.PrefetchCount;

            var amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = linkSuffix,
                Role = true,
                TotalLinkCredit = prefetchCount,
                AutoSendFlow = prefetchCount > 0,
                Source = new Source { Address = BuildLinkAddress(connectionCredentials, deviceTemplate, moduleTemplate) },
                Target = new Target { Address = connectionCredentials.DeviceId },
                SndSettleMode = senderSettleMode,
                RcvSettleMode = receiverSettleMode,
            };

            amqpLinkSettings.AddProperty(AmqpIotConstants.ClientVersion, clientInformation.ProductInfo?.ToString());
            amqpLinkSettings.AddProperty(AmqpIotConstants.ApiVersion, ClientApiVersionHelper.ApiVersionString);

            if (!amqpSettings.AuthenticationChain.IsNullOrWhiteSpace())
            {
                amqpLinkSettings.AddProperty(AmqpIotConstants.AuthChain, amqpSettings.AuthenticationChain);
            }

            if (correlationId != null)
            {
                amqpLinkSettings.AddProperty(AmqpIotConstants.ChannelCorrelationId, correlationId);
            }

            try
            {
                var receivingLink = new ReceivingAmqpLink(amqpLinkSettings);
                receivingLink.AttachTo(amqpSession);
                await receivingLink.OpenAsync(cancellationToken).ConfigureAwait(false);
                return new AmqpIotReceivingLink(receivingLink);
            }
            catch (Exception ex) when (!Fx.IsFatal(ex))
            {
                Exception iotEx = AmqpIotExceptionAdapter.ConvertToIotHubException(ex, amqpSession);
                if (ReferenceEquals(ex, iotEx))
                {
                    throw;
                }

                if (iotEx is IotHubClientException hubEx && hubEx.InnerException is AmqpException)
                {
                    amqpSession.SafeClose();
                    hubEx.ErrorCode = IotHubErrorCode.NetworkErrors;
                    throw hubEx;
                }

                throw iotEx;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(typeof(AmqpIotSession), connectionCredentials, $"{nameof(OpenReceivingAmqpLinkAsync)}");
            }
        }

        private static string BuildLinkAddress(IConnectionCredentials connectionCredentials, string deviceTemplate, string moduleTemplate)
        {
            string path = connectionCredentials.ModuleId.IsNullOrWhiteSpace()
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    deviceTemplate,
                    WebUtility.UrlEncode(connectionCredentials.DeviceId))
                : string.Format(
                    CultureInfo.InvariantCulture,
                    moduleTemplate,
                    WebUtility.UrlEncode(connectionCredentials.DeviceId), WebUtility.UrlEncode(connectionCredentials.ModuleId));

            Uri amqpEndpoint = new UriBuilder(
                CommonConstants.AmqpsScheme,
                connectionCredentials.HostName,
                CommonConstants.DefaultAmqpSecurePort)
                {
                    Path = path,
                }.Uri;

            return amqpEndpoint.AbsoluteUri;
        }

        #endregion Common link handling
    }
}
