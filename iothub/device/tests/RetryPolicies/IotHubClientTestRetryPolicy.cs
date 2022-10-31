// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client.Test
{
    internal class IotHubClientTestRetryPolicy : IotHubClientRetryPolicyBase
    {
        public IotHubClientTestRetryPolicy(uint maxRetries)
            : base(maxRetries)
        {
        }

        public new TimeSpan UpdateWithJitter(double baseTimeMs)
        {
            return base.UpdateWithJitter(baseTimeMs);
        }
    }
}
