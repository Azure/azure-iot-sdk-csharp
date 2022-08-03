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
using Microsoft.Azure.Devices.Client.Transport.Amqp;

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
            IClientIdentity clientIdentity,
            CancellationToken cancellationToken)
        {
            return await OpenSendingAmqpLinkAsync(
                    clientIdentity,
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
            IClientIdentity clientIdentity,
            CancellationToken cancellationToken)
        {
            return await OpenReceivingAmqpLinkAsync(
                    clientIdentity,
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
            IClientIdentity clientIdentity,
            CancellationToken cancellationToken)
        {
            return await OpenReceivingAmqpLinkAsync(
                    clientIdentity,
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
            IClientIdentity clientIdentity,
            string correlationIdSuffix,
            CancellationToken cancellationToken)
        {
            return await OpenSendingAmqpLinkAsync(
                    clientIdentity,
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
            IClientIdentity clientIdentity,
            string correlationIdSuffix,
            CancellationToken cancellationToken)
        {
            return await OpenReceivingAmqpLinkAsync(
                    clientIdentity,
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
            IClientIdentity clientIdentity,
            string correlationIdSuffix,
            CancellationToken cancellationToken)
        {
            return await OpenReceivingAmqpLinkAsync(
                    clientIdentity,
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
            IClientIdentity clientIdentity,
            string correlationIdSuffix,
            CancellationToken cancellationToken)
        {
            return await OpenSendingAmqpLinkAsync(
                    clientIdentity,
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
            IClientIdentity clientIdentity,
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
                Logging.Enter(typeof(AmqpIotSession), clientIdentity, nameof(OpenSendingAmqpLinkAsync));
            }

            var amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = linkSuffix,
                Role = false,
                InitialDeliveryCount = 0,
                Target = new Target { Address = BuildLinkAddress(clientIdentity, deviceTemplate, moduleTemplate) },
                Source = new Source { Address = clientIdentity.DeviceId },
                SndSettleMode = senderSettleMode,
                RcvSettleMode = receiverSettleMode,
            };

            amqpLinkSettings.AddProperty(AmqpIotConstants.ClientVersion, clientIdentity.ClientOptions.ProductInfo.ToString());

            if (correlationId != null)
            {
                amqpLinkSettings.AddProperty(AmqpIotConstants.ChannelCorrelationId, correlationId);
            }

            var amqpSettings = clientIdentity.ClientOptions.TransportSettings as IotHubClientAmqpSettings;
            if (!amqpSettings.AuthenticationChain.IsNullOrWhiteSpace())
            {
                amqpLinkSettings.AddProperty(AmqpIotConstants.AuthChain, amqpSettings.AuthenticationChain);
            }

            // This check is added to enable the device or module client to available plug and play features. For devices or modules that pass in the model Id,
            // the SDK will enable plug and play features by setting the modelId to AMQP link settings.
            if (!string.IsNullOrWhiteSpace(clientIdentity.ClientOptions?.ModelId))
            {
                amqpLinkSettings.AddProperty(AmqpIotConstants.ModelId, clientIdentity.ClientOptions.ModelId);
            }

            amqpLinkSettings.AddProperty(AmqpIotConstants.ApiVersion, ClientApiVersionHelper.ApiVersionString);

            try
            {
                var sendingLink = new SendingAmqpLink(amqpLinkSettings);
                sendingLink.AttachTo(amqpSession);
                await sendingLink.OpenAsync(cancellationToken).ConfigureAwait(false);
                return new AmqpIotSendingLink(sendingLink);
            }
            catch (Exception e) when (!e.IsFatal())
            {
                Exception ex = AmqpIotExceptionAdapter.ConvertToIotHubException(e, amqpSession);
                if (ReferenceEquals(e, ex))
                {
                    throw;
                }
                else
                {
                    if (ex is AmqpIotResourceException)
                    {
                        amqpSession.SafeClose();
                        throw new IotHubCommunicationException(ex.Message, ex);
                    }
                    throw ex;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(typeof(AmqpIotSession), clientIdentity, nameof(OpenSendingAmqpLinkAsync));
                }
            }
        }

        private static async Task<AmqpIotReceivingLink> OpenReceivingAmqpLinkAsync(
            IClientIdentity clientIdentity,
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
                Logging.Enter(typeof(AmqpIotSession), clientIdentity, $"{nameof(OpenReceivingAmqpLinkAsync)}");

            var amqpSettings = clientIdentity.ClientOptions.TransportSettings as IotHubClientAmqpSettings;
            uint prefetchCount = amqpSettings.PrefetchCount;

            var amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = linkSuffix,
                Role = true,
                TotalLinkCredit = prefetchCount,
                AutoSendFlow = prefetchCount > 0,
                Source = new Source { Address = BuildLinkAddress(clientIdentity, deviceTemplate, moduleTemplate) },
                Target = new Target { Address = clientIdentity.DeviceId },
                SndSettleMode = senderSettleMode,
                RcvSettleMode = receiverSettleMode,
            };

            amqpLinkSettings.AddProperty(AmqpIotConstants.ClientVersion, clientIdentity.ClientOptions.ProductInfo.ToString());
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
            catch (Exception e) when (!e.IsFatal())
            {
                Exception ex = AmqpIotExceptionAdapter.ConvertToIotHubException(e, amqpSession);
                if (ReferenceEquals(e, ex))
                {
                    throw;
                }
                else
                {
                    if (ex is AmqpIotResourceException)
                    {
                        amqpSession.SafeClose();
                        throw new IotHubCommunicationException(ex.Message, ex);
                    }
                    throw ex;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(typeof(AmqpIotSession), clientIdentity, $"{nameof(OpenReceivingAmqpLinkAsync)}");
            }
        }

        private static string BuildLinkAddress(IClientIdentity clientIdentity, string deviceTemplate, string moduleTemplate)
        {
            string path = string.IsNullOrEmpty(clientIdentity.ModuleId)
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    deviceTemplate,
                    WebUtility.UrlEncode(clientIdentity.DeviceId))
                : string.Format(
                    CultureInfo.InvariantCulture,
                    moduleTemplate,
                    WebUtility.UrlEncode(clientIdentity.DeviceId), WebUtility.UrlEncode(clientIdentity.ModuleId));

            Uri amqpEndpoint = new UriBuilder(CommonConstants.AmqpsScheme, clientIdentity.HostName, CommonConstants.DefaultAmqpSecurePort)
            {
                Path = path,
            }.Uri;

            return amqpEndpoint.AbsoluteUri;
        }

        #endregion Common link handling
    }
}
