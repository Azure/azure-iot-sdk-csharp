﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Implementation of the IAmqpClientConnectionFactory interface
    /// </summary>
    internal class AmqpClientConnectionFactory : IAmqpClientConnectionFactory
    {
        public AmqpClientConnection Create(DeviceClientEndpointIdentity deviceClientEndpointIdentity, RemoveClientConnectionFromPool removeDelegate)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientConnectionFactory)}.{nameof(Create)}");

            if (deviceClientEndpointIdentity.GetType() == typeof(DeviceClientEndpointIdentitySasSingle))
            {
                return new AmqpClientConnectionSasSingle(deviceClientEndpointIdentity);
            }
            else if (deviceClientEndpointIdentity.GetType() == typeof(DeviceClientEndpointIdentityX509))
            {
                return new AmqpClientConnectionX509(deviceClientEndpointIdentity);
            }
            else if (deviceClientEndpointIdentity.GetType() == typeof(DeviceClientEndpointIdentityIoTHubSas))
            {
                return new AmqpClientConnectionIoTHubSas(deviceClientEndpointIdentity, removeDelegate);
            }
            else if (deviceClientEndpointIdentity.GetType() == typeof(DeviceClientEndpointIdentitySasMux))
            {
                return new AmqpClientConnectionSasMux(deviceClientEndpointIdentity, removeDelegate);
            }
            else
            {
                throw new ArgumentException("Unknown type of DeviceClientEndpointIdentity");
            }
        }
    }
}
