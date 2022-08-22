// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Options that allow configuration of the provisioning service client instance during initialization.
    /// </summary>
    public class ProvisioningServiceClientOptions
    {
        /// <summary>
        /// The transport settings to use.
        /// </summary>
        public ProvisioningServiceHttpSettings ProvisioningServiceHttpSettings { get; } = new();
    }
}
