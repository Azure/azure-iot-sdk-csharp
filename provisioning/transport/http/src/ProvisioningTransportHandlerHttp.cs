// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        public async override Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterMessage message,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(ProvisioningTransportHandlerHttp)}.{nameof(RegisterAsync)}");

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                HttpAuthStrategy authStrategy;

                if (message.Security is SecurityProviderTpm)
                {
                    authStrategy = new HttpAuthStrategyTpm((SecurityProviderTpm)message.Security);
                }
                else if (message.Security is SecurityProviderX509)
                {
                    authStrategy = new HttpAuthStrategyX509((SecurityProviderX509)message.Security);
                }
                else if (message.Security is SecurityProviderSymmetricKey)
                {
                    authStrategy = new HttpAuthStrategySymmetricKey((SecurityProviderSymmetricKey)message.Security);
                }
                else
                {
                    if (Logging.IsEnabled) Logging.Error(this, $"Invalid {nameof(SecurityProvider)} type.");
                    throw new NotSupportedException(
                        $"{nameof(message.Security)} must be of type {nameof(SecurityProviderTpm)} " +
                        $"or {nameof(SecurityProviderX509)}");
                }

                if (Logging.IsEnabled) Logging.Associate(authStrategy, this);

                var builder = new UriBuilder()
                {
                    Scheme = Uri.UriSchemeHttps,
                    Host = message.GlobalDeviceEndpoint,
                    Port = Port,
                };

                HttpClientHandler httpClientHandler = new HttpClientHandler();

                if (Proxy != DefaultWebProxySettings.Instance)
                {
                    httpClientHandler.UseProxy = (Proxy != null);
                    httpClientHandler.Proxy = Proxy;
                    if (Logging.IsEnabled)
                    {
                        Logging.Info(this, $"{nameof(RegisterAsync)} Setting HttpClientHandler.Proxy");
                    }
                }

                DeviceProvisioningServiceRuntimeClient client = authStrategy.CreateClient(builder.Uri, httpClientHandler);
                client.HttpClient.DefaultRequestHeaders.Add("User-Agent", message.ProductInfo);
                if (Logging.IsEnabled) Logging.Info(this, $"Uri: {builder.Uri}; User-Agent: {message.ProductInfo}");

                DeviceRegistration deviceRegistration = authStrategy.CreateDeviceRegistration();

                string registrationId = message.Security.GetRegistrationID();

                RegistrationOperationStatus operation =
                    await client.RuntimeRegistration.RegisterDeviceAsync(
                        registrationId,
                        message.IdScope,
                        deviceRegistration).ConfigureAwait(false);

                int attempts = 0;
                string operationId = operation.OperationId;

                if (Logging.IsEnabled) Logging.RegisterDevice(
                    this,
                    registrationId,
                    message.IdScope,
                    deviceRegistration.Tpm == null ? "X509" : "TPM",
                    operation.OperationId,
                    operation.RetryAfter,
                    operation.Status);

                // Poll with operationId until registration complete.
                while (string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusAssigning) == 0 ||
                       string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusUnassigned) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task.Delay(
                        operation.RetryAfter ??
                        s_defaultOperationPoolingIntervalMilliseconds).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    operation = await client.RuntimeRegistration.OperationStatusLookupAsync(
                        registrationId,
                        operationId,
                        message.IdScope).ConfigureAwait(false);

                    if (Logging.IsEnabled) Logging.OperationStatusLookup(
                        this,
                        registrationId,
                        operation.OperationId,
                        operation.RetryAfter,
                        operation.Status,
                        attempts);

                    attempts++;
                }

                if (string.CompareOrdinal(operation.Status, RegistrationOperationStatus.OperationStatusAssigned) == 0)
                {
                    authStrategy.SaveCredentials(operation);
                }

                return ConvertToProvisioningRegistrationResult(operation.RegistrationState);
            }
            catch (HttpOperationException oe)
            {
                if (Logging.IsEnabled) Logging.Error(
                    this,
                    $"{nameof(ProvisioningTransportHandlerHttp)} threw exception {oe}",
                    nameof(RegisterAsync));

                bool isTransient = oe.Response.StatusCode >= HttpStatusCode.InternalServerError || (int)oe.Response.StatusCode == 429;

                try
                {
                    var errorDetails = JsonConvert.DeserializeObject<ProvisioningErrorDetails>(oe.Response.Content);
                    throw new ProvisioningTransportException(oe.Response.Content, oe, isTransient, errorDetails);
                }
                catch (JsonException ex)
                {
                    if (Logging.IsEnabled) Logging.Error(
                        this,
                        $"{nameof(ProvisioningTransportHandlerHttp)} server returned malformed error response." +
                        $"Parsing error: {ex}. Server response: {oe.Response.Content}",
                        nameof(RegisterAsync));

                    throw new ProvisioningTransportException(
                        $"HTTP transport exception: malformed server error message: '{oe.Response.Content}'",
                        ex,
                        false);
                }
            }
            catch (Exception ex) when (!(ex is ProvisioningTransportException))
            {
                if (Logging.IsEnabled) Logging.Error(
                    this,
                    $"{nameof(ProvisioningTransportHandlerHttp)} threw exception {ex}",
                    nameof(RegisterAsync));

                throw new ProvisioningTransportException($"HTTP transport exception", ex, true);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(ProvisioningTransportHandlerHttp)}.{nameof(RegisterAsync)}");
            }
        }

        private static DeviceRegistrationResult ConvertToProvisioningRegistrationResult(Models.DeviceRegistrationResult result)
        {
            var status = ProvisioningRegistrationStatusType.Failed;
            Enum.TryParse(result.Status, true, out status);

            var substatus = ProvisioningRegistrationSubstatusType.InitialAssignment;
            Enum.TryParse(result.Substatus, true, out substatus);

            return new DeviceRegistrationResult(
                result.RegistrationId,
                result.CreatedDateTimeUtc,
                result.AssignedHub,
                result.DeviceId,
                status,
                substatus,
                result.GenerationId,
                result.LastUpdatedDateTimeUtc,
                result.ErrorCode == null ? 0 : (int)result.ErrorCode,
                result.ErrorMessage,
                result.Etag);
        }
    }
}
