// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    public class ProvisioningTransportHandlerHttp : ProvisioningTransportHandler
    {
        private static readonly TimeSpan s_defaultOperationPoolingIntervalMilliseconds = TimeSpan.FromSeconds(2);

        public async override Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterMessage message,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(ProvisioningTransportHandlerHttp)}.{nameof(RegisterAsync)}");

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                HttpAuthStrategy authStrategy;

                if (message.Security is SecurityClientHsmTpm)
                {
                    authStrategy = new HttpAuthStrategyTpm((SecurityClientHsmTpm)message.Security);
                }
                else if (message.Security is SecurityClientHsmX509)
                {
                    authStrategy = new HttpAuthStrategyX509((SecurityClientHsmX509)message.Security);
                }
                else
                {
                    if (Logging.IsEnabled) Logging.Error(this, $"Invalid {nameof(SecurityClient)} type.");
                    throw new NotSupportedException(
                        $"{nameof(message.Security)} must be of type {nameof(SecurityClientHsmTpm)} " +
                        $"or {nameof(SecurityClientHsmX509)}");
                }

                if (Logging.IsEnabled) Logging.Associate(authStrategy, this);

                var builder = new UriBuilder()
                {
                    Scheme = Uri.UriSchemeHttps,
                    Host = message.GlobalDeviceEndpoint
                };

                DeviceProvisioningServiceRuntimeClient client = authStrategy.CreateClient(builder.Uri);
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

                return ConvertToProvisioningRegistrationResult(operation.RegistrationStatus);
            }
            // TODO: Catch only expected exceptions from HTTP and REST.
            catch (Exception ex)
            {
                if (Logging.IsEnabled) Logging.Error(
                    this, 
                    $"{nameof(ProvisioningTransportHandlerHttp)} threw exception {ex}", 
                    nameof(RegisterAsync));

                // TODO: Extract trackingId from the exception.
                throw new ProvisioningTransportException($"HTTP transport exception", true, "", ex);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(ProvisioningTransportHandlerHttp)}.{nameof(RegisterAsync)}");
            }
        }

        private DeviceRegistrationResult ConvertToProvisioningRegistrationResult(Models.DeviceRegistrationResult result)
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
