// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal class ProvisioningErrorDetailsHttp : ProvisioningErrorDetails
    {
        /// <summary>
        /// The time to wait before trying again if this error is transient
        /// </summary>
        internal TimeSpan? RetryAfter { get; set; }
    }
}
