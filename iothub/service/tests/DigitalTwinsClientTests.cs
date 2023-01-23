// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class DigitalTwinsClientTests
    {
        [TestMethod]
        public async Task DigitalTwinsClient_GetAsync()
        {
            // arrange
            var digitalTwinsClient = new Mock<DigitalTwinsClient>();
            string digitalTwinId = Guid.NewGuid().ToString();

            // act
            Func<Task> act = async() => await digitalTwinsClient.Object.GetAsync<string>(digitalTwinId);

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_GetAsync_HttpException()
        {
            // arrange
            string cs = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            string digitalTwinId = Guid.NewGuid().ToString();
            using var serviceClient = new IotHubServiceClient(cs);
            DigitalTwinsClient digialTwinsClient = serviceClient.DigitalTwins;

            // act
            // deliberately throw http exception by searching for twin that does not exist
            Func<Task> act = async() => await digialTwinsClient.GetAsync<string>(digitalTwinId);

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_UpdateAsync()
        {
            // arrange
            var digitalTwinsClient = new Mock<DigitalTwinsClient>();
            string digitalTwinId = Guid.NewGuid().ToString();
            string jsonPatch = "";

            // act
            Func<Task> act = async () => await digitalTwinsClient.Object.UpdateAsync(digitalTwinId, jsonPatch);

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_UpdateAsync_HttpException()
        {
            // arrange
            string cs = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            string digitalTwinId = Guid.NewGuid().ToString();
            string jsonPatch = "test";
            using var serviceClient = new IotHubServiceClient(cs);
            DigitalTwinsClient digialTwinsClient = serviceClient.DigitalTwins;

            // act
            // deliberately throw http exception by searching for twin that does not exist
            Func<Task> act = async () => await digialTwinsClient.UpdateAsync(digitalTwinId, jsonPatch);

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_InvokeCommandAsync()
        {
            // arrange
            var digitalTwinsClient = new Mock<DigitalTwinsClient>();
            string digitalTwinId = Guid.NewGuid().ToString();
            string commandName = "test";

            // act
            Func<Task> act = async () => await digitalTwinsClient.Object.InvokeCommandAsync(digitalTwinId, commandName);

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_InvokeCommandAysnc_HttpException()
        {
            // arrange
            string cs = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            string digitalTwinId = Guid.NewGuid().ToString();
            string commandName = "test";
            using var serviceClient = new IotHubServiceClient(cs);
            DigitalTwinsClient digialTwinsClient = serviceClient.DigitalTwins;

            // act
            // deliberately throw http exception by searching for twin that does not exist
            Func<Task> act = async () => await digialTwinsClient.InvokeCommandAsync(digitalTwinId, commandName);

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_InvokeComponentCommandAsync()
        {
            // arrange
            var digitalTwinsClient = new Mock<DigitalTwinsClient>();
            string digitalTwinId = Guid.NewGuid().ToString();
            string commandName = "test";
            string componentName = "test";

            // act
            Func<Task> act = async () => await digitalTwinsClient.Object.InvokeComponentCommandAsync(digitalTwinId, componentName, commandName);

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task DigitalTwinsClient_InvokeComponentCommandAsync_HttpException()
        {
            // arrange
            string cs = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            string digitalTwinId = Guid.NewGuid().ToString();
            string commandName = "test";
            string componentName = "test";
            using var serviceClient = new IotHubServiceClient(cs);
            DigitalTwinsClient digialTwinsClient = serviceClient.DigitalTwins;

            // act
            // deliberately throw http exception by searching for twin that does not exist
            Func<Task> act = async () => await digialTwinsClient.InvokeComponentCommandAsync(digitalTwinId, componentName, commandName);

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }
    }
}
