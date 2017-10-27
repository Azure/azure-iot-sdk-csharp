// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class AmqpAuthStrategyX509 : AmqpAuthStrategy
    {
        SecurityClientHsmX509 _security;

        public AmqpAuthStrategyX509(SecurityClientHsmX509 security)
        {
            _security = security;
        }

        public override AmqpSettings CreateAmqpSettings(string linkendpoint)
        {
            return new AmqpSettings();
        }

        public override Task OpenConnectionAsync(AmqpClientConnection connection, TimeSpan timeout, bool useWebSocket)
        {
            X509Certificate2 clientCert = _security.GetAuthenticationCertificate();
            return connection.OpenAsync(timeout, useWebSocket, clientCert);
        }

        public override void SaveCredentials(RegistrationOperationStatus status)
        {
            // no-op.
        }
    }
}
