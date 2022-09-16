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

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal class AmqpAuthStrategySymmetricKey : AmqpAuthStrategy
    {
        private readonly AuthenticationProviderSymmetricKey _authentication;

        public AmqpAuthStrategySymmetricKey(AuthenticationProviderSymmetricKey authentication)
        {
            _authentication = authentication;
        }

        public override AmqpSettings CreateAmqpSettings(string idScope)
        {
            var settings = new AmqpSettings();

            var saslProvider = new SaslTransportProvider();
            saslProvider.Versions.Add(AmqpConstants.DefaultProtocolVersion);
            settings.TransportProviders.Add(saslProvider);

            var saslHandler = new SaslPlainHandler();
            saslHandler.AuthenticationIdentity = $"{idScope}/registrations/{_authentication.GetRegistrationId()}";
            string key = _authentication.GetPrimaryKey();
            saslHandler.Password = ProvisioningSasBuilder.BuildSasSignature(key, saslHandler.AuthenticationIdentity, TimeSpan.FromDays(1));
            saslProvider.AddHandler(saslHandler);

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
        }
    }
}
