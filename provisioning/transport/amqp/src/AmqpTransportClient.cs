// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport.Http.Models;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{
    public class AmqpTransportClient : ProvisioningTransportClient
    {
        public const int DefaultOperationPoolingIntervalMilliseconds = 2 * 1000;

        public TransportFallbackType FallbackType { get; private set; }

        public AmqpTransportClient(
            TransportFallbackType transportFallbackType = TransportFallbackType.TcpWithWebSocketFallback)
        {
            FallbackType = transportFallbackType;
        }

        public override async Task<ProvisioningRegistrationResult> RegisterAsync(
            string globalDeviceEndpoint, 
            string idScope, 
            ProvisioningSecurityClient securityClient, 
            CancellationToken cancellationToken)
        {
            AmqpClientConnection client;

            if (securityClient is ProvisioningSecurityClientSasToken)
            {
                client = await AmqpTransportHandler.CreateAmqpCloudConnectionAsync(
                    globalDeviceEndpoint,
                    null,
                    idScope + $"/registrations/{securityClient.RegistrationID}",
                    false,
                    (ProvisioningSecurityClientSasToken)securityClient).ConfigureAwait(false);
            }
            //else if (securityClient is ProvisioningSecurityClientX509Certificate)
            //{
            //    //TODO: replace with ProvisioningSecurityClientX509Certificate
            //    client = await AmqpRawClient.CreateAmqpCloudConnectionAsync(
            //        globalDeviceEndpoint,
            //        null,
            //        idScope + $"/registrations/{securityClient.RegistrationID}",
            //        false,
            //        (ProvisioningSecurityClientX509Certificate)securityClient).ConfigureAwait(false);
            //}
            else
            {
                throw new NotSupportedException(
                    $"{nameof(securityClient)} must be of type {nameof(ProvisioningSecurityClientSasToken)} " +
                    $"or {nameof(ProvisioningSecurityClientX509Certificate)}");
            }

            string correlationId = new Guid().ToString();
            RegistrationOperationStatus operation = 
                await AmqpTransportHandler.RegisterDeviceAsync(client, correlationId).ConfigureAwait(false);

            // Poll with operationId until registration complete.
            int attempts = 0;
            string operationId = operation.OperationId;

            while (string.Equals(operation.Status, "assigning", StringComparison.OrdinalIgnoreCase))
            {
                attempts++;
                operation = await AmqpTransportHandler.OperationStatusLookupAsync(client, 
                    operationId, 
                    correlationId).ConfigureAwait(false);

                await Task.Delay(DefaultOperationPoolingIntervalMilliseconds).ConfigureAwait(false);
            }

            if (operation.Status != "assigned")
            {
                throw new InvalidOperationException("Failed to register. Status = " + operation.Status);
            }

            return AmqpTransportHandler.ConvertToProvisioningRegistrationResult(operation.RegistrationStatus);
        }

        public override Task CloseAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}
