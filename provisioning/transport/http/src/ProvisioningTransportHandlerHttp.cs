// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Authentication;
using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    /// <summary>
    /// Represents the HTTP protocol implementation for the Provisioning Transport Handler.
    /// </summary>
    public class ProvisioningTransportHandlerHttp : ProvisioningTransportHandler
    {
        private static readonly TimeSpan s_defaultOperationPoolingIntervalMilliseconds = TimeSpan.FromSeconds(2);
        private const int DefaultHttpsPort = 443;

        /// <summary>
        /// Creates an instance of the ProvisioningTransportHandlerHttp class.
        /// </summary>
        public ProvisioningTransportHandlerHttp()
        {
            Port = DefaultHttpsPort;
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

            using var cts = new CancellationTokenSource(timeout);
            return await RegisterAsync(message, cts.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Registers a device described by the message.
        /// </summary>
        /// <param name="message">The provisioning message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        public async override Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterMessage message,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(ProvisioningTransportHandlerHttp)}.{nameof(RegisterAsync)}");

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                HttpAuthStrategy authStrategy;

                switch (message.Authentication)
                {
                    case AuthenticationProviderTpm _:
                        authStrategy = new HttpAuthStrategyTpm((AuthenticationProviderTpm)message.Authentication);
                        break;

                    case AuthenticationProviderX509 _:
                        authStrategy = new HttpAuthStrategyX509((AuthenticationProviderX509)message.Authentication);
                        break;

                    case AuthenticationProviderSymmetricKey _:
                        authStrategy = new HttpAuthStrategySymmetricKey((AuthenticationProviderSymmetricKey)message.Authentication);
                        break;

                    default:
                        if (Logging.IsEnabled)
                            Logging.Error(this, $"Invalid {nameof(AuthenticationProvider)} type.");

                        throw new NotSupportedException(
                            $"{nameof(message.Authentication)} must be of type {nameof(AuthenticationProviderTpm)}, {nameof(AuthenticationProviderX509)} or {nameof(AuthenticationProviderSymmetricKey)}");
                }

                Logging.Associate(authStrategy, this);

                using var httpClientHandler = new HttpClientHandler()
                {
                    // Cannot specify a specific protocol here, as desired due to an error:
                    //   ProvisioningDeviceClient_ValidRegistrationId_AmqpWithProxy_SymmetricKey_RegisterOk_GroupEnrollment failing for me with System.PlatformNotSupportedException: Operation is not supported on this platform.	
                    // When revisiting TLS12 work for DPS, we should figure out why. Perhaps the service needs to support it.	

                    //SslProtocols = TlsVersions.Preferred,
                };

                if (Proxy != DefaultWebProxySettings.Instance)
                {
                    httpClientHandler.UseProxy = Proxy != null;
                    httpClientHandler.Proxy = Proxy;
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"{nameof(RegisterAsync)} Setting HttpClientHandler.Proxy");
                }

                var builder = new UriBuilder
                {
                    Scheme = Uri.UriSchemeHttps,
                    Host = message.GlobalDeviceEndpoint,
                    Port = Port,
                };

                using DeviceProvisioningServiceRuntimeClient client = authStrategy.CreateClient(builder.Uri, httpClientHandler);
                client.HttpClient.DefaultRequestHeaders.Add("User-Agent", message.ProductInfo);
                if (Logging.IsEnabled)
                    Logging.Info(this, $"Uri: {builder.Uri}; User-Agent: {message.ProductInfo}");

                DeviceRegistration deviceRegistration = authStrategy.CreateDeviceRegistration();
                if (message.Payload != null
                    && message.Payload.Length > 0)
                {
                    deviceRegistration.Payload = new JRaw(message.Payload);
                }
                string registrationId = message.Authentication.GetRegistrationID();

                RegistrationOperationStatus operation = await client.RuntimeRegistration
                    .RegisterDeviceAsync(
                        registrationId,
                        message.IdScope,
                        deviceRegistration,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                int attempts = 0;
                string operationId = operation.OperationId;

                if (Logging.IsEnabled)
                    Logging.RegisterDevice(
                        this,
                        registrationId,
                        message.IdScope,
                        deviceRegistration.Tpm == null ? "X509" : "TPM",
                        operation.OperationId,
                        operation.RetryAfter,
                        operation.Status);

                // Poll with operationId until registration complete.
                while (string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusAssigning) == 0
                    || string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusUnassigned) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    TimeSpan? serviceRecommendedDelay = operation.RetryAfter;
                    if (serviceRecommendedDelay != null
                        && serviceRecommendedDelay?.TotalSeconds < s_defaultOperationPoolingIntervalMilliseconds.TotalSeconds)
                    {
                        if (Logging.IsEnabled)
                            Logging.Error(
                                this,
                                $"Service recommended unexpected retryAfter of {operation.RetryAfter?.TotalSeconds}, defaulting to delay of {s_defaultOperationPoolingIntervalMilliseconds}",
                                nameof(RegisterAsync));

                        serviceRecommendedDelay = s_defaultOperationPoolingIntervalMilliseconds;
                    }

                    await Task
                        .Delay(serviceRecommendedDelay ?? RetryJitter.GenerateDelayWithJitterForRetry(s_defaultOperationPoolingIntervalMilliseconds), cancellationToken)
                        .ConfigureAwait(false);

                    try
                    {
                        operation = await client
                            .RuntimeRegistration.OperationStatusLookupAsync(
                                registrationId,
                                operationId,
                                message.IdScope,
                                cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (HttpOperationException ex)
                    {
                        bool isTransient = ex.Response.StatusCode >= HttpStatusCode.InternalServerError
                            || (int)ex.Response.StatusCode == 429;

                        try
                        {
                            ProvisioningErrorDetailsHttp errorDetails = JsonConvert.DeserializeObject<ProvisioningErrorDetailsHttp>(ex.Response.Content);

                            if (isTransient)
                            {
                                serviceRecommendedDelay = errorDetails.RetryAfter;
                            }
                            else
                            {
                                if (Logging.IsEnabled)
                                    Logging.Error(this, $"{nameof(ProvisioningTransportHandlerHttp)} threw exception {ex}", nameof(RegisterAsync));

                                throw new ProvisioningTransportException(ex.Response.Content, ex, isTransient, errorDetails);
                            }
                        }
                        catch (JsonException jex)
                        {
                            if (Logging.IsEnabled)
                                Logging.Error(
                                    this,
                                    $"{nameof(ProvisioningTransportHandlerHttp)} server returned malformed error response." +
                                    $"Parsing error: {jex}. Server response: {ex.Response.Content}",
                                    nameof(RegisterAsync));

                            throw new ProvisioningTransportException(
                                $"HTTP transport exception: malformed server error message: '{ex.Response.Content}'",
                                jex,
                                false);
                        }
                    }

                    if (Logging.IsEnabled)
                        Logging.OperationStatusLookup(
                            this,
                            registrationId,
                            operation.OperationId,
                            operation.RetryAfter,
                            operation.Status,
                            attempts);

                    ++attempts;
                }

                if (string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusAssigned) == 0)
                {
                    authStrategy.SaveCredentials(operation);
                }

                return ConvertToProvisioningRegistrationResult(operation.RegistrationState);
            }
            catch (HttpOperationException ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ProvisioningTransportHandlerHttp)} threw exception {ex}", nameof(RegisterAsync));

                bool isTransient = ex.Response.StatusCode >= HttpStatusCode.InternalServerError
                    || (int)ex.Response.StatusCode == 429;

                try
                {
                    ProvisioningErrorDetailsHttp errorDetails = JsonConvert.DeserializeObject<ProvisioningErrorDetailsHttp>(ex.Response.Content);
                    throw new ProvisioningTransportException(ex.Response.Content, ex, isTransient, errorDetails);
                }
                catch (JsonException jex)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(
                            this,
                            $"{nameof(ProvisioningTransportHandlerHttp)} server returned malformed error response. Parsing error: {jex}. Server response: {ex.Response.Content}",
                            nameof(RegisterAsync));

                    throw new ProvisioningTransportException(
                        $"HTTP transport exception: malformed server error message: '{ex.Response.Content}'",
                        jex,
                        false);
                }
            }
            catch (Exception ex) when (!(ex is ProvisioningTransportException))
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ProvisioningTransportHandlerHttp)} threw exception {ex}", nameof(RegisterAsync));

                throw new ProvisioningTransportException($"HTTP transport exception", ex, true);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(ProvisioningTransportHandlerHttp)}.{nameof(RegisterAsync)}");
            }
        }

        private static DeviceRegistrationResult ConvertToProvisioningRegistrationResult(Models.DeviceRegistrationResult result)
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
    }
}
