// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    // TODO: Unify ProductInfo with DeviceClient.
    internal class ProductInfo
    {
        public string Extra { get; set; } = "";

        public override string ToString()
        {
            const string Name = "Microsoft.Azure.Devices.Provisioning.Client";

            string version = typeof(ProvisioningDeviceClient).GetTypeInfo().Assembly.GetName().Version.ToString(3);
            string runtime = RuntimeInformation.FrameworkDescription.Trim();
            string operatingSystem = RuntimeInformation.OSDescription.Trim();
            string processorArchitecture = RuntimeInformation.ProcessArchitecture.ToString().Trim();

            string userAgent = $"{Name}/{version} ({runtime}; {operatingSystem}; {processorArchitecture})";

            if (!String.IsNullOrWhiteSpace(Extra))
            {
                userAgent += $" {Extra.Trim()}";
            }

            return userAgent;
        }
    }
}
