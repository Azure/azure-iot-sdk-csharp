// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProductInfoTests
    {
        string ExpectedUserAgentString()
        {
            var version = typeof(DeviceClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            string runtime = RuntimeInformation.FrameworkDescription.Trim();
            string operatingSystem = RuntimeInformation.OSDescription.Trim();
            string processorArchitecture = RuntimeInformation.ProcessArchitecture.ToString().Trim();

            return $"Microsoft.Azure.Devices.Client/{version} ({runtime}; {operatingSystem}; {processorArchitecture})";
        }

        [TestMethod]
        public void Extra_DefaultValueIsEmpty()
        {
            Assert.AreEqual(String.Empty, (new ProductInfo()).Extra);
        }

        [TestMethod]
        public void ToString_ReturnsProductNameAndVersion()
        {
            Assert.AreEqual(ExpectedUserAgentString(), (new ProductInfo()).ToString());
        }

        [TestMethod]
        public void ToString_AppendsValueOfExtra()
        {
            const string extra = "abc 123 (xyz; 456)";

            var info = new ProductInfo();
            info.Extra = extra;

            Assert.AreEqual($"{ExpectedUserAgentString()} {extra}", info.ToString());
        }

        [TestMethod]
        public void ToString_DoesNotAppendWhenExtraIsNull()
        {
            var info = new ProductInfo();
            info.Extra = null;

            Assert.AreEqual(ExpectedUserAgentString(), info.ToString());
        }

        [TestMethod]
        public void ToString_DoesNotAppendWhenExtraContainsOnlyWhitespace()
        {
            var info = new ProductInfo();
            info.Extra = "\t  ";

            Assert.AreEqual(ExpectedUserAgentString(), info.ToString());
        }

        [TestMethod]
        public void ToString_AppendsTrimmedValueOfExtra()
        {
            const string extra = "\tabc123  ";

            var info = new ProductInfo();
            info.Extra = extra;

            Assert.AreEqual($"{ExpectedUserAgentString()} {extra.Trim()}", info.ToString());
        }
    }
}
