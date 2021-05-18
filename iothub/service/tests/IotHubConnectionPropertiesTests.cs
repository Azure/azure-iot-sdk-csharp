﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubConnectionPropertiesTests
    {
        [TestMethod]
        [DataRow("acme.azure-devices.net", "acme")]
        [DataRow("Acme-1.azure-devices.net", "Acme-1")]
        [DataRow("acme2.azure-devices.net", "acme2")]
        [DataRow("3acme.azure-devices.net", "3acme")]
        [DataRow("4-acme.azure-devices.net", "4-acme")]
        public void IotHubConnectionPropertiesGetHubNameTest(string hostName, string expectedHubName)
        {
            // act
            string hubName = IotHubConnectionProperties.GetIotHubName(hostName);

            // assert
            hubName.Should().Be(expectedHubName);
        }
    }
}
