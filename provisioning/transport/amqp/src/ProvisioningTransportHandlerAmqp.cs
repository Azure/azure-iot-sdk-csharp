﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    /// <summary>
    /// Represents the AMQP protocol implementation for the Provisioning Transport Handler.
    /// </summary>
    public class ProvisioningTransportHandlerAmqp : ProvisioningTransportHandler
    {
        // This polling interval is the default time between checking if the device has reached a terminal state in its registration process
        // DPS will generally send a retry-after header that overrides this default value though.
        private static readonly TimeSpan s_defaultOperationPollingInterval = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan s_timeoutConstant = TimeSpan.FromMinutes(1);

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
            bool useWebSocket = FallbackType == TransportFallbackType.WebSocketOnly;
            Port = useWebSocket ? WebSocketConstants.Port : AmqpConstants.DefaultSecurePort;
            Proxy = DefaultWebProxySettings.Instance;
        }

        /// <summary>
        /// Registers a device described by the message.
        /// </summary>
        /// <param name="message">The provisioning message.</param>
        /// <param name="timeout">The maximum amount of time to allow this operation to run for before timing out.</param>
        /// <returns>The registration result.</returns>
        public override async Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterMessage message,
            TimeSpan timeout)
        {
            if (TimeSpan.Zero.Equals(timeout))
            {
                throw new OperationCanceledException();
            }

            return await RegisterAsync(message, timeout, CancellationToken.None).ConfigureAwait(false);
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
            return await RegisterAsync(message, s_timeoutConstant, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Registers a device described by the message. Because the AMQP library does not accept cancellation tokens, the provided cancellation token
        /// will only be checked for cancellation between AMQP operations. The timeout will be respected during the AMQP operations.
        /// </summary>
        /// <param name="message">The provisioning message.</param>
        /// <param name="timeout">The maximum amount of time to allow this operation to run for before timing out.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        private async Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterMessage message,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, $"{nameof(ProvisioningTransportHandlerAmqp)}.{nameof(RegisterAsync)}");
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

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
                else if (message.Security is SecurityProviderSymmetricKey)
                {
                    authStrategy = new AmqpAuthStrategySymmetricKey((SecurityProviderSymmetricKey)message.Security);
                }
                else
                {
                    throw new NotSupportedException(
                        $"{nameof(message.Security)} must be of type {nameof(SecurityProviderTpm)}, " +
                        $"{nameof(SecurityProviderX509)} or {nameof(SecurityProviderSymmetricKey)}");
                }

                if (Logging.IsEnabled)
                {
                    Logging.Associate(authStrategy, this);
                }

                bool useWebSocket = (FallbackType == TransportFallbackType.WebSocketOnly);

                var builder = new UriBuilder()
                {
                    Scheme = useWebSocket ? WebSocketConstants.Scheme : AmqpConstants.SchemeAmqps,
                    Host = message.GlobalDeviceEndpoint,
                    Port = Port,
                };

                string registrationId = message.Security.GetRegistrationID();
                string linkEndpoint = $"{message.IdScope}/registrations/{registrationId}";

                using AmqpClientConnection connection = authStrategy.CreateConnection(builder.Uri, message.IdScope);
                await authStrategy.OpenConnectionAsync(connection, timeout, useWebSocket, Proxy, RemoteCertificateValidationCallback).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                await CreateLinksAsync(
                    connection,
                    linkEndpoint,
                    message.ProductInfo,
                    timeout).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                string correlationId = Guid.NewGuid().ToString();
                DeviceRegistration deviceRegistration = (message.Payload != null && message.Payload.Length > 0) ? new DeviceRegistration { Payload = new JRaw(message.Payload) } : null;

                RegistrationOperationStatus operation = await RegisterDeviceAsync(connection, correlationId, deviceRegistration, timeout).ConfigureAwait(false);

                // Poll with operationId until registration complete.
                int attempts = 0;
                string operationId = operation.OperationId;

                // Poll with operationId until registration complete.
                while (string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusAssigning) == 0 ||
                       string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusUnassigned) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task.Delay(
                        operation.RetryAfter ?? RetryJitter.GenerateDelayWithJitterForRetry(s_defaultOperationPollingInterval),
                        cancellationToken).ConfigureAwait(false);

                    try
                    {
                        operation = await OperationStatusLookupAsync(
                        connection,
                        operationId,
                        correlationId,
                        timeout).ConfigureAwait(false);
                    }
                    catch (ProvisioningTransportException e) when (e.ErrorDetails is ProvisioningErrorDetailsAmqp amqp && e.IsTransient)
                    {
                        operation.RetryAfter = amqp.RetryAfter;
                    }

                    attempts++;
                }

                if (string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusAssigned) == 0)
                {
                    authStrategy.SaveCredentials(operation);
                }

                await connection.CloseAsync(timeout).ConfigureAwait(false);

                return ConvertToProvisioningRegistrationResult(operation.RegistrationState);
            }
            catch (Exception ex) when (!(ex is ProvisioningTransportException))
            {
                if (Logging.IsEnabled)
                {
                    Logging.Error(
                    this,
                    $"{nameof(ProvisioningTransportHandlerAmqp)} threw exception {ex}",
                    nameof(RegisterAsync));
                }

                throw new ProvisioningTransportException($"AMQP transport exception", ex, true);
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, $"{nameof(ProvisioningTransportHandlerAmqp)}.{nameof(RegisterAsync)}");
                }
            }
        }

        private static async Task CreateLinksAsync(AmqpClientConnection connection, string linkEndpoint, string productInfo, TimeSpan timeout)
        {
            AmqpClientSession amqpDeviceSession = connection.CreateSession();
            await amqpDeviceSession.OpenAsync(timeout).ConfigureAwait(false);

            AmqpClientLink amqpReceivingLink = amqpDeviceSession.CreateReceivingLink(linkEndpoint);

            amqpReceivingLink.AddClientVersion(productInfo);
            amqpReceivingLink.AddApiVersion(ClientApiVersionHelper.ApiVersion);

            await amqpReceivingLink.OpenAsync(timeout).ConfigureAwait(false);

            AmqpClientLink amqpSendingLink = amqpDeviceSession.CreateSendingLink(linkEndpoint);

            amqpSendingLink.AddClientVersion(productInfo);
            amqpSendingLink.AddApiVersion(ClientApiVersionHelper.ApiVersion);

            await amqpSendingLink.OpenAsync(timeout).ConfigureAwait(false);
        }

        private async Task<RegistrationOperationStatus> RegisterDeviceAsync(
            AmqpClientConnection client,
            string correlationId,
            DeviceRegistration deviceRegistration,
            TimeSpan timeout)
        {
            AmqpMessage amqpMessage = null;

            try
            {
                if (deviceRegistration == null)
                {
                    amqpMessage = AmqpMessage.Create(new MemoryStream(), true);
                }
                else
                {
                    var customContentStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deviceRegistration)));
                    amqpMessage = AmqpMessage.Create(customContentStream, true);
                }

                amqpMessage.Properties.CorrelationId = correlationId;
                amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.OperationType] = DeviceOperations.Register;
                amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.ForceRegistration] = false;

                Outcome outcome = await client.AmqpSession.SendingLink
                    .SendMessageAsync(
                        amqpMessage,
                        new ArraySegment<byte>(Guid.NewGuid().ToByteArray()),
                        timeout)
                    .ConfigureAwait(false);
                ValidateOutcome(outcome);

                AmqpMessage amqpResponse = await client.AmqpSession.ReceivingLink.ReceiveMessageAsync(timeout).ConfigureAwait(false);
                client.AmqpSession.ReceivingLink.AcceptMessage(amqpResponse);

                using var streamReader = new StreamReader(amqpResponse.BodyStream);
                string jsonResponse = await streamReader
                    .ReadToEndAsync()
                    .ConfigureAwait(false);
                RegistrationOperationStatus status = JsonConvert.DeserializeObject<RegistrationOperationStatus>(jsonResponse);
                status.RetryAfter = ProvisioningErrorDetailsAmqp.GetRetryAfterFromApplicationProperties(amqpResponse, s_defaultOperationPollingInterval);
                return status;
            }
            finally
            {
                amqpMessage?.Dispose();
            }
        }

        private async Task<RegistrationOperationStatus> OperationStatusLookupAsync(
            AmqpClientConnection client,
            string operationId,
            string correlationId,
            TimeSpan timeout)
        {
            using var amqpMessage = AmqpMessage.Create(new AmqpValue { Value = DeviceOperations.GetOperationStatus });

            amqpMessage.Properties.CorrelationId = correlationId;
            amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.OperationType] =
                DeviceOperations.GetOperationStatus;
            amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.OperationId] = operationId;

            Outcome outcome = await client.AmqpSession.SendingLink
                .SendMessageAsync(
                    amqpMessage,
                    new ArraySegment<byte>(Guid.NewGuid().ToByteArray()),
                    timeout)
                .ConfigureAwait(false);

            ValidateOutcome(outcome);

            AmqpMessage amqpResponse = await client.AmqpSession.ReceivingLink.ReceiveMessageAsync(timeout)
                .ConfigureAwait(false);

            client.AmqpSession.ReceivingLink.AcceptMessage(amqpResponse);

            using var streamReader = new StreamReader(amqpResponse.BodyStream);

            string jsonResponse = await streamReader.ReadToEndAsync().ConfigureAwait(false);
            RegistrationOperationStatus status = JsonConvert.DeserializeObject<RegistrationOperationStatus>(jsonResponse);
            status.RetryAfter = ProvisioningErrorDetailsAmqp.GetRetryAfterFromApplicationProperties(amqpResponse, s_defaultOperationPollingInterval);

            return status;
        }

        private static DeviceRegistrationResult ConvertToProvisioningRegistrationResult(
            Models.DeviceRegistrationResult result)
        {
            Enum.TryParse(result.Status, true, out ProvisioningRegistrationStatusType status);
            Enum.TryParse(result.Substatus, true, out ProvisioningRegistrationSubstatusType substatus);

            return new DeviceRegistrationResult(
                result.RegistrationId,
                result.CreatedDateTimeUtc,
                result.AssignedHub,
                result.DeviceId,
                status,
                substatus,
                result.GenerationId,
                result.LastUpdatedDateTimeUtc,
                result.ErrorCode ?? 0,
                result.ErrorMessage,
                result.Etag,
                result?.Payload?.ToString(CultureInfo.InvariantCulture));
        }

        private void ValidateOutcome(Outcome outcome)
        {
            if (outcome is Rejected rejected)
            {
                try
                {
                    ProvisioningErrorDetailsAmqp errorDetails = JsonConvert.DeserializeObject<ProvisioningErrorDetailsAmqp>(rejected.Error.Description);
                    int statusCode = errorDetails.ErrorCode / 1000;
                    bool isTransient = statusCode >= (int)HttpStatusCode.InternalServerError || statusCode == 429;
                    if (isTransient)
                    {
                        errorDetails.RetryAfter = ProvisioningErrorDetailsAmqp.GetRetryAfterFromRejection(rejected, s_defaultOperationPollingInterval);
                    }

                    throw new ProvisioningTransportException(
                        rejected.Error.Description,
                        null,
                        isTransient,
                        errorDetails);
                }
                catch (JsonException ex)
                {
                    if (Logging.IsEnabled)
                    {
                        Logging.Error(
                        this,
                        $"{nameof(ProvisioningTransportHandlerAmqp)} server returned malformed error response." +
                        $"Parsing error: {ex}. Server response: {rejected.Error.Description}",
                        nameof(RegisterAsync));
                    }

                    throw new ProvisioningTransportException(
                        $"AMQP transport exception: malformed server error message: '{rejected.Error.Description}'",
                        ex,
                        false);
                }
            }
        }
    }
}
