// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotCbsLink
    {
        private readonly AmqpCbsLink _amqpCbsLink;

        internal AmqpIotCbsLink(AmqpCbsLink amqpCbsLink)
        {
            _amqpCbsLink = amqpCbsLink;
        }

        public async Task<DateTime> SendTokenAsync(
            ICbsTokenProvider tokenProvider,
            Uri namespaceAddress,
            string audience,
            string resource,
            string[] requiredClaims,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(SendTokenAsync));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await _amqpCbsLink
                    .SendTokenAsync(tokenProvider, namespaceAddress, audience, resource, requiredClaims, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (AmqpException ex) when (!Fx.IsFatal(ex))
            {
                Exception iotEx = AmqpIotExceptionAdapter.ConvertToIotHubException(ex);
                if (ReferenceEquals(ex, iotEx))
                {
                    throw;
                }
                else
                {
                    throw iotEx;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(SendTokenAsync));
            }
        }
    }
}
