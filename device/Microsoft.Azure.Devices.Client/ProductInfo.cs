// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Devices.Client
{
    internal class ProductInfo
    {
        public string Extra { get; set; } = "";

        public override string ToString()
        {
            const string Name = "Microsoft.Azure.Devices.Client";
#if PCL
            // DO NOT EDIT the following line; it is updated by the bump_version script (https://github.com/Azure/iot-sdks-internals/blob/master/release/csharp/inputs.js)
            const string Version = "undefined"; // PCLAssemblyVersion

            string userAgent = $"{Name}/{Version} (PCL)";
#else
            // DO NOT EDIT the following line; it is updated by the bump_version script (https://github.com/Azure/iot-sdks-internals/blob/master/release/csharp/inputs.js)
            const string Version = "1.7.0-preview-001"; // CommonAssemblyVersion

            string runtime = RuntimeInformation.FrameworkDescription.Trim();
            string operatingSystem = RuntimeInformation.OSDescription.Trim();
            string processorArchitecture = RuntimeInformation.ProcessArchitecture.ToString().Trim();

            string userAgent = $"{Name}/{Version} ({runtime}; {operatingSystem}; {processorArchitecture})";
#endif

            if (!String.IsNullOrWhiteSpace(this.Extra))
            {
                userAgent += $" {this.Extra.Trim()}";
            }

            return userAgent;
        }
    }
}


