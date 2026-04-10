// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Win32;

namespace Microsoft.Azure.Devices.Client
{
    internal class ProductInfo
    {
        public string Extra { get; set; } = "";

        private readonly Lazy<int> _productType = new Lazy<int>(() => GetWindowsProductType());
        private readonly Lazy<string> _sqmId = new Lazy<string>(() => GetSqmMachineId());

        public override string ToString()
        {
            return ToString(UserAgentFormats.Default);
        }

        internal string ToString(UserAgentFormats format)
        {
            switch (format)
            {
                case UserAgentFormats.Http:
                    return ToString("{runtime}; {operatingSystem}; {architecture}");
                default:
                    return ToString(null);
            }
        }

        /// <summary>
        /// <para>Specify the format of the content in the parentheses of the UserAgent string</para>
        /// <para>Example: "{runtime}; {operatingSystem}; {architecture}; {deviceId}"</para>
        /// </summary>
        /// <param name="format"></param>
        private string ToString(string format)
        {
            const string name = ".NET";
            string version = string.Empty;
            string infoParts = string.Empty;

            try
            {
                version = typeof(DeviceClient).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
                string runtime = RuntimeInformation.FrameworkDescription.Trim();
                string operatingSystem = RuntimeInformation.OSDescription.Trim();
                string processorArchitecture = RuntimeInformation.ProcessArchitecture.ToString().Trim();
                string productType = (_productType.Value != 0) ? $" WindowsProduct:0x{_productType.Value:X8}" : string.Empty;
                string deviceId = (!string.IsNullOrWhiteSpace(_sqmId.Value)) ? _sqmId.Value : string.Empty;

                if (!string.IsNullOrWhiteSpace(format))
                {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
                    infoParts = format
                        .Replace("{runtime}", runtime, StringComparison.InvariantCultureIgnoreCase)
                        .Replace("{operatingSystem}", operatingSystem + productType, StringComparison.InvariantCultureIgnoreCase)
                        .Replace("{architecture}", processorArchitecture, StringComparison.InvariantCultureIgnoreCase)
                        .Replace("{deviceId}", deviceId, StringComparison.InvariantCultureIgnoreCase);
#else
                    infoParts = format
                        .Replace("{runtime}", runtime)
                        .Replace("{operatingSystem}", operatingSystem + productType)
                        .Replace("{architecture}", processorArchitecture)
                        .Replace("{deviceId}", deviceId);
#endif
                }
                else
                {
                    string[] agentInfoParts =
                    {
                        runtime,
                        operatingSystem + productType,
                        processorArchitecture,
                        deviceId,
                    };

                    infoParts = string.Join("; ", agentInfoParts.Where(x => !string.IsNullOrEmpty(x)));
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                // no-op
            }

            string userAgent = $"{name}/{version} ({infoParts})";

            if (!string.IsNullOrWhiteSpace(Extra))
            {
                userAgent += $" {Extra.Trim()}";
            }

            return userAgent;
        }

        internal static string GetSqmMachineId()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\SQMClient");
                    if (key != null)
                    {
                        return key.GetValue("MachineId") as string;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Assert(false, ex.Message);

                    if (Logging.IsEnabled)
                        Logging.Error(null, ex, nameof(ProductInfo));
                }
            }

            return null;
        }

        internal static int GetWindowsProductType()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    && GetProductInfo(Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor, 0, 0, out int productType))
                {
                    return productType;
                }
            }
            catch (DllNotFoundException ex)
            {
                // Catch any DLL not found exceptions
                Debug.Assert(false, ex.Message);

                if (Logging.IsEnabled)
                    Logging.Error(null, ex, nameof(ProductInfo));
            }

            return 0;
        }

        [DllImport("kernel32.dll", SetLastError = false)]
        private static extern bool GetProductInfo(
           int dwOSMajorVersion,
           int dwOSMinorVersion,
           int dwSpMajorVersion,
           int dwSpMinorVersion,
           out int pdwReturnedProductType);
    }

    internal enum UserAgentFormats
    {
        Default,
        Http,
    };
}
