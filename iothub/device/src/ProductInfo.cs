// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client
{
    internal class ProductInfo
    {
        public string Extra { get; set; } = "";

        private readonly Lazy<int> _productType = new Lazy<int>(() => NativeMethods.GetWindowsProductType());
        private readonly Lazy<string> _sqmId = new Lazy<string>(() => TelemetryMethods.GetSqmMachineId());

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
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Globalization", "CA1307:Specify StringComparison"
            , Justification = "string.Replace(string, string, StringComparison) is not supported in net standard 2.0")]
        // https://docs.microsoft.com/en-us/dotnet/api/system.string.replace?view=net-5.0#System_String_Replace_System_String_System_String_System_Boolean_System_Globalization_CultureInfo_
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
                    infoParts = format
                        .Replace("{runtime}", runtime)
                        .Replace("{operatingSystem}", operatingSystem + productType)
                        .Replace("{architecture}", processorArchitecture)
                        .Replace("{deviceId}", deviceId);
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
    }

    internal enum UserAgentFormats
    {
        Default,
        Http,
    };
}
