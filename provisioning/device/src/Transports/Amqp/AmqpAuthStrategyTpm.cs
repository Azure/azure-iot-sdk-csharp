// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Devices.Authentication;
using Tpm2Lib;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal class AmqpAuthStrategyTpm : AmqpAuthStrategy
    {
        private readonly AuthenticationProviderTpm _authentication;

        public AmqpAuthStrategyTpm(AuthenticationProviderTpm authentication)
        {
            _authentication = authentication;
        }

        public override AmqpSettings CreateAmqpSettings(string idScope)
        {
            var settings = new AmqpSettings();

            var saslProvider = new SaslTransportProvider();
            saslProvider.Versions.Add(AmqpConstants.DefaultProtocolVersion);
            settings.TransportProviders.Add(saslProvider);

            byte[] ekBuffer = Array.Empty<byte>();
            byte[] srkBuffer = Array.Empty<byte>();
            try
            {
                ekBuffer = _authentication.GetEndorsementKey();
                srkBuffer = _authentication.GetStorageRootKey();
            }
            catch (Exception ex) when (ex is TssException || ex is TpmException)
            {
                throw new DeviceProvisioningClientException(ex.Message, ex, false);
            }

            var tpmHandler = new SaslTpmHandler(ekBuffer, srkBuffer, idScope, _authentication);
            saslProvider.AddHandler(tpmHandler);

            return settings;
        }

        public override Task OpenConnectionAsync(
            AmqpClientConnection connection,
            bool useWebSocket,
            IWebProxy proxy,
            RemoteCertificateValidationCallback remoteCertificateValidationCallback,
            CancellationToken cancellationToken)
        {
            return connection.OpenAsync(useWebSocket, null, proxy, remoteCertificateValidationCallback, cancellationToken);
        }

        public override void SaveCredentials(RegistrationOperationStatus operation)
        {
            if (operation?.RegistrationState?.Tpm?.AuthenticationKey == null)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Authentication key not found. OperationId=${operation?.OperationId}");

                throw new DeviceProvisioningClientException("Authentication key not found.", null, false);
            }

            byte[] key = Convert.FromBase64String(operation.RegistrationState.Tpm.AuthenticationKey);
            if (Logging.IsEnabled)
                Logging.DumpBuffer(this, key, nameof(operation.RegistrationState.Tpm.AuthenticationKey));

            try
            {
                _authentication.ActivateIdentityKey(key);
            }
            catch (Exception ex) when (ex is TssException || ex is TpmException)
            {
                throw new DeviceProvisioningClientException(ex.Message, ex, false);
            }
        }
    }
}
