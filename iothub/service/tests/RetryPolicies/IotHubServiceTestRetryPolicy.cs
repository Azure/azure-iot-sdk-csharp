// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Tests
{
    internal class IotHubServiceTestRetryPolicy : IotHubServiceRetryPolicyBase
    {
        public IotHubServiceTestRetryPolicy(uint maxRetries)
            : base(maxRetries)
        {
        }

        public new TimeSpan UpdateWithJitter(double baseTimeMs)
        {
            return base.UpdateWithJitter(baseTimeMs);
        }
    }
}
