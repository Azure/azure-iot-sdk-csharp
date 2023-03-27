// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.E2ETests.iothub.service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class DirectMethodE2ETests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(DirectMethodE2ETests)}_";
        private readonly string _modulePrefix = $"{nameof(DirectMethodE2ETests)}_Module_";


        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DirectMethodsClient_InvokeAsync_MethodDoesNotExistAtFirst()
        {
            // arrange
            using IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var cts = new CancellationToken();

            var methodInvocation = new DirectMethodServiceRequest("SetTelemetryInterval")
            {
                Payload = "10",
                ResponseTimeout = TimeSpan.FromSeconds(30),
            };

            // act
            Func<Task> act1 = async () => await serviceClient.DirectMethods.InvokeAsync(testDevice.Id, methodInvocation, cts);

            // assert
            var errorContext = await act1.Should().ThrowAsync<IotHubServiceException>();
            errorContext.And.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            errorContext.And.IsTransient.Should().BeTrue();

            // rearrange, open device client and set direct method callback
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient();
            await deviceClient.OpenAsync();
            DirectMethodRequest actualRequest = null;
            await deviceClient.SetDirectMethodCallbackAsync((request) =>
            {
                actualRequest = request;
                return Task.FromResult(new DirectMethodResponse(200));
            }).ConfigureAwait(false);

            // act
            Func<Task> act2 = async () => await serviceClient.DirectMethods.InvokeAsync(testDevice.Id, methodInvocation, cts);

            // assert
            var response = await act2.Should().NotThrowAsync();
            actualRequest.Should().NotBeNull();

            await deviceClient.CloseAsync();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DirectMethodsClient_InvokeAsycn_MethodDoesNotExist_ModuleId()
        {
            // arrange
            using IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            await using TestModule testModule = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix);
            var cts = new CancellationToken();

            var methodInvocation = new DirectMethodServiceRequest("SetTelemetryInterval")
            {
                Payload = "10",
                ResponseTimeout = TimeSpan.FromSeconds(30),
            };

            // act
            Func<Task> act = async () => await serviceClient.DirectMethods.InvokeAsync(testModule.Id, methodInvocation, cts);

            // assert
            var errorContext = await act.Should().ThrowAsync<IotHubServiceException>();
            errorContext.And.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            errorContext.And.IsTransient.Should().BeFalse();
        }
    }
}
