// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Tests.Amqp
{
    public class MockableAmqpCbsLink
    {
        /// <summary>
        /// Since AmqpCbsLink from Amqp library is not overridable, this mockable class was created.
        /// </summary>
        public MockableAmqpCbsLink() { }

        internal virtual Task<DateTime> SendTokenAsync(IotHubConnectionProperties credential, Uri amqpEndpoint, string audience, string resource, string[] strings, CancellationToken cancellationToken)
        {
            return Task.FromResult(DateTime.UtcNow.AddMinutes(15));
        }
    }
}
