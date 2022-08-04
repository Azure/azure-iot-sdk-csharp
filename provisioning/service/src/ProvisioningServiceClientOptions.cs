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
