// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Devices.Client
{
    internal class ProductInfo
    {
        public string Extra { get; set; } = "";

        private Lazy<int> _productType = new Lazy<int>(() => NativeMethods.GetWindowsProductType());

        public override string ToString()
        {
            const string Name = "Microsoft.Azure.Devices.Client";
            string version = typeof(DeviceClient).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            string runtime = RuntimeInformation.FrameworkDescription.Trim();
            string operatingSystem = RuntimeInformation.OSDescription.Trim();
            string processorArchitecture = RuntimeInformation.ProcessArchitecture.ToString().Trim();

            string userAgent = $"{Name}/{version} ({runtime}; {operatingSystem}; {processorArchitecture})";

            if (_productType.Value != -1)
            {
                userAgent += $" WindowsProduct:0x{_productType.Value:X8}";
            }

            if (!String.IsNullOrWhiteSpace(this.Extra))
            {
                userAgent += $" {this.Extra.Trim()}";
            }

            return userAgent;
        }
    }
}
