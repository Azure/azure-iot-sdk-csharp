// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
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
        /// <param name="provisioningRequest">The provisioning message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        internal override async Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterRequest provisioningRequest,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(ProvisioningTransportHandlerAmqp)}.{nameof(RegisterAsync)}");

            Argument.AssertNotNull(provisioningRequest, nameof(provisioningRequest));

            using var connectionLostCancellationToken = new CancellationTokenSource();

            try
            {
                AmqpAuthStrategy authStrategy = provisioningRequest.Authentication switch
                {
                    AuthenticationProviderX509 x509 => new AmqpAuthStrategyX509(x509),
                    AuthenticationProviderSymmetricKey key => new AmqpAuthStrategySymmetricKey(key),
                    _ => throw new NotSupportedException(
                        $"{nameof(provisioningRequest.Authentication)} must be of type " +
                        $"{nameof(AuthenticationProviderX509)} or {nameof(AuthenticationProviderSymmetricKey)}"),
                };

                if (Logging.IsEnabled)
                    Logging.Associate(authStrategy, this);

                cancellationToken.ThrowIfCancellationRequested();

                bool useWebSocket = _settings.Protocol == ProvisioningClientTransportProtocol.WebSocket;

                string registrationId = provisioningRequest.Authentication.GetRegistrationId();
                string linkEndpoint = $"{provisioningRequest.IdScope}/registrations/{registrationId}";

                using AmqpClientConnection connection = authStrategy.CreateConnection(
                    provisioningRequest.GlobalDeviceEndpoint,
                    provisioningRequest.IdScope,
                    () =>
                    {
                        if (Logging.IsEnabled)
                            Logging.Error(this, $"AMQP connection was lost.");

                        connectionLostCancellationToken.Cancel();
                    },
                    _settings);

                await authStrategy
                    .OpenConnectionAsync(
                        connection,
                        useWebSocket,
                        _settings.Proxy,
                        _settings.RemoteCertificateValidationCallback,
                        cancellationToken)
                    .ConfigureAwait(false);

                // Link the user-supplied cancellation token with a cancellation token that is cancelled
                // when the connection is lost so that all operations stop when either the user
                // cancels the token or when the connection is lost.
                using var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
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

                RegistrationOperationStatus operation = await RegisterDeviceAsync(
                        connection,
                        correlationId,
                        provisioningRequest.Payload,
                        linkedCancellationToken.Token)
                    .ConfigureAwait(false);

                // Poll with operationId until registration complete.
                int attempts = 0;
                string operationId = operation.OperationId;

                // Poll with operationId until registration complete.
                while (operation.Status == ProvisioningRegistrationStatus.Assigning
                    || operation.Status == ProvisioningRegistrationStatus.Unassigned)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task
                        .Delay(
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
                    catch (ProvisioningClientException e) when (e.IsTransient)
                    {
                        operation.RetryAfter = _retryAfter;
                    }

                    attempts++;
                }

                if (operation.Status == ProvisioningRegistrationStatus.Assigned)
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
                throw new ProvisioningClientException($"AMQP connection was lost during provisioning.", true);

                // If it was the user's cancellation token that requested cancellation, then this catch block
                // won't execute and the OperationCanceledException will be thrown as expected.
            }
            catch (AuthenticationException authEx)
            {
                throw new ProvisioningClientException(authEx.Message, authEx, false);
            }
            catch (WebSocketException webEx)
            {
                if (ContainsAuthenticationException(webEx))
                {
                    throw new ProvisioningClientException(webEx.Message, webEx, false);
                }
                throw new ProvisioningClientException($"AMQP transport exception", webEx, true);
            }
            catch (Exception ex) when (ex is not ProvisioningClientException)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ProvisioningTransportHandlerAmqp)} threw exception {ex}", nameof(RegisterAsync));

                throw new ProvisioningClientException($"AMQP transport exception", ex, true);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(ProvisioningTransportHandlerAmqp)}.{nameof(RegisterAsync)}");
            }
        }

        private static async Task CreateLinksAsync(
            AmqpClientConnection connection,
            string linkEndpoint,
            string productInfo,
            CancellationToken cancellationToken)
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
            RegistrationRequestPayload payload,
            CancellationToken cancellationToken)
        {
            AmqpMessage amqpMessage = null;

            try
            {
                amqpMessage = payload == null
                    ? AmqpMessage.Create(new MemoryStream(), true)
                    : AmqpMessage.Create(new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload))), true);

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
                    bool isTransient = statusCode == 429;
                    if (isTransient)
                    {
                        errorDetails.RetryAfter = ProvisioningErrorDetailsAmqp.GetRetryAfterFromRejection(rejected, s_defaultOperationPollingInterval);
                        _retryAfter = errorDetails.RetryAfter;
                    }

                    throw new ProvisioningClientException(
                        rejected.Error.Description,
                        null,
                        isTransient,
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

                    throw new ProvisioningClientException(
                        $"AMQP transport exception: malformed server error message: '{rejected.Error.Description}'",
                        ex,
                        false);
                }
            }
        }

        private static bool ContainsAuthenticationException(Exception ex)
        {
            return ex != null
                && (ex is AuthenticationException
                    || ContainsAuthenticationException(ex.InnerException));
        }
    }
}
