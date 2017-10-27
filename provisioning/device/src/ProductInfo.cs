// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

            // TODO: Replace with Assembly information.
            // DO NOT EDIT the following line; it is updated by the bump_version script (https://github.com/Azure/iot-sdks-internals/blob/master/release/csharp/inputs.js)
            const string Version = "1.5.2"; // CommonAssemblyVersion

            string runtime = RuntimeInformation.FrameworkDescription.Trim();
            string operatingSystem = RuntimeInformation.OSDescription.Trim();
            string processorArchitecture = RuntimeInformation.ProcessArchitecture.ToString().Trim();

            string userAgent = $"{Name}/{Version} ({runtime}; {operatingSystem}; {processorArchitecture})";

            if (!String.IsNullOrWhiteSpace(this.Extra))
            {
                userAgent += $" {this.Extra.Trim()}";
            }

            return userAgent;
        }
    }
}
