// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Common.Extensions
{
    /// <summary>
    /// Contains extension methods for amqp
    /// </summary>
    internal static class AmqpExtensions
    {
        internal static async Task<ReceivingAmqpLink> GetReceivingLinkAsync(this FaultTolerantAmqpObject<ReceivingAmqpLink> faultTolerantReceivingLink, TimeSpan? timeout = null)
        {
            if (!faultTolerantReceivingLink.TryGetOpenedObject(out ReceivingAmqpLink receivingLink))
            {
                timeout ??= IotHubConnection.DefaultOperationTimeout;
                receivingLink = await faultTolerantReceivingLink.GetOrCreateAsync(timeout.Value).ConfigureAwait(false);
            }

            return receivingLink;
        }
    }
}
