// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTCbsLink
    {
        private AmqpCbsLink _amqpCbsLink;

        internal AmqpIoTCbsLink(AmqpConnection amqpConnection)
        {
            _amqpCbsLink = _amqpCbsLink ?? new AmqpCbsLink(amqpConnection);
        }

        public async Task<DateTime> SendTokenAsync(ICbsTokenProvider tokenProvider, Uri namespaceAddress, string audience, string resource, string[] requiredClaims, TimeSpan timeout)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(SendTokenAsync)}");
            try
            {
                return await _amqpCbsLink.SendTokenAsync(tokenProvider, namespaceAddress, audience, resource, requiredClaims, timeout).ConfigureAwait(false);
            }
            catch (AmqpException ex)
            {
                throw new IotHubCommunicationException("AmqpIoTCbsLink.SendTokenAsync error", ex.InnerException);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(SendTokenAsync)}");
            }
        }

        public void Close()
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(Close)}");
            try
            {
                _amqpCbsLink.Close();
            }
            catch (AmqpException ex)
            {
                throw new IotHubCommunicationException("AmqpIoTCbsLink.Close error", ex.InnerException);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(Close)}");
            }
        }
    }
}
