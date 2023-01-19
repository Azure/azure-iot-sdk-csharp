// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProductInfoTests
    {
        [TestMethod]
        public void ProductInfo_WithExtra()
        {
            // arrange
            string extraString = "extra-user-agent-string";

            var productInfo = new ProductInfo
            {
                Extra = "   " + extraString + "   "
            };

            string name = "Microsoft.Azure.Devices.Provisioning.Client";
            string version = typeof(ProvisioningDeviceClient).GetTypeInfo().Assembly.GetName().Version.ToString(3);
            string runtime = RuntimeInformation.FrameworkDescription.Trim();
            string operatingSystem = RuntimeInformation.OSDescription.Trim();
            string processorArchitecture = RuntimeInformation.ProcessArchitecture.ToString().Trim();

            string userAgent = $"{name}/{version} ({runtime}; {operatingSystem}; {processorArchitecture})";

            // act
            string productInfoString = productInfo.ToString();

            // assert
            productInfoString.Should().BeEquivalentTo(userAgent + " " + extraString);
        }
    }
}
