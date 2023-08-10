// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using Microsoft.Azure.Devices.Shared;
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
        private const int DefaultHttpsPort = 443;
        private static readonly TimeSpan s_defaultOperationPoolingIntervalMilliseconds = TimeSpan.FromSeconds(2);

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

                switch (message.Security)
                {
                    case SecurityProviderTpm _:
                        authStrategy = new HttpAuthStrategyTpm((SecurityProviderTpm)message.Security);
                        break;

                    case SecurityProviderX509 _:
                        authStrategy = new HttpAuthStrategyX509((SecurityProviderX509)message.Security);
                        break;

                    case SecurityProviderSymmetricKey _:
                        authStrategy = new HttpAuthStrategySymmetricKey((SecurityProviderSymmetricKey)message.Security);
                        break;

                    default:
                        if (Logging.IsEnabled)
                            Logging.Error(this, $"Invalid {nameof(SecurityProvider)} type.");

                        throw new NotSupportedException(
                            $"{nameof(message.Security)} must be of type {nameof(SecurityProviderTpm)}, {nameof(SecurityProviderX509)} or {nameof(SecurityProviderSymmetricKey)}");
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
                string registrationId = message.Security.GetRegistrationID();

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

        /// <inheritdoc/>
        public override async Task<DeviceOnboardingResult> OnboardAsync(ProvisioningTransportOnboardRequest request, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(ProvisioningTransportHandlerHttp)}.{nameof(OnboardAsync)}");

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                HttpAuthStrategy authStrategy;

                switch (request.Security)
                {
                    case SecurityProviderX509 _:
                        authStrategy = new HttpAuthStrategyX509((SecurityProviderX509)request.Security);
                        break;

                    default:
                        if (Logging.IsEnabled)
                            Logging.Error(this, $"Invalid {nameof(SecurityProvider)} type.");

                        throw new NotSupportedException(
                            $"{nameof(request.Security)} must be of type {nameof(SecurityProviderX509)}");
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
                        Logging.Info(this, $"{nameof(OnboardAsync)} Setting HttpClientHandler.Proxy");
                }

                var builder = new UriBuilder
                {
                    Scheme = Uri.UriSchemeHttps,
                    Host = request.GlobalDeviceEndpoint,
                    Port = Port,
                };

                using DeviceProvisioningServiceRuntimeClient client = authStrategy.CreateClient(builder.Uri, httpClientHandler);
                client.HttpClient.DefaultRequestHeaders.Add("User-Agent", request.ProductInfo);
                if (Logging.IsEnabled)
                    Logging.Info(this, $"Uri: {builder.Uri}; User-Agent: {request.ProductInfo}");

                string registrationId = request.Security.GetRegistrationID();

                OnboardingRequest onboardRequest = new OnboardingRequest(registrationId, new ArcEnabledDeviceMetadata(request.PublicKey));

                DeviceOnboardingOperationStatus operation = await client.RuntimeRegistration
                    .OnboardDeviceAsync(
                        registrationId,
                        onboardRequest,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                int attempts = 0;
                string operationId = operation.Id;

                if (Logging.IsEnabled)
                    Logging.RegisterDevice(
                        this,
                        registrationId,
                        "",
                        "X509",
                        operation.Id,
                        operation.RetryAfter,
                        operation.Status.ToString());

                // Poll with operationId until registration complete.
                while (operation.Status == OperationState.InProgress)
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
                                nameof(OnboardAsync));

                        serviceRecommendedDelay = s_defaultOperationPoolingIntervalMilliseconds;
                    }

                    await Task
                        .Delay(serviceRecommendedDelay ?? RetryJitter.GenerateDelayWithJitterForRetry(s_defaultOperationPoolingIntervalMilliseconds), cancellationToken)
                        .ConfigureAwait(false);

                    try
                    {
                        operation = await client
                            .RuntimeRegistration.OnboardingOperationStatusLookupAsync(
                                registrationId,
                                operationId,
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
                                    Logging.Error(this, $"{nameof(ProvisioningTransportHandlerHttp)} threw exception {ex}", nameof(OnboardAsync));

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
                                    nameof(OnboardAsync));

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
                            operation.Id,
                            operation.RetryAfter,
                            operation.Status.ToString(),
                            attempts);

                    ++attempts;
                }

                return ConvertToOnboardingResult(operation);
            }
            catch (HttpOperationException ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ProvisioningTransportHandlerHttp)} threw exception {ex}", nameof(OnboardAsync));

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
                            nameof(OnboardAsync));

                    throw new ProvisioningTransportException(
                        $"HTTP transport exception: malformed server error message: '{ex.Response.Content}'",
                        jex,
                        false);
                }
            }
            catch (Exception ex) when (!(ex is ProvisioningTransportException))
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ProvisioningTransportHandlerHttp)} threw exception {ex}", nameof(OnboardAsync));

                throw new ProvisioningTransportException($"HTTP transport exception", ex, true);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(ProvisioningTransportHandlerHttp)}.{nameof(OnboardAsync)}");
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

        private static DeviceOnboardingResult ConvertToOnboardingResult(DeviceOnboardingOperationStatus result)
        {
            return new DeviceOnboardingResult(
                result.Id,
                new Device(
                    result.Result.RegistrationId, 
                    result.Result.OnboardingStatus)
                );
        }
    }
}
