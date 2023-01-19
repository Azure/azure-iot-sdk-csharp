// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    // Methods and properties included here are not part of the core functionality of the client,
    // so we will not concern ourselves with code coverage of this class.
    [ExcludeFromCodeCoverage]
    internal class ProductInfo
    {
        public string Extra { get; set; } = "";

        public override string ToString()
        {
            const string name = "Microsoft.Azure.Devices.Provisioning.Client";

            string version = typeof(ProvisioningDeviceClient).GetTypeInfo().Assembly.GetName().Version.ToString(3);
            string runtime = RuntimeInformation.FrameworkDescription.Trim();
            string operatingSystem = RuntimeInformation.OSDescription.Trim();
            string processorArchitecture = RuntimeInformation.ProcessArchitecture.ToString().Trim();

            string userAgent = $"{name}/{version} ({runtime}; {operatingSystem}; {processorArchitecture})";

            if (!string.IsNullOrWhiteSpace(Extra))
            {
                userAgent += $" {Extra.Trim()}";
            }

            return userAgent;
        }
    }
}
