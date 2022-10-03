// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Authentication;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Represents the HTTP protocol implementation for the Provisioning Transport Handler.
    /// </summary>
    internal class ProvisioningTransportHandlerHttp : ProvisioningTransportHandler
    {
        private static readonly TimeSpan s_defaultOperationPoolingIntervalMilliseconds = TimeSpan.FromSeconds(2);
        private const int DefaultHttpsPort = 443;

        // These default values are consistent with Azure.Core default values:
        // https://github.com/Azure/azure-sdk-for-net/blob/7e3cf643977591e9041f4c628fd4d28237398e0b/sdk/core/Azure.Core/src/Pipeline/ServicePointHelpers.cs#L28
        private const int DefaultMaxConnectionsPerServer = 50;

        // How long, in milliseconds, a given cached TCP connection created by this client's HTTP layer will live before being closed.
        // If this value is set to any negative value, the connection lease will be infinite. If this value is set to 0, then the TCP connection will close after
        // each HTTP request and a new TCP connection will be opened upon the next request.
        //
        // By closing cached TCP connections and opening a new one upon the next request, the underlying HTTP client has a chance to do a DNS lookup
        // to validate that it will send the requests to the correct IP address. While it is atypical for a given IoT hub to change its IP address, it does
        // happen when a given IoT hub fails over into a different region.
        //
        // This default value is consistent with the default value used in Azure.Core
        // https://github.com/Azure/azure-sdk-for-net/blob/7e3cf643977591e9041f4c628fd4d28237398e0b/sdk/core/Azure.Core/src/Pipeline/ServicePointHelpers.cs#L29
        private static readonly TimeSpan DefaultConnectionLeaseTimeout = TimeSpan.FromMinutes(5);

        private readonly ProvisioningClientOptions _options;
        private readonly ProvisioningClientHttpSettings _settings;

        /// <summary>
        /// Creates an instance of the ProvisioningTransportHandlerHttp class.
        /// </summary>
        internal ProvisioningTransportHandlerHttp(ProvisioningClientOptions options)
        {
            _options = options;
            _settings = (ProvisioningClientHttpSettings)options.TransportSettings;
        }

        /// <summary>
        /// Registers a device described by the message.
        /// </summary>
        /// <param name="message">The provisioning message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        internal override async Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterRequest message,
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
                            $"{nameof(message.Authentication)} must be of type {nameof(AuthenticationProviderX509)} or {nameof(AuthenticationProviderSymmetricKey)}");
                }

                Logging.Associate(authStrategy, this);

                using var httpClientHandler = new HttpClientHandler()
                {
                    // Cannot specify a specific protocol here, as desired due to an error:
                    //   ProvisioningDeviceClient_ValidRegistrationId_AmqpWithProxy_SymmetricKey_RegisterOk_GroupEnrollment failing for me with System.PlatformNotSupportedException: Operation is not supported on this platform.
                    // When revisiting TLS12 work for DPS, we should figure out why. Perhaps the service needs to support it.

                    SslProtocols = _settings.SslProtocols,
                };

                if (_settings.Proxy != null)
                {
                    httpClientHandler.UseProxy = true;
                    httpClientHandler.Proxy = _settings.Proxy;
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"{nameof(RegisterAsync)} Setting HttpClientHandler.Proxy");
                }

                var builder = new UriBuilder
                {
                    Scheme = Uri.UriSchemeHttps,
                    Host = message.GlobalDeviceEndpoint,
                    Port = DefaultHttpsPort,
                };

                httpClientHandler.MaxConnectionsPerServer = DefaultMaxConnectionsPerServer;
                ServicePoint servicePoint = ServicePointManager.FindServicePoint(builder.Uri);
                servicePoint.ConnectionLeaseTimeout = DefaultConnectionLeaseTimeout.Milliseconds;

                using DeviceProvisioningServiceRuntimeClient client = authStrategy.CreateClient(builder.Uri, httpClientHandler);
                client.HttpClient.DefaultRequestHeaders.Add("User-Agent", _options.UserAgentInfo.ToString());
                if (Logging.IsEnabled)
                    Logging.Info(this, $"Uri: {builder.Uri}; User-Agent: {_options.UserAgentInfo}");

                DeviceRegistrationHttp deviceRegistration = authStrategy.CreateDeviceRegistration();
                if (message.Payload != null
                    && message.Payload.Length > 0)
                {
                    deviceRegistration.Payload = new JRaw(message.Payload);
                }
                string registrationId = message.Authentication.GetRegistrationId();

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

                                throw new DeviceProvisioningClientException(
                                    ex.Response.Content,
                                    ex,
                                    ex.Response.StatusCode,
                                    errorDetails.ErrorCode,
                                    errorDetails.TrackingId);
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

                            throw new DeviceProvisioningClientException(
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

                return operation.RegistrationState;
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
                    throw new DeviceProvisioningClientException(
                        ex.Response.Content,
                        ex,
                        ex.Response.StatusCode,
                        errorDetails.ErrorCode,
                        errorDetails.TrackingId);
                }
                catch (JsonException jex)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(
                            this,
                            $"{nameof(ProvisioningTransportHandlerHttp)} server returned malformed error response. Parsing error: {jex}. Server response: {ex.Response.Content}",
                            nameof(RegisterAsync));

                    throw new DeviceProvisioningClientException(
                        $"HTTP transport exception: malformed server error message: '{ex.Response.Content}'",
                        jex,
                        false);
                }
            }
            catch (Exception ex) when (!(ex is DeviceProvisioningClientException))
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ProvisioningTransportHandlerHttp)} threw exception {ex}", nameof(RegisterAsync));

                throw new DeviceProvisioningClientException($"HTTP transport exception", ex, true);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(ProvisioningTransportHandlerHttp)}.{nameof(RegisterAsync)}");
            }
        }
    }
}
