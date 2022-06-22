// Copyright (c) Microsoft. All rights reserved.
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
        private const string EmptyJson = "{}";

        // This polling interval is the default time between checking if the device has reached a terminal state in its registration process
        // DPS will generally send a retry-after header that overrides this default value though.
        private static readonly TimeSpan s_defaultOperationPollingInterval = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan s_timeoutConstant = TimeSpan.FromMinutes(1);

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
        /// The fallback type. This allows direct or WebSocket connections.
        /// </summary>
        public TransportFallbackType FallbackType { get; private set; }

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

            using var cts = new CancellationTokenSource(timeout);
            return await RegisterAsync(message, cts.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Registers a device described by the message. Because the AMQP library does not accept cancellation tokens, the provided cancellation token
        /// will only be checked for cancellation between AMQP operations. The timeout will be respected during the AMQP operations.
        /// </summary>
        /// <param name="message">The provisioning message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        public override async Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterMessage message,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(ProvisioningTransportHandlerAmqp)}.{nameof(RegisterAsync)}");

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            // We need to create a LinkedTokenSource to include both the default timeout and the cancellation token
            // AMQP library started supporting CancellationToken starting from version 2.5.5
            // To preserve current behavior, we will honor both the legacy timeout and the cancellation token parameter.
            using var timeoutTokenSource = new CancellationTokenSource(s_timeoutConstant);
            using var cancellationTokenSourceBundle = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);

            CancellationToken bundleCancellationToken = cancellationTokenSourceBundle.Token;

            bundleCancellationToken.ThrowIfCancellationRequested();

            try
            {
                AmqpAuthStrategy authStrategy;

                if (message.Security is SecurityProviderTpm tpm)
                {
                    authStrategy = new AmqpAuthStrategyTpm(tpm);
                }
                else if (message.Security is SecurityProviderX509 x509)
                {
                    authStrategy = new AmqpAuthStrategyX509(x509);
                }
                else if (message.Security is SecurityProviderSymmetricKey key)
                {
                    authStrategy = new AmqpAuthStrategySymmetricKey(key);
                }
                else
                {
                    throw new NotSupportedException(
                        $"{nameof(message.Security)} must be of type {nameof(SecurityProviderTpm)}, " +
                        $"{nameof(SecurityProviderX509)} or {nameof(SecurityProviderSymmetricKey)}");
                }

                if (Logging.IsEnabled)
                    Logging.Associate(authStrategy, this);

                bool useWebSocket = FallbackType == TransportFallbackType.WebSocketOnly;

                var builder = new UriBuilder
                {
                    Scheme = useWebSocket ? WebSocketConstants.Scheme : AmqpConstants.SchemeAmqps,
                    Host = message.GlobalDeviceEndpoint,
                    Port = Port,
                };

                string registrationId = message.Security.GetRegistrationID();
                string linkEndpoint = $"{message.IdScope}/registrations/{registrationId}";

                using AmqpClientConnection connection = authStrategy.CreateConnection(builder.Uri, message.IdScope);

                await authStrategy.OpenConnectionAsync(connection, useWebSocket, Proxy, RemoteCertificateValidationCallback, bundleCancellationToken).ConfigureAwait(false);
                bundleCancellationToken.ThrowIfCancellationRequested();

                await CreateLinksAsync(
                        connection,
                        linkEndpoint,
                        message.ProductInfo,
                        bundleCancellationToken)
                    .ConfigureAwait(false);

                bundleCancellationToken.ThrowIfCancellationRequested();

                string correlationId = Guid.NewGuid().ToString();

                var deviceRegistration = new DeviceRegistration();

                if (!string.IsNullOrWhiteSpace(message.Payload))
                {
                    deviceRegistration.Payload = new JRaw(message.Payload);
                }

                if (!string.IsNullOrWhiteSpace(message.ClientCertificateSigningRequest))
                {
                    deviceRegistration.ClientCertificateSigningRequest = message.ClientCertificateSigningRequest;
                }

                RegistrationOperationStatus operation = await RegisterDeviceAsync(connection, correlationId, deviceRegistration, bundleCancellationToken).ConfigureAwait(false);

                // Poll with operationId until registration complete.
                int attempts = 0;
                string operationId = operation.OperationId;

                // Poll with operationId until registration complete.
                while (string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusAssigning) == 0
                    || string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusUnassigned) == 0)
                {
                    bundleCancellationToken.ThrowIfCancellationRequested();

                    await Task.Delay(
                        operation.RetryAfter ?? RetryJitter.GenerateDelayWithJitterForRetry(s_defaultOperationPollingInterval),
                        bundleCancellationToken).ConfigureAwait(false);

                    try
                    {
                        operation = await OperationStatusLookupAsync(
                            connection,
                            operationId,
                            correlationId,
                            bundleCancellationToken)
                        .ConfigureAwait(false);
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

                await connection.CloseAsync(bundleCancellationToken).ConfigureAwait(false);

                return ConvertToProvisioningRegistrationResult(operation.RegistrationState);
            }
            catch (Exception ex) when (!(ex is ProvisioningTransportException))
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ProvisioningTransportHandlerAmqp)} threw exception {ex}", nameof(RegisterAsync));

                throw new ProvisioningTransportException($"AMQP transport exception", ex, true);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(ProvisioningTransportHandlerAmqp)}.{nameof(RegisterAsync)}");
            }
        }

        private static async Task CreateLinksAsync(AmqpClientConnection connection, string linkEndpoint, string productInfo, CancellationToken cancellationToken)
        {
            AmqpClientSession amqpDeviceSession = connection.CreateSession();
            await amqpDeviceSession.OpenAsync(cancellationToken).ConfigureAwait(false);

            AmqpClientLink amqpReceivingLink = amqpDeviceSession.CreateReceivingLink(linkEndpoint);

            amqpReceivingLink.AddClientVersion(productInfo);
            amqpReceivingLink.AddApiVersion(ClientApiVersionHelper.ApiVersion);

            await amqpReceivingLink.OpenAsync(cancellationToken).ConfigureAwait(false);

            AmqpClientLink amqpSendingLink = amqpDeviceSession.CreateSendingLink(linkEndpoint);

            amqpSendingLink.AddClientVersion(productInfo);
            amqpSendingLink.AddApiVersion(ClientApiVersionHelper.ApiVersion);

            await amqpSendingLink.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<RegistrationOperationStatus> RegisterDeviceAsync(
            AmqpClientConnection client,
            string correlationId,
            DeviceRegistration deviceRegistration,
            CancellationToken cancellationToken)
        {
            AmqpMessage amqpMessage = null;

            try
            {
                string deviceRegistrationJsonString = JsonConvert.SerializeObject(deviceRegistration);

                if (deviceRegistrationJsonString == EmptyJson)
                {
                    amqpMessage = AmqpMessage.Create(new MemoryStream(), true);
                }
                else
                {
                    var customContentStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deviceRegistration)));
                    amqpMessage = AmqpMessage.Create(customContentStream, true);
                }

                amqpMessage.Properties.CorrelationId = correlationId;
                amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.OperationType] = DeviceOperations.Register;
                amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.ForceRegistration] = false;

                Outcome outcome = await client.AmqpSession.SendingLink
                    .SendMessageAsync(
                        amqpMessage,
                        new ArraySegment<byte>(Guid.NewGuid().ToByteArray()),
                        cancellationToken)
                    .ConfigureAwait(false);
                ValidateOutcome(outcome);

                AmqpMessage amqpResponse = await client.AmqpSession.ReceivingLink.ReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
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
            CancellationToken cancellationToken)
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
                    cancellationToken)
                .ConfigureAwait(false);

            ValidateOutcome(outcome);

            AmqpMessage amqpResponse = await client.AmqpSession.ReceivingLink
                .ReceiveMessageAsync(cancellationToken)
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
            Enum.TryParse(result?.Status, true, out ProvisioningRegistrationStatusType status);
            Enum.TryParse(result?.Substatus, true, out ProvisioningRegistrationSubstatusType substatus);

            return new DeviceRegistrationResult(
                result?.RegistrationId,
                result?.CreatedDateTimeUtc,
                result?.AssignedHub,
                result?.DeviceId,
                status,
                substatus,
                result?.GenerationId,
                result?.LastUpdatedDateTimeUtc,
                result.ErrorCode == null ? 0 : (int)result.ErrorCode,
                result?.ErrorMessage,
                result?.Etag,
                result?.Payload?.ToString(CultureInfo.InvariantCulture),
                result?.IssuedClientCertificate,
                result?.TrustBundle);
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
                        Logging.Error(
                            this,
                            $"{nameof(ProvisioningTransportHandlerAmqp)} server returned malformed error response." +
                            $"Parsing error: {ex}. Server response: {rejected.Error.Description}",
                            nameof(RegisterAsync));

                    throw new ProvisioningTransportException(
                        $"AMQP transport exception: malformed server error message: '{rejected.Error.Description}'",
                        ex,
                        false);
                }
            }
        }
    }
}
