// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class SdkUtilsGaVersionTests
    {
        [TestMethod]
        public void ApiVersionQueryString_WhenAccessed_ReturnsGaVersion()
        {
            // Arrange
            const string expected = "api-version=2026-11-01";

            // Act
            string actual = SdkUtils.ApiVersionQueryString;

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
