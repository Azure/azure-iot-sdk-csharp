// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Tests.Amqp
{
    public class MockableAmqpCbsLink
    {
        public MockableAmqpCbsLink() { }
        internal virtual Task<DateTime> SendTokenAsync(IotHubConnectionProperties credential, Uri amqpEndpoint, string audience, string resource, string[] strings, CancellationToken cancellationToken)
        {
            return Task.FromResult(DateTime.UtcNow.AddMinutes(15));
        }
    }
}
