// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Common.Extensions;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    internal class AmqpClientHelper
    {
        public delegate object ParseFunc<in T>(AmqpMessage amqpMessage, T data);

        public static Exception ToIotHubClientContract(Exception exception)
        {
            if (exception is TimeoutException)
            {
                return new IotHubCommunicationException(exception.Message);
            }
            else if (exception is UnauthorizedAccessException)
            {
                return new UnauthorizedException(exception.Message);
            }
            else
            {
                if (exception is AmqpException amqpException)
                {
                    return AmqpErrorMapper.ToIotHubClientContract(amqpException.Error);
                }

                return exception;
            }
        }

        internal static string GetReceivingPath(EndpointKind endpointKind)
        {
            string path;
            switch (endpointKind)
            {
                case EndpointKind.Feedback:
                    path = "/messages/serviceBound/feedback";
                    break;

                case EndpointKind.FileNotification:
                    path = "/messages/serviceBound/filenotifications";
                    break;

                default:
                    throw new ArgumentException("Invalid endpoint kind to receive messages from Service endpoints", nameof(endpointKind));
            }

            Logging.Info(endpointKind, path, nameof(GetReceivingPath));

            return path;
        }

        internal static async Task DisposeMessageAsync(
            FaultTolerantAmqpObject<ReceivingAmqpLink> faultTolerantReceivingLink,
            ArraySegment<byte> deliveryTag,
            Outcome outcome,
            bool batchable)
        {
            using var cts = new CancellationTokenSource(IotHubConnection.DefaultOperationTimeout);
            await DisposeMessageAsync(faultTolerantReceivingLink, deliveryTag, outcome, batchable, cts.Token).ConfigureAwait(false);
        }

        internal static async Task DisposeMessageAsync(
            FaultTolerantAmqpObject<ReceivingAmqpLink> faultTolerantReceivingLink,
            ArraySegment<byte> deliveryTag,
            Outcome outcome,
            bool batchable,
            CancellationToken cancellationToken)
        {
            Logging.Enter(faultTolerantReceivingLink, deliveryTag, outcome.DescriptorCode, batchable, nameof(DisposeMessageAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                Outcome disposeOutcome;
                try
                {
                    ReceivingAmqpLink deviceBoundReceivingLink = await faultTolerantReceivingLink.GetReceivingLinkAsync().ConfigureAwait(false);
                    disposeOutcome = await deviceBoundReceivingLink
                        .DisposeMessageAsync(
                            deliveryTag,
                            outcome,
                            batchable,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    Logging.Error(faultTolerantReceivingLink, exception, nameof(DisposeMessageAsync));

                    if (exception.IsFatal())
                    {
                        throw;
                    }

                    throw ToIotHubClientContract(exception);
                }

                Logging.Info(faultTolerantReceivingLink, disposeOutcome.DescriptorCode, nameof(DisposeMessageAsync));

                if (disposeOutcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(disposeOutcome);
                }
            }
            finally
            {
                Logging.Exit(faultTolerantReceivingLink, deliveryTag, outcome.DescriptorCode, batchable, nameof(DisposeMessageAsync));
            }
        }

        public static void ValidateContentType(AmqpMessage amqpMessage, string expectedContentType)
        {
            string contentType = amqpMessage.Properties.ContentType.ToString();
            if (!string.Equals(contentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Unsupported content type: {0}".FormatInvariant(contentType));
            }
        }

        public static async Task<T> GetObjectFromAmqpMessageAsync<T>(AmqpMessage amqpMessage)
        {
            using var reader = new StreamReader(amqpMessage.BodyStream, Encoding.UTF8);
            string jsonString = await reader.ReadToEndAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }
    }
}
