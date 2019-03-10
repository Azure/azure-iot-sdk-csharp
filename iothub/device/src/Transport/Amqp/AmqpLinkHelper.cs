using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpLinkHelper
    {
        private const string TelemetrySenderLinkSuffix = "_TelemetrySenderLink";
        private const string TelemetryReceiveLinkSuffix = "_TelemetryReceiverLink";
        private const string EventsReceiverLinkSuffix = "_EventsReceiverLink";
        private const string MethodsSenderLinkSuffix = "_MethodsSenderLink";
        private const string MethodsReceiverLinkSuffix = "_MethodsReceiverLink";
        private const string MethodCorrelationIdPrefix = "methods:";
        private const string TwinSenderLinkSuffix = "_TwinSenderLink";
        private const string TwinReceiverLinkSuffix = "_TwinReceiverLink";
        private const string TwinCorrelationIdPrefix = "twin:";

        #region SendingAmqpLink
        internal static async Task<SendingAmqpLink> OpenSendingAmqpLinkAsync(
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
            if (Logging.IsEnabled) Logging.Enter(typeof(AmqpLinkHelper), deviceIdentity, $"{nameof(OpenSendingAmqpLinkAsync)}");
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
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.TimeoutName, timeout.TotalMilliseconds);
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.ClientVersion, deviceIdentity.ProductInfo.ToString());
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.ApiVersion, ClientApiVersionHelper.ApiVersionString);
            if (CorrelationId != null)
            {
                amqpLinkSettings.AddProperty(IotHubAmqpProperty.ChannelCorrelationId, CorrelationId);
            }

            SendingAmqpLink sendingLink = new SendingAmqpLink(amqpLinkSettings);
            sendingLink.AttachTo(amqpSession);
            await sendingLink.OpenAsync(timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(typeof(AmqpLinkHelper), deviceIdentity, $"{nameof(OpenSendingAmqpLinkAsync)}");
            return sendingLink;
        }
        
        internal static async Task<Outcome> SendAmqpMessageAsync(
            SendingAmqpLink sendingAmqpLink, 
            AmqpMessage message, 
            TimeSpan timeout)
        {
            return await sendingAmqpLink.SendMessageAsync(
                message,
                new ArraySegment<byte>(Guid.NewGuid().ToByteArray()),
                AmqpConstants.NullBinary,
                timeout
            ).ConfigureAwait(false);
        }
        #endregion

        #region ReceivingAmqpLink
        internal static async Task<ReceivingAmqpLink> OpenReceivingAmqpLinkAsync(
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
            if (Logging.IsEnabled) Logging.Enter(typeof(AmqpLinkHelper), deviceIdentity, $"{nameof(OpenReceivingAmqpLinkAsync)}");

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
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.TimeoutName, timeout.TotalMilliseconds);
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.ClientVersion, deviceIdentity.ProductInfo.ToString());
            amqpLinkSettings.AddProperty(IotHubAmqpProperty.ApiVersion, ClientApiVersionHelper.ApiVersionString);
            if (CorrelationId != null)
            {
                amqpLinkSettings.AddProperty(IotHubAmqpProperty.ChannelCorrelationId, CorrelationId);
            }

            ReceivingAmqpLink receivingLink = new ReceivingAmqpLink(amqpLinkSettings);
            receivingLink.AttachTo(amqpSession);
            await receivingLink.OpenAsync(timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(typeof(AmqpLinkHelper), deviceIdentity, $"{nameof(OpenReceivingAmqpLinkAsync)}");
            return receivingLink;
        }

        internal static async Task<AmqpMessage> ReceiveAmqpMessageAsync(ReceivingAmqpLink receivingAmqpLink, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(receivingAmqpLink, timeout, $"{nameof(DisposeMessageAsync)}");

            AmqpMessage amqpMessage = null;
            if (receivingAmqpLink != null)
            {
                amqpMessage = await receivingAmqpLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);
            }
            if (Logging.IsEnabled) Logging.Exit(receivingAmqpLink, timeout, $"{nameof(DisposeMessageAsync)}");
            return amqpMessage;
        }

        #endregion

        #region TelemetryLink
        internal static async Task<SendingAmqpLink> OpenTelemetrySenderLinkAsync(
            DeviceIdentity deviceIdentity,
            AmqpSession amqpSession,
            TimeSpan timeout
        )
        {
            return await OpenSendingAmqpLinkAsync(
                deviceIdentity,
                amqpSession,
                null,
                null,
                CommonConstants.DeviceEventPathTemplate,
                CommonConstants.ModuleEventPathTemplate,
                TelemetrySenderLinkSuffix,
                null,
                timeout
            ).ConfigureAwait(false);
        }
        internal static async Task<ReceivingAmqpLink> OpenTelemetryReceiverLinkAsync(
            DeviceIdentity deviceIdentity,
            AmqpSession amqpSession,
            TimeSpan timeout
        )
        {
            return await OpenReceivingAmqpLinkAsync(
                deviceIdentity,
                amqpSession, 
                null,
                (byte)ReceiverSettleMode.Second,
                CommonConstants.DeviceBoundPathTemplate,
                CommonConstants.ModuleBoundPathTemplate,
                TelemetryReceiveLinkSuffix,
                null,
                timeout
            ).ConfigureAwait(false);
        }
        #endregion

        #region EventLink
        internal static async Task<ReceivingAmqpLink> OpenEventsReceiverLinkAsync(
            DeviceIdentity deviceIdentity,
            AmqpSession amqpSession,
            TimeSpan timeout
        )
        {
            return await OpenReceivingAmqpLinkAsync(
                deviceIdentity,
                amqpSession,
                null,
                (byte)ReceiverSettleMode.First,
                CommonConstants.DeviceEventPathTemplate,
                CommonConstants.ModuleEventPathTemplate,
                EventsReceiverLinkSuffix,
                null,
                timeout
            ).ConfigureAwait(false);
        }
        #endregion

        #region MethodLink
        internal static async Task<ReceivingAmqpLink> OpenMethodsReceiverLinkAsync(
            DeviceIdentity deviceIdentity,
            AmqpSession amqpSession,
            string correlationIdSuffix,
            TimeSpan timeout
        )
        {
            return await OpenReceivingAmqpLinkAsync(
                deviceIdentity,
                amqpSession,
                (byte)SenderSettleMode.Settled,
                (byte)ReceiverSettleMode.First,
                CommonConstants.DeviceMethodPathTemplate,
                CommonConstants.ModuleMethodPathTemplate,
                MethodsReceiverLinkSuffix,
                MethodCorrelationIdPrefix + correlationIdSuffix,
                timeout
            ).ConfigureAwait(false);
        }

        internal static async Task<SendingAmqpLink> OpenMethodsSenderLinkAsync(
            DeviceIdentity deviceIdentity,
            AmqpSession amqpSession,
            string correlationIdSuffix,
            TimeSpan timeout
        )
        {
            return await OpenSendingAmqpLinkAsync(
                    deviceIdentity,
                    amqpSession,
                    (byte)SenderSettleMode.Settled,
                    (byte)ReceiverSettleMode.First,
                    CommonConstants.DeviceMethodPathTemplate,
                    CommonConstants.ModuleMethodPathTemplate,
                    MethodsSenderLinkSuffix,
                    MethodCorrelationIdPrefix + correlationIdSuffix,
                    timeout
            ).ConfigureAwait(false);
        }
        #endregion

        #region TwinLink
        internal static async Task<ReceivingAmqpLink> OpenTwinReceiverLinkAsync(
            DeviceIdentity deviceIdentity,
            AmqpSession amqpSession,
            string correlationIdSuffix,
            TimeSpan timeout
        )
        {
            return await OpenReceivingAmqpLinkAsync(
                deviceIdentity,
                amqpSession,
                (byte)SenderSettleMode.Settled,
                (byte)ReceiverSettleMode.First,
                CommonConstants.DeviceTwinPathTemplate,
                CommonConstants.ModuleTwinPathTemplate,
                TwinReceiverLinkSuffix,
                TwinCorrelationIdPrefix + correlationIdSuffix,
                timeout
            ).ConfigureAwait(false);
        }

        internal static async Task<SendingAmqpLink> OpenTwinSenderLinkAsync(
            DeviceIdentity deviceIdentity,
            AmqpSession amqpSession,
            string correlationIdSuffix,
            TimeSpan timeout
        )
        {
            return await OpenSendingAmqpLinkAsync(
                    deviceIdentity,
                    amqpSession,
                    (byte)SenderSettleMode.Settled,
                    (byte)ReceiverSettleMode.First,
                    CommonConstants.DeviceTwinPathTemplate,
                    CommonConstants.ModuleTwinPathTemplate,
                    TwinSenderLinkSuffix,
                    TwinCorrelationIdPrefix + correlationIdSuffix,
                    timeout
            ).ConfigureAwait(false);
        }
        #endregion

        public static async Task<Outcome> DisposeMessageAsync(ReceivingAmqpLink receivingAmqpLink, string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(receivingAmqpLink, timeout, $"{nameof(DisposeMessageAsync)}");
            ArraySegment<byte> deliveryTag = ConvertToDeliveryTag(lockToken);
            Outcome disposeOutcome = await receivingAmqpLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout).ConfigureAwait(false);
            if (Logging.IsEnabled) Logging.Exit(receivingAmqpLink, timeout, $"{nameof(DisposeMessageAsync)}");
            return disposeOutcome;
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

        private static ArraySegment<byte> ConvertToDeliveryTag(string lockToken)
        {
            if (lockToken == null)
            {
                throw new ArgumentNullException("lockToken");
            }

            if (!Guid.TryParse(lockToken, out Guid lockTokenGuid))
            {
                throw new ArgumentException("Should be a valid Guid", "lockToken");
            }

            return new ArraySegment<byte>(lockTokenGuid.ToByteArray());
        }

    }
}
