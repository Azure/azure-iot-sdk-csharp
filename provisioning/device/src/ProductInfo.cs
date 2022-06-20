// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    // TODO: Unify ProductInfo with DeviceClient.
    internal class ProductInfo
    {
        private const string Name = "Microsoft.Azure.Devices.Provisioning.Client";

        public string Extra { get; set; } = "";

        public override string ToString()
        {
            string version = typeof(ProvisioningDeviceClient).GetTypeInfo().Assembly.GetName().Version.ToString(3);
            string runtime = RuntimeInformation.FrameworkDescription.Trim();
            string operatingSystem = RuntimeInformation.OSDescription.Trim();
            string processorArchitecture = RuntimeInformation.ProcessArchitecture.ToString().Trim();

            string userAgent = $"{Name}/{version} ({runtime}; {operatingSystem}; {processorArchitecture})";

            if (!string.IsNullOrWhiteSpace(Extra))
            {
                userAgent += $" {Extra.Trim()}";
            }

            return userAgent;
        }
    }
}
