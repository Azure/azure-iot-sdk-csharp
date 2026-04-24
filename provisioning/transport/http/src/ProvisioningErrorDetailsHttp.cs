// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    [SuppressMessage("Microsoft.Performance", "CA1812", Justification = "Is instantiated by json convertor")]
    internal class ProvisioningErrorDetailsHttp : ProvisioningErrorDetails
    {
        /// <summary>
        /// The time to wait before trying again if this error is transient
        /// </summary>
        internal TimeSpan? RetryAfter { get; set; }
    }
}
