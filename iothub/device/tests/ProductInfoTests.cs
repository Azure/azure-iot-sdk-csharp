// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProductInfoTests
    {
        [TestMethod]
        public void ToString_IsValidHttpHeaderFormat()
        {
            var httpRequestMessage = new HttpRequestMessage();
            Assert.IsTrue(httpRequestMessage.Headers.UserAgent.TryParseAdd((new ProductInfo()).ToString()));
            Assert.IsTrue(httpRequestMessage.Headers.UserAgent.TryParseAdd((new ProductInfo()).ToString(UserAgentFormats.Http)));
        }

        [TestMethod]
        public void Extra_DefaultValueIsEmpty()
        {
            Assert.AreEqual(string.Empty, new ProductInfo().Extra);
        }

#if !NETCOREAPP1_1

        [TestMethod]
        public void ToString_ReturnsProductNameAndVersion()
        {
            Assert.AreEqual(ExpectedUserAgentString(), new ProductInfo().ToString());
        }

        [TestMethod]
        public void ToString_AppendsValueOfExtra()
        {
            const string extra = "abc 123 (xyz; 456)";

            var info = new ProductInfo
            {
                Extra = extra,
            };

            Assert.AreEqual($"{ExpectedUserAgentString()} {extra}", info.ToString());
            Assert.AreEqual($"{ExpectedHttpUserAgentString()} {extra}", info.ToString(UserAgentFormats.Http));
        }

        [TestMethod]
        public void ToString_DoesNotAppendWhenExtraIsNull()
        {
            var info = new ProductInfo();
            info.Extra = null;

            Assert.AreEqual(ExpectedUserAgentString(), info.ToString());
            Assert.AreEqual(ExpectedHttpUserAgentString(), info.ToString(UserAgentFormats.Http));
        }

        [TestMethod]
        public void ToString_DoesNotAppendWhenExtraContainsOnlyWhitespace()
        {
            var info = new ProductInfo
            {
                Extra = "\t  ",
            };

            Assert.AreEqual(ExpectedUserAgentString(), info.ToString());
            Assert.AreEqual(ExpectedHttpUserAgentString(), info.ToString(UserAgentFormats.Http));
        }

        [TestMethod]
        public void ToString_AppendsTrimmedValueOfExtra()
        {
            const string extra = "\tabc123  ";

            var info = new ProductInfo
            {
                Extra = extra,
            };

            Assert.AreEqual($"{ExpectedUserAgentString()} {extra.Trim()}", info.ToString());
            Assert.AreEqual($"{ExpectedHttpUserAgentString()} {extra.Trim()}", info.ToString(UserAgentFormats.Http));
        }

        [TestMethod]
        public void ToString_ProtocolFormats()
        {
            var info = new ProductInfo();

            Assert.AreEqual(ExpectedUserAgentString(), info.ToString(UserAgentFormats.Default));

            // HTTP user agent string should not include SQM ID
            Assert.AreEqual(ExpectedHttpUserAgentString(), info.ToString(UserAgentFormats.Http));
        }

        private string ExpectedUserAgentString()
        {
            string version = typeof(IotHubDeviceClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            string runtime = RuntimeInformation.FrameworkDescription.Trim();
            string operatingSystem = RuntimeInformation.OSDescription.Trim();
            string processorArchitecture = RuntimeInformation.ProcessArchitecture.ToString().Trim();

            int productType = ProductInfo.GetWindowsProductType();
            string productTypeString = (productType != 0) ? $" WindowsProduct:0x{productType:X8}" : string.Empty;
            string deviceId = ProductInfo.GetSqmMachineId() ?? string.Empty;

            string[] agentInfoParts =
            {
                runtime,
                operatingSystem + productTypeString,
                processorArchitecture,
                deviceId,
            };

            return $".NET/{version} ({string.Join("; ", agentInfoParts.Where(x => !string.IsNullOrEmpty(x)))})";
        }

        private string ExpectedHttpUserAgentString()
        {
            string version = typeof(IotHubDeviceClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            string runtime = RuntimeInformation.FrameworkDescription.Trim();
            string operatingSystem = RuntimeInformation.OSDescription.Trim();
            string processorArchitecture = RuntimeInformation.ProcessArchitecture.ToString().Trim();

            int productType = ProductInfo.GetWindowsProductType();
            string productTypeString = (productType != 0) ? $" WindowsProduct:0x{productType:X8}" : string.Empty;

            return $".NET/{version} ({runtime}; {operatingSystem + productTypeString}; {processorArchitecture})";
        }

#endif

        [TestMethod]
        public void GetSqmMachineId_ReturnsExpectedValue()
        {
            string actualValue = ProductInfo.GetSqmMachineId();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string expectedValue = null;

                // Get SQM ID from Registry if exists
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\SQMClient");
                if (key != null)
                {
                    expectedValue = key.GetValue("MachineId") as string;
                }

                Assert.AreEqual(expectedValue, actualValue);
            }
            else
            {
                // GetSqmMachineId() should always return null for all other platforms
                Assert.IsNull(actualValue);
            }
        }
    }
}
