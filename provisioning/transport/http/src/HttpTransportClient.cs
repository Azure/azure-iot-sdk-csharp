// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport.Http.Models;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Http
{
    public class HttpTransportClient : ProvisioningTransportClient
    {
        public const int DefaultOperationPoolingIntervalMilliseconds = 2 * 1000;
        private const string OperationStatusAssigning = "assigning";

        public async override Task<ProvisioningRegistrationResult> RegisterAsync(
            string globalDeviceEndpoint, 
            string idScope, 
            ProvisioningSecurityClient securityClient,
            CancellationToken cancellationToken)
        {
            HttpTransportHandler transportHandler;

            if (securityClient is ProvisioningSecurityClientSasToken)
            {
                transportHandler = new HttpTransportHandlerTpm((ProvisioningSecurityClientSasToken)securityClient);
            }
            else if (securityClient is ProvisioningSecurityClientX509Certificate)
            {
                transportHandler = new HttpTransportHandlerX509((ProvisioningSecurityClientX509Certificate)securityClient);
            }
            else
            {
                throw new NotSupportedException(
                    $"{nameof(securityClient)} must be of type {nameof(ProvisioningSecurityClientSasToken)} " +
                    $"or {nameof(ProvisioningSecurityClientX509Certificate)}");
            }

            var builder = new UriBuilder()
            {
                Scheme = Uri.UriSchemeHttps,
                Host = globalDeviceEndpoint
            };

            DeviceProvisioningServiceRuntimeClient client = 
                await transportHandler.CreateClient(builder.Uri).ConfigureAwait(false);

            DeviceRegistration deviceRegistration = 
                await transportHandler.CreateDeviceRegistration().ConfigureAwait(false);

            RegistrationOperationStatus operation = 
                await client.RuntimeRegistration.RegisterDeviceAsync(
                    securityClient.RegistrationID,
                    idScope,
                    deviceRegistration).ConfigureAwait(false);

            // Poll with operationId until registration complete.
            int attempts = 0;
            string operationId = operation.OperationId;

            while (operation.Status == OperationStatusAssigning)
            {
                attempts++;
                operation = await client.RuntimeRegistration.OperationStatusLookupAsync(
                    securityClient.RegistrationID,
                    operationId,
                    idScope).ConfigureAwait(false);

                await Task.Delay(DefaultOperationPoolingIntervalMilliseconds).ConfigureAwait(false);
            }

            return transportHandler.ConvertToProvisioningRegistrationResult(operation.RegistrationStatus);
        }
    }
}
