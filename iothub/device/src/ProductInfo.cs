// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Devices.Client
{
    internal class ProductInfo
    {
        public string Extra { get; set; } = "";

        private readonly Lazy<int> _productType = new Lazy<int>(() => NativeMethods.GetWindowsProductType());
        private readonly Lazy<string> _sqmId = new Lazy<string>(() => TelemetryMethods.GetSqmMachineId());

        public override string ToString()
        {
            const string Name = ".NET";
            string version = typeof(DeviceClient).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            string runtime = RuntimeInformation.FrameworkDescription.Trim();
            string operatingSystem = RuntimeInformation.OSDescription.Trim();
            string processorArchitecture = RuntimeInformation.ProcessArchitecture.ToString().Trim();
            string productType = (_productType.Value != 0) ? $" WindowsProduct:0x{_productType.Value:X8}" : string.Empty;
            string deviceId = (!string.IsNullOrWhiteSpace(_sqmId.Value)) ? _sqmId.Value : string.Empty;

            string[] agentInfoParts =
            {
                runtime,
                operatingSystem + productType,
                processorArchitecture,
                deviceId,
            };

            string userAgent = $"{Name}/{version} ({string.Join("; ", agentInfoParts.Where(x => !string.IsNullOrEmpty(x)))})";
            
            if (!String.IsNullOrWhiteSpace(this.Extra))
            {
                userAgent += $" {this.Extra.Trim()}";
            }

            return userAgent;
        }
    }
}
