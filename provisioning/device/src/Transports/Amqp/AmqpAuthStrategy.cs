// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Transport;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal abstract class AmqpAuthStrategy
    {
        public virtual AmqpClientConnection CreateConnection(Uri uri, string idScope)
        {
            AmqpSettings settings = CreateAmqpSettings(idScope);
            var amqpProvider = new AmqpTransportProvider();
            amqpProvider.Versions.Add(AmqpConstants.DefaultProtocolVersion);
            settings.TransportProviders.Add(amqpProvider);

            return new AmqpClientConnection(uri, settings);
        }

        public abstract AmqpSettings CreateAmqpSettings(string idScope);

        public abstract Task OpenConnectionAsync(
            AmqpClientConnection connection,
            bool useWebSocket,
            IWebProxy proxy,
            RemoteCertificateValidationCallback remoteCertificateValidationCallback,
            CancellationToken cancellationToken);

        public abstract void SaveCredentials(RegistrationOperationStatus status);
    }
}
