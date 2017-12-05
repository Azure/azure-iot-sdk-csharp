// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    /// <summary>
    /// Represents the AMQP protocol implementation for the Provisioning Transport Handler.
    /// </summary>
    public class ProvisioningTransportHandlerAmqp : ProvisioningTransportHandler
    {
        private static readonly TimeSpan DefaultOperationPoolingIntervalMilliseconds = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan TimeoutConstant = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The fallback type. This allows direct or WebSocket connections.
        /// </summary>
        public TransportFallbackType FallbackType { get; private set; }

        /// <summary>
        /// Creates an instance of the ProvisioningTransportHandlerAmqp class using the specified fallback type.
        /// </summary>
        /// <param name="transportFallbackType">The fallback type allowing direct or WebSocket connections.</param>
        public ProvisioningTransportHandlerAmqp(
            TransportFallbackType transportFallbackType = TransportFallbackType.TcpWithWebSocketFallback)
        {
            FallbackType = transportFallbackType;
        }

        /// <summary>
        /// Registers a device described by the message.
        /// </summary>
        /// <param name="message">The provisioning message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        public override async Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterMessage message,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(ProvisioningTransportHandlerAmqp)}.{nameof(RegisterAsync)}");

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                AmqpAuthStrategy authStrategy;

                if (message.Security is SecurityProviderTpm)
                {
                    authStrategy = new AmqpAuthStrategyTpm((SecurityProviderTpm)message.Security);
                }
                else if (message.Security is SecurityProviderX509)
                {
                    authStrategy = new AmqpAuthStrategyX509((SecurityProviderX509)message.Security);
                }
                else
                {
                    throw new NotSupportedException(
                        $"{nameof(message.Security)} must be of type {nameof(SecurityProviderTpm)} " +
                        $"or {nameof(SecurityProviderX509)}");
                }

                if (Logging.IsEnabled) Logging.Associate(authStrategy, this);

                bool useWebSocket = (FallbackType == TransportFallbackType.WebSocketOnly);

                var builder = new UriBuilder()
                {
                    Scheme = useWebSocket ? WebSocketConstants.Scheme : AmqpConstants.SchemeAmqps,
                    Host = message.GlobalDeviceEndpoint,
                    Port = useWebSocket ? WebSocketConstants.Port : AmqpConstants.DefaultSecurePort
                };

                string registrationId = message.Security.GetRegistrationID();
                string linkEndpoint = $"{message.IdScope}/registrations/{registrationId}";

                AmqpClientConnection connection = authStrategy.CreateConnection(builder.Uri, message.IdScope);
                await authStrategy.OpenConnectionAsync(connection, TimeoutConstant, useWebSocket).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                
                await CreateLinksAsync(connection, linkEndpoint, message.ProductInfo).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                
                string correlationId = new Guid().ToString();
                RegistrationOperationStatus operation =
                    await RegisterDeviceAsync(connection, correlationId).ConfigureAwait(false);
                
                // Poll with operationId until registration complete.
                int attempts = 0;
                string operationId = operation.OperationId;

                // Poll with operationId until registration complete.
                while (string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusAssigning) == 0 ||
                       string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusUnassigned) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task.Delay(
                        operation.RetryAfter ??
                        DefaultOperationPoolingIntervalMilliseconds).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    operation = await OperationStatusLookupAsync(
                        connection,
                        operationId,
                        correlationId).ConfigureAwait(false);

                    attempts++;
                }

                if (string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusAssigned) == 0)
                {
                    authStrategy.SaveCredentials(operation);
                }

                return ConvertToProvisioningRegistrationResult(operation.RegistrationState);
            }
            // TODO: Catch only expected exceptions from Amqp.
            catch (Exception ex)
            {
                if (Logging.IsEnabled) Logging.Error(
                    this,
                    $"{nameof(ProvisioningTransportHandlerAmqp)} threw exception {ex}",
                    nameof(RegisterAsync));

                // TODO: Extract trackingId from the exception.
                throw new ProvisioningTransportException($"AMQP transport exception", ex, true);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(ProvisioningTransportHandlerAmqp)}.{nameof(RegisterAsync)}");
            }
        }

        private async Task CreateLinksAsync(AmqpClientConnection connection, string linkEndpoint, string productInfo)
        {
            var amqpDeviceSession = connection.CreateSession();
            await amqpDeviceSession.OpenAsync(TimeoutConstant).ConfigureAwait(false);

            var amqpReceivingLink = amqpDeviceSession.CreateReceivingLink(linkEndpoint);

            amqpReceivingLink.AddClientVersion(productInfo);
            amqpReceivingLink.AddApiVersion(ClientApiVersionHelper.ApiVersion);

            await amqpReceivingLink.OpenAsync(TimeoutConstant).ConfigureAwait(false);

            var amqpSendingLink = amqpDeviceSession.CreateSendingLink(linkEndpoint);

            amqpSendingLink.AddClientVersion(productInfo);
            amqpSendingLink.AddApiVersion(ClientApiVersionHelper.ApiVersion);

            await amqpSendingLink.OpenAsync(TimeoutConstant).ConfigureAwait(false);
        }

        private async Task<RegistrationOperationStatus> RegisterDeviceAsync(
            AmqpClientConnection client, 
            string correlationId)
        {
            var amqpMessage = AmqpMessage.Create(new AmqpValue() { Value = DeviceOperations.Register });
            amqpMessage.Properties.CorrelationId = correlationId;
            amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.OperationType] =
                DeviceOperations.Register;
            amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.ForceRegistration] = false;
            var outcome = await client.AmqpSession.SendingLink
                .SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()),
                    TimeoutConstant).ConfigureAwait(false);
            var amqpResponse = await client.AmqpSession.ReceivingLink.ReceiveMessageAsync(TimeoutConstant)
                .ConfigureAwait(false);
            client.AmqpSession.ReceivingLink.AcceptMessage(amqpResponse);
            string jsonResponse = await new StreamReader(amqpResponse.BodyStream).ReadToEndAsync()
                .ConfigureAwait(false);
            return JsonConvert.DeserializeObject<RegistrationOperationStatus>(jsonResponse);
        }

        private async Task<RegistrationOperationStatus> OperationStatusLookupAsync(
            AmqpClientConnection client,
            string operationId, 
            string correlationId)
        {
            var amqpMessage = AmqpMessage.Create(new AmqpValue() { Value = DeviceOperations.GetOperationStatus });
            amqpMessage.Properties.CorrelationId = correlationId;
            amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.OperationType] =
                DeviceOperations.GetOperationStatus;
            amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.OperationId] = operationId;
            var outcome = await client.AmqpSession.SendingLink
                .SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()),
                    TimeoutConstant).ConfigureAwait(false);
            var amqpResponse = await client.AmqpSession.ReceivingLink.ReceiveMessageAsync(TimeoutConstant)
                .ConfigureAwait(false);
            client.AmqpSession.ReceivingLink.AcceptMessage(amqpResponse);
            string jsonResponse = await new StreamReader(amqpResponse.BodyStream).ReadToEndAsync()
                .ConfigureAwait(false);
            return JsonConvert.DeserializeObject<RegistrationOperationStatus>(jsonResponse);
        }

        private DeviceRegistrationResult ConvertToProvisioningRegistrationResult(
            Models.DeviceRegistrationResult result)
        {
            var status = ProvisioningRegistrationStatusType.Failed;
            Enum.TryParse(result.Status, true, out status);

            return new DeviceRegistrationResult(
                result.RegistrationId,
                result.CreatedDateTimeUtc,
                result.AssignedHub,
                result.DeviceId,
                status,
                result.GenerationId,
                result.LastUpdatedDateTimeUtc,
                result.ErrorCode == null ? 0 : (int)result.ErrorCode,
                result.ErrorMessage,
                result.Etag);
        }
    }
}
