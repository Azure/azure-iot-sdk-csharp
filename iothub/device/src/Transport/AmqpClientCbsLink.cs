// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Special link for CBS authentication
    /// Supports creation, authentication and token refresh
    /// The CbsLink closing event closes the owner session in the Amqp library, 
    /// so connection layer should handle the session close (e.g. use separate session for authentication)
    /// </summary>
    internal class AmqpClientCbsLink
    {
        #region Members-Constructor
        private AmqpCbsLink amqpCbsLink { get; set; }

        internal AmqpClientCbsLink(AmqpConnection amqpConnection)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientCbsLink)}");

            amqpCbsLink = new AmqpCbsLink(amqpConnection);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientCbsLink)}");
        }
        #endregion

        #region Authenticate
        internal async Task<DateTime> AuthenticateCbsAsync(DeviceClientEndpointIdentity deviceClientEndpointIdentity, string audience, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientCbsLink)}.{nameof(AuthenticateCbsAsync)}");

            DateTime expiresAtUtc;

            expiresAtUtc = await amqpCbsLink.SendTokenAsync(
                deviceClientEndpointIdentity.iotHubConnectionString,
                deviceClientEndpointIdentity.iotHubConnectionString.AmqpEndpoint,
                audience,
                deviceClientEndpointIdentity.iotHubConnectionString.AmqpEndpoint.AbsoluteUri,
                AccessRightsHelper.AccessRightsToStringArray(AccessRights.DeviceConnect), 
                timeout).ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientCbsLink)}.{nameof(AuthenticateCbsAsync)}");

            return expiresAtUtc;
        }

        internal async Task CloseLink(TimeSpan timeout)
        {
            if (amqpCbsLink != null)
            {
                await CloseLink(timeout).ConfigureAwait(false);
            }
        }
        #endregion
    }
}
