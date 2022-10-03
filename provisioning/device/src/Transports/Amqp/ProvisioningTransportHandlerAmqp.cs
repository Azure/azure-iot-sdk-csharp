// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Represents the AMQP protocol implementation for the provisioning transport handler.
    /// </summary>
    internal class ProvisioningTransportHandlerAmqp : ProvisioningTransportHandler
    {
        private const string Register = "iotdps-register";
        private const string GetRegistration = "iotdps-get-registration";
        private const string GetOperationStatus = "iotdps-get-operationstatus";
        private const string Prefix = "iotdps-";
        private const string OperationType = Prefix + "operation-type";
        private const string OperationId = Prefix + "operation-id";
        private const string Status = Prefix + "status";
        private const string ForceRegistration = Prefix + "forceRegistration";

        // This polling interval is the default time between checking if the device has reached a terminal state in its registration process
        // DPS will generally send a retry-after header that overrides this default value though.
        private static readonly TimeSpan s_defaultOperationPollingInterval = TimeSpan.FromSeconds(2);

        private TimeSpan? _retryAfter;

        private readonly ProvisioningClientOptions _options;
        private readonly ProvisioningClientAmqpSettings _settings;

        /// <summary>
        /// Creates an instance of the ProvisioningTransportHandlerAmqp class using the specified fallback type.
        /// </summary>
        /// <param name="options">The options for the connection and messages sent/received on the connection.</param>
        internal ProvisioningTransportHandlerAmqp(ProvisioningClientOptions options)
        {
            _options = options;
            _settings = (ProvisioningClientAmqpSettings)options.TransportSettings;
        }

        /// <summary>
        /// Registers a device described by the message. Because the AMQP library does not accept cancellation tokens, the provided cancellation token
        /// will only be checked for cancellation between AMQP operations. The timeout will be respected during the AMQP operations.
        /// </summary>
        /// <param name="message">The provisioning message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        internal override async Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterRequest message,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(ProvisioningTransportHandlerAmqp)}.{nameof(RegisterAsync)}");

            Argument.AssertNotNull(message, nameof(message));

            using var connectionLostCancellationToken = new CancellationTokenSource();

            try
            {
                AmqpAuthStrategy authStrategy;

                if (message.Authentication is AuthenticationProviderX509 x509)
                {
                    authStrategy = new AmqpAuthStrategyX509(x509);
                }
                else if (message.Authentication is AuthenticationProviderSymmetricKey key)
                {
                    authStrategy = new AmqpAuthStrategySymmetricKey(key);
                }
                else
                {
                    throw new NotSupportedException(
                        $"{nameof(message.Authentication)} must be of type " +
                        $"{nameof(AuthenticationProviderX509)} or {nameof(AuthenticationProviderSymmetricKey)}");
                }

                if (Logging.IsEnabled)
                    Logging.Associate(authStrategy, this);

                cancellationToken.ThrowIfCancellationRequested();

                bool useWebSocket = _settings.Protocol == ProvisioningClientTransportProtocol.WebSocket;
                var builder = new UriBuilder
                {
                    Scheme = useWebSocket ? AmqpWebSocketConstants.Scheme : AmqpConstants.SchemeAmqps,
                    Host = message.GlobalDeviceEndpoint,
                    Port = useWebSocket ? AmqpWebSocketConstants.Port : AmqpConstants.DefaultSecurePort,
                };

                string registrationId = message.Authentication.GetRegistrationId();
                string linkEndpoint = $"{message.IdScope}/registrations/{registrationId}";

                using AmqpClientConnection connection = authStrategy.CreateConnection(
                    builder.Uri,
                    message.IdScope,
                    () =>
                    {
                        if (Logging.IsEnabled)
                            Logging.Error(this, $"AMQP connection was lost.");

                        connectionLostCancellationToken.Cancel();
                    },
                    _settings);

                await authStrategy.OpenConnectionAsync(connection, useWebSocket, _settings.Proxy, _settings.RemoteCertificateValidationCallback, cancellationToken).ConfigureAwait(false);

                // Link the user-supplied cancellation token with a cancellation token that is cancelled
                // when the connection is lost so that all operations stop when either the user
                // cancels the token or when the connection is lost.
                using CancellationTokenSource linkedCancellationToken =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        connectionLostCancellationToken.Token);

                await CreateLinksAsync(
                        connection,
                        linkEndpoint,
                        _options.UserAgentInfo.ToString(),
                        linkedCancellationToken.Token)
                    .ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                string correlationId = Guid.NewGuid().ToString();
                DeviceRegistration deviceRegistration = (message.Payload != null && message.Payload.Length > 0)
                    ? new DeviceRegistration(new JRaw(message.Payload))
                    : null;

                RegistrationOperationStatus operation = await RegisterDeviceAsync(
                        connection,
                        correlationId,
                        deviceRegistration,
                        linkedCancellationToken.Token)
                    .ConfigureAwait(false);

                // Poll with operationId until registration complete.
                int attempts = 0;
                string operationId = operation.OperationId;

                // Poll with operationId until registration complete.
                while (string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusAssigning) == 0
                    || string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusUnassigned) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task.Delay(
                            operation.RetryAfter ?? RetryJitter.GenerateDelayWithJitterForRetry(s_defaultOperationPollingInterval),
                            linkedCancellationToken.Token)
                        .ConfigureAwait(false);

                    try
                    {
                        operation = await OperationStatusLookupAsync(
                                connection,
                                operationId,
                                correlationId,
                                linkedCancellationToken.Token)
                            .ConfigureAwait(false);
                    }
                    catch (DeviceProvisioningClientException e) when (e.IsTransient)
                    {
                        operation.RetryAfter = _retryAfter;
                    }

                    attempts++;
                }

                if (string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusAssigned) == 0)
                {
                    authStrategy.SaveCredentials(operation);
                }

                await connection.CloseAsync(cancellationToken).ConfigureAwait(false);

                return operation.RegistrationState;
            }
            catch (OperationCanceledException) when (connectionLostCancellationToken.IsCancellationRequested)
            {
                // _connectionLostCancellationToken is cancelled when the connection is lost. This acts as
                // a signal to stop waiting on any service response and to throw the below exception up to the user
                // so they can retry.

                // Deliberately not including the caught exception as this exception's inner exception because
                // if the user sees an OperationCancelledException in the thrown exception, they may think they cancelled
                // the operation even though they didn't.
                throw new DeviceProvisioningClientException($"AMQP connection was lost during provisioning.", true);

                // If it was the user's cancellation token that requested cancellation, then this catch block
                // won't execute and the OperationCanceledException will be thrown as expected.
            }
            catch (Exception ex) when (ex is not DeviceProvisioningClientException)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ProvisioningTransportHandlerAmqp)} threw exception {ex}", nameof(RegisterAsync));

                throw new DeviceProvisioningClientException($"AMQP transport exception", ex, true);
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
                if (deviceRegistration == null)
                {
                    amqpMessage = AmqpMessage.Create(new MemoryStream(), true);
                }
                else
                {
                    var customContentStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deviceRegistration)));
                    amqpMessage = AmqpMessage.Create(customContentStream, true);
                }

                amqpMessage.Properties.CorrelationId = correlationId;
                amqpMessage.ApplicationProperties.Map[OperationType] = Register;
                amqpMessage.ApplicationProperties.Map[ForceRegistration] = false;

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
            using var amqpMessage = AmqpMessage.Create(new AmqpValue { Value = GetOperationStatus });

            amqpMessage.Properties.CorrelationId = correlationId;
            amqpMessage.ApplicationProperties.Map[OperationType] = GetOperationStatus;
            amqpMessage.ApplicationProperties.Map[OperationId] = operationId;

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

        private void ValidateOutcome(Outcome outcome)
        {
            if (outcome is Rejected rejected)
            {
                try
                {
                    ProvisioningErrorDetailsAmqp errorDetails = JsonConvert.DeserializeObject<ProvisioningErrorDetailsAmqp>(rejected.Error.Description);
                    // status code has an extra 3 trailing digits as a sub-code, so turn this into a standard 3 digit status code
                    int statusCode = errorDetails.ErrorCode / 1000;
                    bool isTransient = statusCode >= (int)HttpStatusCode.InternalServerError || statusCode == 429;
                    if (isTransient)
                    {
                        errorDetails.RetryAfter = ProvisioningErrorDetailsAmqp.GetRetryAfterFromRejection(rejected, s_defaultOperationPollingInterval);
                        _retryAfter = errorDetails.RetryAfter;
                    }

                    throw new DeviceProvisioningClientException(
                        rejected.Error.Description,
                        null,
                        (HttpStatusCode)statusCode,
                        errorDetails.ErrorCode,
                        errorDetails.TrackingId);
                }
                catch (JsonException ex)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(
                            this,
                            $"{nameof(ProvisioningTransportHandlerAmqp)} server returned malformed error response." +
                                $"Parsing error: {ex}. Server response: {rejected.Error.Description}",
                            nameof(RegisterAsync));

                    throw new DeviceProvisioningClientException(
                        $"AMQP transport exception: malformed server error message: '{rejected.Error.Description}'",
                        ex,
                        false);
                }
            }
        }
    }
}
