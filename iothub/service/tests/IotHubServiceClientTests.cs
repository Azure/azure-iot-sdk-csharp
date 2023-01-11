// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubServiceClientTests
    {
        [TestMethod]
        public void IotHubServiceClient_CreateWithConnectionString()
        {
            // arrange and act
            string cs = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var serviceClient = new IotHubServiceClient(cs);

            // assert
            serviceClient.Devices.Should().NotBeNull();
            serviceClient.Modules.Should().NotBeNull();
            serviceClient.Configurations.Should().NotBeNull();
            serviceClient.DirectMethods.Should().NotBeNull();
            serviceClient.Query.Should().NotBeNull();
            serviceClient.ScheduledJobs.Should().NotBeNull();
            serviceClient.DigitalTwins.Should().NotBeNull();
            serviceClient.Twins.Should().NotBeNull();
            serviceClient.MessageFeedback.Should().NotBeNull();
            serviceClient.FileUploadNotifications.Should().NotBeNull();
            serviceClient.Messages.Should().NotBeNull();
        }

        [TestMethod]
        public void IotHubServiceClient_CreateWithAAD()
        {
            string hostName = "acme.azure-devices.net";
        }

        [TestMethod]
        public void IotHubServiceClient_CreateWithSasToken()
        {

        }

        [TestMethod]
        public void IotHubServiceClient_InitializeSubclients()
        {

        }

        [TestMethod]
        public void IotHubServiceClient_Dispose()
        {

        }
    }
}
