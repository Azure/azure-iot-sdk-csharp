// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal class AmqpAuthStrategyX509 : AmqpAuthStrategy
    {
        private readonly AuthenticationProviderX509 _authentication;

        public AmqpAuthStrategyX509(AuthenticationProviderX509 authentication)
        {
            _authentication = authentication;
        }

        public override AmqpSettings CreateAmqpSettings(string idScope)
        {
            return new AmqpSettings();
        }

        public override Task OpenConnectionAsync(
            AmqpClientConnection connection,
            bool useWebSocket,
            IWebProxy proxy,
            RemoteCertificateValidationCallback remoteCertificateValidationCallback,
            CancellationToken cancellationToken)
        {
            X509Certificate2 clientCert = _authentication.GetAuthenticationCertificate();
            return connection.OpenAsync(useWebSocket, clientCert, proxy, remoteCertificateValidationCallback, cancellationToken);
        }

        public override void SaveCredentials(RegistrationOperationStatus status)
        {
            // no-op.
        }
    }
}
