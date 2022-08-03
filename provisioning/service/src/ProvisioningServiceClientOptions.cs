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
        /// Creates an instances of this class with the default transport settings.
        /// </summary>
        public ProvisioningServiceClientOptions()
        {
            ProvisioningServiceHttpSettings = new ProvisioningServiceHttpSettings();
        }

        /// <summary>
        /// The <see cref="ProvisioningServiceHttpSettings"/> transport settings to use.
        /// </summary>
        public ProvisioningServiceHttpSettings ProvisioningServiceHttpSettings { get; }
    }
}
