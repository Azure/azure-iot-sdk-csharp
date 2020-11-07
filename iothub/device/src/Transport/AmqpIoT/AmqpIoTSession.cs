// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTSession
    {
        public event EventHandler Closed;

        private readonly AmqpSession _amqpSession;

        public AmqpIoTSession(AmqpSession amqpSession)
        {
            _amqpSession = amqpSession;
            _amqpSession.Closed += AmqpSessionClosed;
        }

        private void AmqpSessionClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, $"{nameof(AmqpSessionClosed)}");
            }

            Closed?.Invoke(this, e);
            if (Logging.IsEnabled)
            {
                Logging.Exit(this, $"{nameof(AmqpSessionClosed)}");
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

        internal async Task<AmqpIoTSendingLink> OpenTelemetrySenderLinkAsync(
            DeviceIdentity deviceIdentity,
            TimeSpan timeout
        )
        {
            return await OpenSendingAmqpLinkAsync(
                deviceIdentity,
                _amqpSession,
                null,
                null,
                CommonConstants.DeviceEventPathTemplate,
                CommonConstants.ModuleEventPathTemplate,
                AmqpIoTConstants.TelemetrySenderLinkSuffix,
                null,
                timeout
            ).ConfigureAwait(false);
        }

        internal async Task<AmqpIoTReceivingLink> OpenMessageReceiverLinkAsync(
            DeviceIdentity deviceIdentity,
            TimeSpan timeout
        )
        {
            return await OpenReceivingAmqpLinkAsync(
                deviceIdentity,
                _amqpSession,
                null,
                (byte)ReceiverSettleMode.Second,
                CommonConstants.DeviceBoundPathTemplate,
                CommonConstants.ModuleBoundPathTemplate,
                AmqpIoTConstants.TelemetryReceiveLinkSuffix,
                null,
                timeout
            ).ConfigureAwait(false);
        }

        #endregion Telemetry links

        #region EventLink

        internal async Task<AmqpIoTReceivingLink> OpenEventsReceiverLinkAsync(
            DeviceIdentity deviceIdentity,
            TimeSpan timeout
        )
        {
            return await OpenReceivingAmqpLinkAsync(
                deviceIdentity,
                _amqpSession,
                null,
                (byte)ReceiverSettleMode.First,
                CommonConstants.DeviceEventPathTemplate,
                CommonConstants.ModuleEventPathTemplate,
                AmqpIoTConstants.EventsReceiverLinkSuffix,
                null,
                timeout
            ).ConfigureAwait(false);
        }

        #endregion EventLink

        #region MethodLink

        internal async Task<AmqpIoTSendingLink> OpenMethodsSenderLinkAsync(
            DeviceIdentity deviceIdentity,
            string correlationIdSuffix,
            TimeSpan timeout
        )
        {
            return await OpenSendingAmqpLinkAsync(
                    deviceIdentity,
                    _amqpSession,
                    (byte)SenderSettleMode.Settled,
                    (byte)ReceiverSettleMode.First,
                    CommonConstants.DeviceMethodPathTemplate,
                    CommonConstants.ModuleMethodPathTemplate,
                    AmqpIoTConstants.MethodsSenderLinkSuffix,
                    AmqpIoTConstants.MethodCorrelationIdPrefix + correlationIdSuffix,
                    timeout
            ).ConfigureAwait(false);
        }

        internal async Task<AmqpIoTReceivingLink> OpenMethodsReceiverLinkAsync(
            DeviceIdentity deviceIdentity,
            string correlationIdSuffix,
            TimeSpan timeout
        )
        {
            return await OpenReceivingAmqpLinkAsync(
                deviceIdentity,
                _amqpSession,
                (byte)SenderSettleMode.Settled,
                (byte)ReceiverSettleMode.First,
                CommonConstants.DeviceMethodPathTemplate,
                CommonConstants.ModuleMethodPathTemplate,
                AmqpIoTConstants.MethodsReceiverLinkSuffix,
                AmqpIoTConstants.MethodCorrelationIdPrefix + correlationIdSuffix,
                timeout
            ).ConfigureAwait(false);
        }

        #endregion MethodLink

        #region TwinLink

        internal async Task<AmqpIoTReceivingLink> OpenTwinReceiverLinkAsync(
            DeviceIdentity deviceIdentity,
            string correlationIdSuffix,
            TimeSpan timeout
        )
        {
            return await OpenReceivingAmqpLinkAsync(
                deviceIdentity,
                _amqpSession,
                (byte)SenderSettleMode.Settled,
                (byte)ReceiverSettleMode.First,
                CommonConstants.DeviceTwinPathTemplate,
                CommonConstants.ModuleTwinPathTemplate,
                AmqpIoTConstants.TwinReceiverLinkSuffix,
                AmqpIoTConstants.TwinCorrelationIdPrefix + correlationIdSuffix,
                timeout
            ).ConfigureAwait(false);
        }

        internal async Task<AmqpIoTSendingLink> OpenTwinSenderLinkAsync(
            DeviceIdentity deviceIdentity,
            string correlationIdSuffix,
            TimeSpan timeout
        )
        {
            return await OpenSendingAmqpLinkAsync(
                    deviceIdentity,
                    _amqpSession,
                    (byte)SenderSettleMode.Settled,
                    (byte)ReceiverSettleMode.First,
                    CommonConstants.DeviceTwinPathTemplate,
                    CommonConstants.ModuleTwinPathTemplate,
                    AmqpIoTConstants.TwinSenderLinkSuffix,
                    AmqpIoTConstants.TwinCorrelationIdPrefix + correlationIdSuffix,
                    timeout
            ).ConfigureAwait(false);
        }

        #endregion TwinLink

        #region StreamLink
        internal async Task<AmqpIoTSendingLink> OpenStreamsSenderLinkAsync(
            DeviceIdentity deviceIdentity,
            string correlationIdSuffix,
            TimeSpan timeout
        )
        {
            return await OpenSendingAmqpLinkAsync(
                deviceIdentity,
                _amqpSession,
                (byte)SenderSettleMode.Settled,
                (byte)ReceiverSettleMode.First,
                CommonConstants.DeviceStreamsPathTemplate,
                CommonConstants.ModuleStreamsPathTemplate,
                AmqpIoTConstants.StreamsSenderLinkSuffix,
                AmqpIoTConstants.StreamsCorrelationIdPrefix + correlationIdSuffix,
                timeout
            ).ConfigureAwait(false);
        }

        internal async Task<AmqpIoTReceivingLink> OpenStreamsReceiverLinkAsync(
            DeviceIdentity deviceIdentity,
            string correlationIdSuffix,
            TimeSpan timeout
        )
        {
            return await OpenReceivingAmqpLinkAsync(
                deviceIdentity,
                _amqpSession,
                (byte)SenderSettleMode.Settled,
                (byte)ReceiverSettleMode.First,
                CommonConstants.DeviceStreamsPathTemplate,
                CommonConstants.ModuleStreamsPathTemplate,
                AmqpIoTConstants.StreamsReceiverLinkSuffix,
                AmqpIoTConstants.StreamsCorrelationIdPrefix + correlationIdSuffix,
                timeout
            ).ConfigureAwait(false);
        }
        #endregion

        #region Common link handling

        private static async Task<AmqpIoTSendingLink> OpenSendingAmqpLinkAsync(
            DeviceIdentity deviceIdentity,
            AmqpSession amqpSession,
            byte? senderSettleMode,
            byte? receiverSettleMode,
            string deviceTemplate,
            string moduleTemplate,
            string linkSuffix,
            string correlationId,
            TimeSpan timeout
        )
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(typeof(AmqpIoTSession), deviceIdentity, $"{nameof(OpenSendingAmqpLinkAsync)}");
            }

            AmqpLinkSettings amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = linkSuffix,
                Role = false,
                InitialDeliveryCount = 0,
                Target = new Target() { Address = BuildLinkAddress(deviceIdentity, deviceTemplate, moduleTemplate) },
                Source = new Source() { Address = deviceIdentity.IotHubConnectionString.DeviceId }
            };
            amqpLinkSettings.SndSettleMode = senderSettleMode;
            amqpLinkSettings.RcvSettleMode = receiverSettleMode;
            amqpLinkSettings.AddProperty(AmqpIoTErrorAdapter.TimeoutName, timeout.TotalMilliseconds);
            amqpLinkSettings.AddProperty(AmqpIoTConstants.ClientVersion, deviceIdentity.ProductInfo.ToString());

            if (correlationId != null)
            {
                amqpLinkSettings.AddProperty(AmqpIoTConstants.ChannelCorrelationId, correlationId);
            }

            if (!deviceIdentity.AmqpTransportSettings.AuthenticationChain.IsNullOrWhiteSpace())
            {
                amqpLinkSettings.AddProperty(AmqpIoTConstants.AuthChain, deviceIdentity.AmqpTransportSettings.AuthenticationChain);
            }

            // This check is added to enable the device or module client to available plug and play features. For devices or modules that pass in the model Id,
            // the SDK will enable plug and play features by setting the modelId to Amqp link settings.
            if (!string.IsNullOrWhiteSpace(deviceIdentity.Options?.ModelId))
            {
                amqpLinkSettings.AddProperty(AmqpIoTConstants.ModelId, deviceIdentity.Options.ModelId);
            }

            amqpLinkSettings.AddProperty(AmqpIoTConstants.ApiVersion, ClientApiVersionHelper.ApiVersionString);

            try
            {
                var sendingLink = new SendingAmqpLink(amqpLinkSettings);
                sendingLink.AttachTo(amqpSession);
                await sendingLink.OpenAsync(timeout).ConfigureAwait(false);
                return new AmqpIoTSendingLink(sendingLink);
            }
            catch (Exception e) when (!e.IsFatal())
            {
                Exception ex = AmqpIoTExceptionAdapter.ConvertToIoTHubException(e, amqpSession);
                if (ReferenceEquals(e, ex))
                {
                    throw;
                }
                else
                {
                    if (ex is AmqpIoTResourceException)
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
                    Logging.Exit(typeof(AmqpIoTSession), deviceIdentity, $"{nameof(OpenSendingAmqpLinkAsync)}");
                }
            }
        }

        private static async Task<AmqpIoTReceivingLink> OpenReceivingAmqpLinkAsync(
            DeviceIdentity deviceIdentity,
            AmqpSession amqpSession,
            byte? senderSettleMode,
            byte? receiverSettleMode,
            string deviceTemplate,
            string moduleTemplate,
            string linkSuffix,
            string correlationId,
            TimeSpan timeout)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(typeof(AmqpIoTSession), deviceIdentity, $"{nameof(OpenReceivingAmqpLinkAsync)}");
            }

            uint prefetchCount = deviceIdentity.AmqpTransportSettings.PrefetchCount;

            AmqpLinkSettings amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = linkSuffix,
                Role = true,
                TotalLinkCredit = prefetchCount,
                AutoSendFlow = prefetchCount > 0,
                Source = new Source() { Address = BuildLinkAddress(deviceIdentity, deviceTemplate, moduleTemplate) },
                Target = new Target() { Address = deviceIdentity.IotHubConnectionString.DeviceId }
            };
            amqpLinkSettings.SndSettleMode = senderSettleMode;
            amqpLinkSettings.RcvSettleMode = receiverSettleMode;
            amqpLinkSettings.AddProperty(AmqpIoTErrorAdapter.TimeoutName, timeout.TotalMilliseconds);
            amqpLinkSettings.AddProperty(AmqpIoTConstants.ClientVersion, deviceIdentity.ProductInfo.ToString());
            amqpLinkSettings.AddProperty(AmqpIoTConstants.ApiVersion, ClientApiVersionHelper.ApiVersionString);

            if (!deviceIdentity.AmqpTransportSettings.AuthenticationChain.IsNullOrWhiteSpace())
            {
                amqpLinkSettings.AddProperty(AmqpIoTConstants.AuthChain, deviceIdentity.AmqpTransportSettings.AuthenticationChain);
            }

            if (correlationId != null)
            {
                amqpLinkSettings.AddProperty(AmqpIoTConstants.ChannelCorrelationId, correlationId);
            }

            try
            {
                ReceivingAmqpLink receivingLink = new ReceivingAmqpLink(amqpLinkSettings);
                receivingLink.AttachTo(amqpSession);
                await receivingLink.OpenAsync(timeout).ConfigureAwait(false);
                return new AmqpIoTReceivingLink(receivingLink);
            }
            catch (Exception e) when (!e.IsFatal())
            {
                Exception ex = AmqpIoTExceptionAdapter.ConvertToIoTHubException(e, amqpSession);
                if (ReferenceEquals(e, ex))
                {
                    throw;
                }
                else
                {
                    if (ex is AmqpIoTResourceException)
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
                    Logging.Exit(typeof(AmqpIoTSession), deviceIdentity, $"{nameof(OpenReceivingAmqpLinkAsync)}");
                }
            }
        }

        private static string BuildLinkAddress(DeviceIdentity deviceIdentity, string deviceTemplate, string moduleTemplate)
        {
            string path;
            if (string.IsNullOrEmpty(deviceIdentity.IotHubConnectionString.ModuleId))
            {
                path = string.Format(
                    CultureInfo.InvariantCulture,
                    deviceTemplate,
                    WebUtility.UrlEncode(deviceIdentity.IotHubConnectionString.DeviceId)
                );
            }
            else
            {
                path = string.Format(
                    CultureInfo.InvariantCulture,
                    moduleTemplate,
                    WebUtility.UrlEncode(deviceIdentity.IotHubConnectionString.DeviceId), WebUtility.UrlEncode(deviceIdentity.IotHubConnectionString.ModuleId)
                );
            }
            return deviceIdentity.IotHubConnectionString.BuildLinkAddress(path).AbsoluteUri;
        }

        #endregion Common link handling
    }
}
