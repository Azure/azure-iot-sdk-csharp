// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using System.Net;
using System.Net.Security;
using System.Threading;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class AmqpAuthStrategyX509 : AmqpAuthStrategy
    {
        private SecurityProviderX509 _security;

        public AmqpAuthStrategyX509(SecurityProviderX509 security)
        {
            _security = security;
        }

        public override AmqpSettings CreateAmqpSettings(string idScope)
        {
            return new AmqpSettings();
        }

        public override Task OpenConnectionAsync(AmqpClientConnection connection, bool useWebSocket, IWebProxy proxy, RemoteCertificateValidationCallback remoteCertificateValidationCallback, CancellationToken cancellationToken)
        {
            X509Certificate2 clientCert = _security.GetAuthenticationCertificate();
            return connection.OpenAsync(useWebSocket, clientCert, proxy, remoteCertificateValidationCallback, cancellationToken);
        }

        public override void SaveCredentials(RegistrationOperationStatus status)
        {
            // no-op.
        }
    }
}
