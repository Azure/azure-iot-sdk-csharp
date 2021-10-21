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
using Microsoft.Azure.Devices.Shared;

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
            {
                Logging.Enter(this, nameof(AmqpSessionClosed));
            }

            Closed?.Invoke(this, e);
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, nameof(AmqpSessionClosed));
            }
        }

        internal Task CloseAsync(TimeSpan timeout)
        {
            return _amqpSession.CloseAsync(timeout);
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
            DeviceIdentity deviceIdentity,
            CancellationToken cancellationToken)
        {
            return await OpenSendingAmqpLinkAsync(
                    deviceIdentity,
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
            DeviceIdentity deviceIdentity,
            CancellationToken cancellationToken)
        {
            return await OpenReceivingAmqpLinkAsync(
                    deviceIdentity,
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
            DeviceIdentity deviceIdentity,
            CancellationToken cancellationToken)
        {
            return await OpenReceivingAmqpLinkAsync(
                    deviceIdentity,
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
            DeviceIdentity deviceIdentity,
            string correlationIdSuffix,
            CancellationToken cancellationToken)
        {
            return await OpenSendingAmqpLinkAsync(
                    deviceIdentity,
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
            DeviceIdentity deviceIdentity,
            string correlationIdSuffix,
            CancellationToken cancellationToken)
        {
            return await OpenReceivingAmqpLinkAsync(
                    deviceIdentity,
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
            DeviceIdentity deviceIdentity,
            string correlationIdSuffix,
            CancellationToken cancellationToken)
        {
            return await OpenReceivingAmqpLinkAsync(
                    deviceIdentity,
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
            DeviceIdentity deviceIdentity,
            string correlationIdSuffix,
            CancellationToken cancellationToken)
        {
            return await OpenSendingAmqpLinkAsync(
                    deviceIdentity,
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
            DeviceIdentity deviceIdentity,
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
                Logging.Enter(typeof(AmqpIotSession), deviceIdentity, nameof(OpenSendingAmqpLinkAsync));
            }

            var amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = linkSuffix,
                Role = false,
                InitialDeliveryCount = 0,
                Target = new Target { Address = BuildLinkAddress(deviceIdentity, deviceTemplate, moduleTemplate) },
                Source = new Source { Address = deviceIdentity.IotHubConnectionString.DeviceId },
                SndSettleMode = senderSettleMode,
                RcvSettleMode = receiverSettleMode,
            };
            //amqpLinkSettings.AddProperty(AmqpIotErrorAdapter.TimeoutName, timeout.TotalMilliseconds);
            amqpLinkSettings.AddProperty(AmqpIotConstants.ClientVersion, deviceIdentity.ProductInfo.ToString());

            if (correlationId != null)
            {
                amqpLinkSettings.AddProperty(AmqpIotConstants.ChannelCorrelationId, correlationId);
            }

            if (!deviceIdentity.AmqpTransportSettings.AuthenticationChain.IsNullOrWhiteSpace())
            {
                amqpLinkSettings.AddProperty(AmqpIotConstants.AuthChain, deviceIdentity.AmqpTransportSettings.AuthenticationChain);
            }

            // This check is added to enable the device or module client to available plug and play features. For devices or modules that pass in the model Id,
            // the SDK will enable plug and play features by setting the modelId to AMQP link settings.
            if (!string.IsNullOrWhiteSpace(deviceIdentity.Options?.ModelId))
            {
                amqpLinkSettings.AddProperty(AmqpIotConstants.ModelId, deviceIdentity.Options.ModelId);
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
                    Logging.Exit(typeof(AmqpIotSession), deviceIdentity, nameof(OpenSendingAmqpLinkAsync));
                }
            }
        }

        private static async Task<AmqpIotReceivingLink> OpenReceivingAmqpLinkAsync(
            DeviceIdentity deviceIdentity,
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
                Logging.Enter(typeof(AmqpIotSession), deviceIdentity, $"{nameof(OpenReceivingAmqpLinkAsync)}");
            }

            uint prefetchCount = deviceIdentity.AmqpTransportSettings.PrefetchCount;

            var amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = linkSuffix,
                Role = true,
                TotalLinkCredit = prefetchCount,
                AutoSendFlow = prefetchCount > 0,
                Source = new Source { Address = BuildLinkAddress(deviceIdentity, deviceTemplate, moduleTemplate) },
                Target = new Target { Address = deviceIdentity.IotHubConnectionString.DeviceId },
                SndSettleMode = senderSettleMode,
                RcvSettleMode = receiverSettleMode,
            };
            //amqpLinkSettings.AddProperty(AmqpIotErrorAdapter.TimeoutName, timeout.TotalMilliseconds);
            amqpLinkSettings.AddProperty(AmqpIotConstants.ClientVersion, deviceIdentity.ProductInfo.ToString());
            amqpLinkSettings.AddProperty(AmqpIotConstants.ApiVersion, ClientApiVersionHelper.ApiVersionString);

            if (!deviceIdentity.AmqpTransportSettings.AuthenticationChain.IsNullOrWhiteSpace())
            {
                amqpLinkSettings.AddProperty(AmqpIotConstants.AuthChain, deviceIdentity.AmqpTransportSettings.AuthenticationChain);
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
                {
                    Logging.Exit(typeof(AmqpIotSession), deviceIdentity, $"{nameof(OpenReceivingAmqpLinkAsync)}");
                }
            }
        }

        private static string BuildLinkAddress(DeviceIdentity deviceIdentity, string deviceTemplate, string moduleTemplate)
        {
            string path = string.IsNullOrEmpty(deviceIdentity.IotHubConnectionString.ModuleId)
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    deviceTemplate,
                    WebUtility.UrlEncode(deviceIdentity.IotHubConnectionString.DeviceId))
                : string.Format(
                    CultureInfo.InvariantCulture,
                    moduleTemplate,
                    WebUtility.UrlEncode(deviceIdentity.IotHubConnectionString.DeviceId), WebUtility.UrlEncode(deviceIdentity.IotHubConnectionString.ModuleId));

            return deviceIdentity.IotHubConnectionString.BuildLinkAddress(path).AbsoluteUri;
        }

        #endregion Common link handling
    }
}
