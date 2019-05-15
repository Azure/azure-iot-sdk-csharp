// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTCbsLink : IAmqpIoTCbsLink
    {
        private AmqpCbsLink _amqpCbsLink;

        internal AmqpIoTCbsLink(AmqpConnection amqpConnection)
        {
            _amqpCbsLink = _amqpCbsLink ?? new AmqpCbsLink(amqpConnection);
        }

        public async Task<DateTime> SendTokenAsync(ICbsTokenProvider tokenProvider, Uri namespaceAddress, string audience, string resource, string[] requiredClaims, TimeSpan timeout)
        {
            try
            {
                return await _amqpCbsLink.SendTokenAsync(tokenProvider, namespaceAddress, audience, resource, requiredClaims, timeout).ConfigureAwait(false);
            }
            catch (AmqpException ex)
            {
                throw new IotHubCommunicationException("AmqpIoTCbsLink.SendTokenAsync error", ex.InnerException);
            }
        }

        public void Close()
        {
            try
            {
                _amqpCbsLink.Close();
            }
            catch (AmqpException ex)
            {
                throw new IotHubCommunicationException("AmqpIoTCbsLink.Close error", ex.InnerException);
            }
        }
    }
}
