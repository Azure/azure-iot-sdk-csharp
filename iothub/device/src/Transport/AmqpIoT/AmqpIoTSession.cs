// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTSession
    {
        public event EventHandler Closed;
        private AmqpSession _amqpSession;

        public AmqpIoTSession(AmqpConnection amqpConnection)
        {
            AmqpSessionSettings amqpSessionSettings = new AmqpSessionSettings()
            {
                Properties = new Fields()
            };
            _amqpSession = new AmqpSession(amqpConnection, amqpSessionSettings, AmqpIoTLinkFactory.GetInstance());
            amqpConnection.AddSession(_amqpSession, new ushort?());

            _amqpSession.Closed += _amqpSessionClosed;
        }

        private void _amqpSessionClosed(object sender, EventArgs e)
        {
            Closed.Invoke(sender, e);
        }

        internal Task OpenAsync(TimeSpan timeout)
        {
            return _amqpSession.OpenAsync(timeout);
        }

        internal Task CloseAsync(TimeSpan timeout)
        {
            return _amqpSession.CloseAsync(timeout);
        }

        internal void Abort()
        {
            _amqpSession.Abort();
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

        internal async Task<AmqpIoTReceivingLink> OpenTelemetryReceiverLinkAsync(
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
        #endregion

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
        #endregion

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
        #endregion

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
            string CorrelationId,
            TimeSpan timeout
        )
        {
            if (Logging.IsEnabled) Logging.Enter(typeof(AmqpIoTSession), deviceIdentity, $"{nameof(OpenSendingAmqpLinkAsync)}");
            AmqpLinkSettings amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = CommonResources.GetNewStringGuid(linkSuffix),
                Role = false,
                InitialDeliveryCount = 0,
                Target = new Target() { Address = BuildLinkAddress(deviceIdentity, deviceTemplate, moduleTemplate) },
                Source = new Source() { Address = deviceIdentity.IotHubConnectionString.DeviceId }
            };
            amqpLinkSettings.SndSettleMode = senderSettleMode;
            amqpLinkSettings.RcvSettleMode = receiverSettleMode;
            amqpLinkSettings.AddProperty(AmqpIoTErrorAdapter.TimeoutName, timeout.TotalMilliseconds);
            amqpLinkSettings.AddProperty(AmqpIoTErrorAdapter.ClientVersion, deviceIdentity.ProductInfo.ToString());
            amqpLinkSettings.AddProperty(AmqpIoTErrorAdapter.ApiVersion, ClientApiVersionHelper.ApiVersionString);
            if (CorrelationId != null)
            {
                amqpLinkSettings.AddProperty(AmqpIoTErrorAdapter.ChannelCorrelationId, CorrelationId);
            }

            SendingAmqpLink sendingLink = new SendingAmqpLink(amqpLinkSettings);
            sendingLink.AttachTo(amqpSession);
            await sendingLink.OpenAsync(timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(typeof(AmqpIoTSession), deviceIdentity, $"{nameof(OpenSendingAmqpLinkAsync)}");
            return new AmqpIoTSendingLink(sendingLink);
        }

        private static async Task<AmqpIoTReceivingLink> OpenReceivingAmqpLinkAsync(
            DeviceIdentity deviceIdentity,
            AmqpSession amqpSession,
            byte? senderSettleMode,
            byte? receiverSettleMode,
            string deviceTemplate,
            string moduleTemplate,
            string linkSuffix,
            string CorrelationId,
            TimeSpan timeout
        )
        {
            if (Logging.IsEnabled) Logging.Enter(typeof(AmqpIoTSession), deviceIdentity, $"{nameof(OpenReceivingAmqpLinkAsync)}");

            uint prefetchCount = deviceIdentity.AmqpTransportSettings.PrefetchCount;

            AmqpLinkSettings amqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = CommonResources.GetNewStringGuid(linkSuffix),
                Role = true,
                TotalLinkCredit = prefetchCount,
                AutoSendFlow = prefetchCount > 0,
                Source = new Source() { Address = BuildLinkAddress(deviceIdentity, deviceTemplate, moduleTemplate) },
                Target = new Target() { Address = deviceIdentity.IotHubConnectionString.DeviceId }
            };
            amqpLinkSettings.SndSettleMode = senderSettleMode;
            amqpLinkSettings.RcvSettleMode = receiverSettleMode;
            amqpLinkSettings.AddProperty(AmqpIoTErrorAdapter.TimeoutName, timeout.TotalMilliseconds);
            amqpLinkSettings.AddProperty(AmqpIoTErrorAdapter.ClientVersion, deviceIdentity.ProductInfo.ToString());
            amqpLinkSettings.AddProperty(AmqpIoTErrorAdapter.ApiVersion, ClientApiVersionHelper.ApiVersionString);
            if (CorrelationId != null)
            {
                amqpLinkSettings.AddProperty(AmqpIoTErrorAdapter.ChannelCorrelationId, CorrelationId);
            }

            ReceivingAmqpLink receivingLink = new ReceivingAmqpLink(amqpLinkSettings);
            receivingLink.AttachTo(amqpSession);
            await receivingLink.OpenAsync(timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(typeof(AmqpIoTSession), deviceIdentity, $"{nameof(OpenReceivingAmqpLinkAsync)}");
            return new AmqpIoTReceivingLink(receivingLink);
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
        #endregion
    }
}
