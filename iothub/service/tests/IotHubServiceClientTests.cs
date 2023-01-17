// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Azure;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubServiceClientTests
    {
        [TestMethod]
        public void IotHubServiceClient_SubClients_NotNull()
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
        public void IotHubServiceClient_CreateSubClientsWithAad()
        {
            // arrange
            string hostName = "acme.azure-devices.net";
            var tokenCrediential = new TestTokenCredential(new DateTimeOffset(DateTime.Now + TimeSpan.FromHours(1)));

            // act
            using var serviceClient = new IotHubServiceClient(hostName, tokenCrediential);

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
        public void IotHubServiceClient_CreateSubClientsWithSasToken()
        {
            // arrange
            string hostName = "acme.azure-devices.net";
            var sasCredential = new AzureSasCredential("test");

            // act
            using var serviceClient = new IotHubServiceClient(hostName, sasCredential);

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
        public void IotHubServiceClient_NullParameters_Throws()
        {
            // arrange
            string hostName = null;
            var sasCredential = new AzureSasCredential("test");

            // act
            Action act = () => _ = new IotHubServiceClient(hostName, sasCredential);
            
            // assert
            act.Should().Throw<ArgumentNullException>();

            // rearrange
            string connectionString = null;

            // act
            act = () => _ = new IotHubServiceClient(connectionString);

            // assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
