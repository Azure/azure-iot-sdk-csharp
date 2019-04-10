// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpHelper
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

        #region Open IoTHub links 
        internal static async Task<ReceivingAmqpLink> OpenEventsReceiverLinkAsync(
            IAmqpSessionHolder amqpSessionHolder,
            TimeSpan timeout
        )
        {
            return await amqpSessionHolder.OpenReceivingAmqpLinkAsync(
                null,
                (byte)ReceiverSettleMode.First,
                CommonConstants.DeviceEventPathTemplate,
                CommonConstants.ModuleEventPathTemplate,
                EventsReceiverLinkSuffix,
                null,
                timeout
            ).ConfigureAwait(false);
        }

        internal static async Task<SendingAmqpLink> OpenTelemetrySenderLinkAsync(
            IAmqpSessionHolder amqpSessionHolder,
            TimeSpan timeout
        )
        {
            return await amqpSessionHolder.OpenSendingAmqpLinkAsync(
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
            IAmqpSessionHolder amqpSessionHolder, 
            TimeSpan timeout
        )
        {
            return await amqpSessionHolder.OpenReceivingAmqpLinkAsync(
                null,
                (byte)ReceiverSettleMode.Second,
                CommonConstants.DeviceBoundPathTemplate,
                CommonConstants.ModuleBoundPathTemplate,
                TelemetryReceiveLinkSuffix,
                null,
                timeout
            ).ConfigureAwait(false);
        }

        internal static async Task<ReceivingAmqpLink> OpenMethodsReceiverLinkAsync(
            IAmqpSessionHolder amqpSessionHolder,
            string correlationIdSuffix,
            TimeSpan timeout
        )
        {
            return await amqpSessionHolder.OpenReceivingAmqpLinkAsync(
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
            IAmqpSessionHolder amqpSessionHolder,
            string correlationIdSuffix,
            TimeSpan timeout
        )
        {
            return await amqpSessionHolder.OpenSendingAmqpLinkAsync(
                    (byte)SenderSettleMode.Settled,
                    (byte)ReceiverSettleMode.First,
                    CommonConstants.DeviceMethodPathTemplate,
                    CommonConstants.ModuleMethodPathTemplate,
                    MethodsSenderLinkSuffix,
                    MethodCorrelationIdPrefix + correlationIdSuffix,
                    timeout
            ).ConfigureAwait(false);
        }

        internal static async Task<ReceivingAmqpLink> OpenTwinReceiverLinkAsync(
            IAmqpSessionHolder amqpSessionHolder,
            string correlationIdSuffix,
            TimeSpan timeout
        )
        {
            return await amqpSessionHolder.OpenReceivingAmqpLinkAsync(
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
            IAmqpSessionHolder amqpSessionHolder,
            string correlationIdSuffix,
            TimeSpan timeout
        )
        {
            return await amqpSessionHolder.OpenSendingAmqpLinkAsync(
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

        #region Send/receive/dispose Message
        internal static async Task<Outcome> SendAmqpMessageAsync(
            SendingAmqpLink sendingAmqpLink,
            AmqpMessage message,
            TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(sendingAmqpLink, timeout, $"{nameof(SendAmqpMessageAsync)}");
            if (sendingAmqpLink == null || sendingAmqpLink.IsClosing())
            {
                throw new IotHubCommunicationException("SendingAmqpLink is closed.");
            }
            try
            {
                Outcome outcome = await sendingAmqpLink.SendMessageAsync(
                    message,
                    new ArraySegment<byte>(Guid.NewGuid().ToByteArray()),
                    AmqpConstants.NullBinary,
                    timeout
                ).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(sendingAmqpLink, timeout, $"{nameof(SendAmqpMessageAsync)}");
                return outcome;
            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                if (sendingAmqpLink.IsClosing())
                {
                    throw new IotHubCommunicationException("Amqp session is closed.");
                }
                else
                {
                    throw;
                }
            }
        }

        internal static async Task<AmqpMessage> ReceiveAmqpMessageAsync(ReceivingAmqpLink receivingAmqpLink, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(receivingAmqpLink, timeout, $"{nameof(DisposeMessageAsync)}");
            if (receivingAmqpLink == null || receivingAmqpLink.IsClosing())
            {
                throw new IotHubCommunicationException("ReceivingAmqpLink is closed.");
            }
            try
            {
                AmqpMessage amqpMessage = await receivingAmqpLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(receivingAmqpLink, timeout, $"{nameof(DisposeMessageAsync)}");
                return amqpMessage;

            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                if (receivingAmqpLink.IsClosing())
                {
                    throw new IotHubCommunicationException("Amqp session is closed.");
                }
                else
                {
                    throw;
                }
            }
        }
        #endregion
        
        public static async Task<Outcome> DisposeMessageAsync(ReceivingAmqpLink receivingAmqpLink, string lockToken, Outcome outcome, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(receivingAmqpLink, timeout, $"{nameof(DisposeMessageAsync)}");
            if (receivingAmqpLink == null || receivingAmqpLink.IsClosing())
            {
                throw new IotHubCommunicationException("ReceivingAmqpLink is closed.");
            }
            try
            {
                ArraySegment<byte> deliveryTag = ConvertToDeliveryTag(lockToken);
                Outcome disposeOutcome = await receivingAmqpLink.DisposeMessageAsync(deliveryTag, outcome, batchable: true, timeout: timeout).ConfigureAwait(false);
                if (Logging.IsEnabled) Logging.Exit(receivingAmqpLink, timeout, $"{nameof(DisposeMessageAsync)}");
                return disposeOutcome;
            }
            catch (Exception exception) when (!exception.IsFatal() && !(exception is OperationCanceledException))
            {
                if (receivingAmqpLink.IsClosing())
                {
                    throw new IotHubCommunicationException("Amqp session is closed.");
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task CloseAmqpObjectAsync(AmqpObject amqpObject, TimeSpan timeout)
        {
            if (amqpObject == null) return;
            try
            {
                await amqpObject.CloseAsync(timeout).ConfigureAwait(false);
            }
            catch(Exception e)
            {
                if (Logging.IsEnabled) Logging.Info(amqpObject, $"Failed with exception {e}", $"{nameof(CloseAmqpObjectAsync)}");
            }
        }

        public static void AbortAmqpObject(AmqpObject amqpObject)
        {
            try
            {
                amqpObject?.Abort();
            }
            catch (Exception e)
            {
                if (Logging.IsEnabled) Logging.Info(amqpObject, $"Failed with exception {e}", $"{nameof(AbortAmqpObject)}");
            }
        }

        public static string BuildLinkAddress(DeviceIdentity deviceIdentity, string deviceTemplate, string moduleTemplate)
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
