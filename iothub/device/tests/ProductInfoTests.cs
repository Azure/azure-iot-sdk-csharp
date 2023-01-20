// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using FluentAssertions;
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
            using var httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.Headers.UserAgent.TryParseAdd(new ProductInfo().ToString()).Should().BeTrue();
            httpRequestMessage.Headers.UserAgent.TryParseAdd(new ProductInfo().ToString(UserAgentFormats.Http)).Should().BeTrue();
        }

        [TestMethod]
        public void Extra_DefaultValueIsEmpty()
        {
            new ProductInfo().Extra.Should().Be(string.Empty);
        }

#if !NETCOREAPP1_1

        [TestMethod]
        public void ToString_ReturnsProductNameAndVersion()
        {
            new ProductInfo().ToString().Should().Be(ExpectedUserAgentString());
        }

        [TestMethod]
        public void ToString_AppendsValueOfExtra()
        {
            // arrange  
            const string extra = "abc 123 (xyz; 456)";

            // act
            var info = new ProductInfo
            {
                Extra = extra,
            };

            // assert
            info.ToString().Should().Be($"{ExpectedUserAgentString()} {extra}");
            info.ToString(UserAgentFormats.Http).Should().Be($"{ExpectedHttpUserAgentString()} {extra}");
        }

        [TestMethod]
        public void ToString_DoesNotAppendWhenExtraIsNull()
        {
            // act
            var info = new ProductInfo
            {
                Extra = null
            };

            // assert
            info.ToString().Should().Be(ExpectedUserAgentString());
            info.ToString(UserAgentFormats.Http).Should().Be(ExpectedHttpUserAgentString());
        }

        [TestMethod]
        public void ToString_DoesNotAppendWhenExtraContainsOnlyWhitespace()
        {
            // act
            var info = new ProductInfo
            {
                Extra = "\t  ",
            };

            // assert
            info.ToString().Should().Be(ExpectedUserAgentString());
            info.ToString(UserAgentFormats.Http).Should().Be(ExpectedHttpUserAgentString());
        }

        [TestMethod]
        public void ToString_AppendsTrimmedValueOfExtra()
        {
            // arrange
            const string extra = "\tabc123  ";

            // act
            var info = new ProductInfo
            {
                Extra = extra,
            };

            // assert
            info.ToString().Should().Be($"{ExpectedUserAgentString()} {extra.Trim()}");
            info.ToString(UserAgentFormats.Http).Should().Be($"{ExpectedHttpUserAgentString()} {extra.Trim()}");
        }

        [TestMethod]
        public void ToString_ProtocolFormats()
        {
            var info = new ProductInfo();
            info.ToString(UserAgentFormats.Default).Should().Be(ExpectedUserAgentString());
            // HTTP user agent string should not include SQM ID
            info.ToString(UserAgentFormats.Http).Should().Be(ExpectedHttpUserAgentString());
        }

        private static string ExpectedUserAgentString()
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

        private static string ExpectedHttpUserAgentString()
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
                expectedValue.Should().Be(actualValue);
            }
            else
            {
                // GetSqmMachineId() should always return null for all other platforms
                actualValue.Should().BeNull();
            }
        }
    }
}
