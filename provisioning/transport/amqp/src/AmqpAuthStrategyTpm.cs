// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class AmqpAuthStrategyTpm : AmqpAuthStrategy
    {
        private SecurityClientHsmTpm _security;

        public AmqpAuthStrategyTpm(SecurityClientHsmTpm security)
        {
            _security = security;
        }

        public override AmqpSettings CreateAmqpSettings(string linkendpoint)
        {
            var settings = new AmqpSettings();

            var saslProvider = new SaslTransportProvider();
            saslProvider.Versions.Add(AmqpConstants.DefaultProtocolVersion);
            settings.TransportProviders.Add(saslProvider);

            byte[] ekBuffer = _security.GetEndorsementKey();
            byte[] srkBuffer = _security.GetStorageRootKey();
            SaslTpmHandler tpmHandler = new SaslTpmHandler(ekBuffer, srkBuffer, linkendpoint, _security);
            saslProvider.AddHandler(tpmHandler);

            return settings;
        }

        public override Task OpenConnectionAsync(AmqpClientConnection connection, TimeSpan timeout, bool useWebSocket)
        {
            return connection.OpenAsync(timeout, useWebSocket, null);
        }

        public override void SaveCredentials(RegistrationOperationStatus operation)
        {
            if (operation?.RegistrationStatus?.Tpm?.AuthenticationKey == null)
            {
                if (Logging.IsEnabled) Logging.Error(
                    this,
                    $"Authentication key not found. OperationId=${operation?.OperationId}");

                throw new ProvisioningTransportException(
                    "Authentication key not found.",
                    false,
                    operation?.OperationId,
                    null);
            }

            byte[] key = Convert.FromBase64String(operation.RegistrationStatus.Tpm.AuthenticationKey);
            if (Logging.IsEnabled) Logging.DumpBuffer(this, key, nameof(operation.RegistrationStatus.Tpm.AuthenticationKey));

            _security.ActivateSymmetricIdentity(key);
        }
    }
}
