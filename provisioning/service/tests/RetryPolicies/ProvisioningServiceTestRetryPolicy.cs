// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service.Tests
{
    // This class is for unit testing of ProvisioningServiceRetryPolicyBase.
    internal class ProvisioningServiceTestRetryPolicy : ProvisioningServiceRetryPolicyBase
    {
        public ProvisioningServiceTestRetryPolicy(uint maxRetries)
            : base(maxRetries)
        {
        }

        public new TimeSpan UpdateWithJitter(double baseTimeMs)
        {
            return base.UpdateWithJitter(baseTimeMs);
        }
    }
}
