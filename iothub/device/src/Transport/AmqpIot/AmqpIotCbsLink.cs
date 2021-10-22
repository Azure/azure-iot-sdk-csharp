// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Extensions;
using System.Threading;

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
            {
                Logging.Enter(this, nameof(SendTokenAsync));
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // TODO: azabbasi: CBS link on AMQP library needs to support cancellation tokens.
                return await _amqpCbsLink
                    .SendTokenAsync(tokenProvider, namespaceAddress, audience, resource, requiredClaims, TimeSpan.MaxValue)
                    .ConfigureAwait(false);
            }
            catch (AmqpException e) when (!e.IsFatal())
            {
                Exception ex = AmqpIotExceptionAdapter.ConvertToIotHubException(e);
                if (ReferenceEquals(e, ex))
                {
                    throw;
                }
                else
                {
                    throw ex;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, nameof(SendTokenAsync));
                }
            }
        }
    }
}
