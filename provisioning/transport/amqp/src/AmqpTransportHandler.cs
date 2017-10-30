// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Provisioning.Client.Transport.Http.Models;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{ 
    internal static class AmqpTransportHandler
    {
        private static readonly TimeSpan TimeoutConstant = TimeSpan.FromMinutes(1);

        private static async Task<AmqpClientConnection> CreateConnection(Uri uri,
            ProvisioningSecurityClient securityClient, string linkendpoint)
        {
            AmqpSettings settings = await CreateAmqpSettings(securityClient, linkendpoint).ConfigureAwait(false);
            return new AmqpClientConnection(uri, settings);
        }

        private static async Task<AmqpSettings> CreateAmqpSettings(ProvisioningSecurityClient securityClient,
            string linkendpoint)
        {
            var settings = new AmqpSettings();

            var saslProvider = new SaslTransportProvider();
            saslProvider.Versions.Add(AmqpConstants.DefaultProtocolVersion);
            settings.TransportProviders.Add(saslProvider);

            if (securityClient is ProvisioningSecurityClientSasToken)
            {
                var tpmSecurityClient = (ProvisioningSecurityClientSasToken) securityClient;
                byte[] ekBuffer = await tpmSecurityClient.GetEndorsementKeyAsync().ConfigureAwait(false);
                byte[] srkBuffer = await tpmSecurityClient.GetStorageRootKeyAsync().ConfigureAwait(false);
                SaslTpmHandler tpmHandler = new SaslTpmHandler(ekBuffer, srkBuffer, linkendpoint, tpmSecurityClient);
                saslProvider.AddHandler(tpmHandler);
            }

            var amqpProvider = new AmqpTransportProvider();
            amqpProvider.Versions.Add(AmqpConstants.DefaultProtocolVersion);
            settings.TransportProviders.Add(amqpProvider);
            return settings;
        }

        internal static async Task<AmqpClientConnection> CreateAmqpCloudConnectionAsync(
            string deviceEndpoint,
            string linkEndpoint,
            bool useWebSocket,
            ProvisioningSecurityClient securityClient)
        {
            AmqpClientConnection amqpClientConnection;
            if (useWebSocket)
            {
                // TODO: enable WS
                amqpClientConnection = await CreateConnection(
                    new Uri(WebSocketConstants.Scheme + deviceEndpoint + ":" + WebSocketConstants.Port),
                    securityClient,
                    linkEndpoint).ConfigureAwait(false);
            }
            else
            {
                amqpClientConnection = await CreateConnection(
                    new Uri("amqps://" + deviceEndpoint + ":" + AmqpConstants.DefaultSecurePort),
                    securityClient,
                    linkEndpoint).ConfigureAwait(false);
            }

            X509Certificate2 clientCert = null;
            if (securityClient is ProvisioningSecurityClientX509Certificate)
            {
                clientCert = await ((ProvisioningSecurityClientX509Certificate) securityClient)
                    .GetAuthenticationCertificate().ConfigureAwait(false);
            }

            await amqpClientConnection.OpenAsync(TimeoutConstant, useWebSocket, clientCert)
                .ConfigureAwait(false);

            var amqpDeviceSession = amqpClientConnection.CreateSession();
            await amqpDeviceSession.OpenAsync(TimeoutConstant).ConfigureAwait(false);

            var amqpReceivingLink = amqpDeviceSession.CreateReceivingLink(linkEndpoint);

            amqpReceivingLink.AddClientVersion(ClientApiVersionHelper.ClientVersion);
            amqpReceivingLink.AddApiVersion(ClientApiVersionHelper.ApiVersion);

            await amqpReceivingLink.OpenAsync(TimeoutConstant).ConfigureAwait(false);

            var amqpSendingLink = amqpDeviceSession.CreateSendingLink(linkEndpoint);

            amqpSendingLink.AddClientVersion(ClientApiVersionHelper.ClientVersion);
            amqpSendingLink.AddApiVersion(ClientApiVersionHelper.ApiVersion);

            await amqpSendingLink.OpenAsync(TimeoutConstant).ConfigureAwait(false);

            return amqpClientConnection;
        }

        public static async Task<RegistrationOperationStatus> RegisterDeviceAsync(AmqpClientConnection client,
            string correlationId)
        {
            var amqpMessage = AmqpMessage.Create(new MemoryStream(Encoding.ASCII.GetBytes(DeviceOperations.Register)),
                false);
            amqpMessage.Properties.CorrelationId = correlationId;
            amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.OperationType] =
                DeviceOperations.Register;
            amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.ForceRegistration] = false;
            var outcome = await client.AmqpSession.SendingLink
                .SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()),
                    TimeoutConstant).ConfigureAwait(false);
            var amqpResponse = await client.AmqpSession.ReceivingLink.ReceiveMessageAsync(TimeoutConstant)
                .ConfigureAwait(false);
            client.AmqpSession.ReceivingLink.AcceptMessage(amqpResponse);
            string jsonResponse = await new StreamReader(amqpResponse.BodyStream).ReadToEndAsync()
                .ConfigureAwait(false);
            return JsonConvert.DeserializeObject<RegistrationOperationStatus>(jsonResponse);
        }

        public static async Task<RegistrationOperationStatus> OperationStatusLookupAsync(AmqpClientConnection client,
            string operationId, string correlationId)
        {
            var amqpMessage =
                AmqpMessage.Create(new MemoryStream(Encoding.ASCII.GetBytes(DeviceOperations.GetOperationStatus)),
                    false);
            amqpMessage.Properties.CorrelationId = correlationId;
            amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.OperationType] =
                DeviceOperations.GetOperationStatus;
            amqpMessage.ApplicationProperties.Map[MessageApplicationPropertyNames.OperationId] = operationId;
            var outcome = await client.AmqpSession.SendingLink
                .SendMessageAsync(amqpMessage, new ArraySegment<byte>(Guid.NewGuid().ToByteArray()),
                    TimeoutConstant).ConfigureAwait(false);
            var amqpResponse = await client.AmqpSession.ReceivingLink.ReceiveMessageAsync(TimeoutConstant)
                .ConfigureAwait(false);
            client.AmqpSession.ReceivingLink.AcceptMessage(amqpResponse);
            string jsonResponse = await new StreamReader(amqpResponse.BodyStream).ReadToEndAsync()
                .ConfigureAwait(false);
            return JsonConvert.DeserializeObject<RegistrationOperationStatus>(jsonResponse);
        }

        public static ProvisioningRegistrationResult ConvertToProvisioningRegistrationResult(
            DeviceRegistrationResult result)
        {
            var status = ProvisioningRegistrationStatusType.Failed;
            Enum.TryParse(result.Status, true, out status);

            return new ProvisioningRegistrationResultTpm(
                result.RegistrationId,
                result.CreatedDateTimeUtc,
                result.AssignedHub,
                result.DeviceId,
                status,
                result.GenerationId,
                result.LastUpdatedDateTimeUtc,
                result.ErrorCode == null ? 0 : (int) result.ErrorCode,
                result.ErrorMessage,
                result.Etag,
                result.Tpm.AuthenticationKey);
        }
    }
}
