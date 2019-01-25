// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Globalization;
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
        protected AmqpClientSession amqpClientSession { get; private set; }

        protected DeviceClientEndpointIdentity deviceClientEndpointIdentity { get; private set; }

        private AmqpCbsLink amqpCbsLink { get; set; }

        internal AmqpClientCbsLink(AmqpClientSession amqpClientSession, DeviceClientEndpointIdentity deviceClientEndpointIdentity)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientCbsLink)}");

            this.amqpClientSession = amqpClientSession;
            this.deviceClientEndpointIdentity = deviceClientEndpointIdentity;

            amqpCbsLink = new AmqpCbsLink(this.amqpClientSession.amqpClientConnection.amqpConnection);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(AmqpClientCbsLink)}");
        }
        #endregion

        #region Authenticate
        internal async Task<DateTime> AuthenticateCbsAsync(TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(AmqpClientCbsLink)}.{nameof(AuthenticateCbsAsync)}");

            DateTime expiresAtUtc;
            var timeoutHelper = new TimeoutHelper(timeout);

            string audience = this.deviceClientEndpointIdentity.iotHubConnectionString.AmqpEndpoint.AbsoluteUri;
            string resource = this.deviceClientEndpointIdentity.iotHubConnectionString.AmqpEndpoint.AbsoluteUri;

            expiresAtUtc = await amqpCbsLink.SendTokenAsync(
                deviceClientEndpointIdentity.iotHubConnectionString,
                deviceClientEndpointIdentity.iotHubConnectionString.AmqpEndpoint,
                audience,
                resource,
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
