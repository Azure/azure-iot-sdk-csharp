//
// Copyright (C) Microsoft.  All rights reserved.
//

namespace Microsoft.Azure.Devices.E2ETests.Discovery
{
    /// <summary>
    /// Appliance ID for Billing and Identity Flow
    /// </summary>
    public class ApplianceID
    {
        /// <summary>
        /// DeviceID Public Key
        /// </summary>
        public string DeviceIdPublicKey
        {
            get; set;
        }

        /// <summary>
        /// Version
        /// </summary>
        public string Version
        {
            get; set;
        }

        /// <summary>
        /// c'tor <see cref="ApplianceID"/> class.
        /// </summary>
        public ApplianceID(string deviceIdPublicKey, string version)
        {
            DeviceIdPublicKey = deviceIdPublicKey;
            Version = version;
        }
    }
}
