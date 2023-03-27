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
using FluentAssertions.Specialized;
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
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            var cts = new CancellationToken();

            var methodInvocation = new DirectMethodServiceRequest("someDirectMethod");

            // act
            Func<Task> act = async () => await serviceClient.DirectMethods.InvokeAsync("some nonexistent device", methodInvocation, cts);

            // assert
            ExceptionAssertions<IotHubServiceException> errorContext = await act.Should().ThrowAsync<IotHubServiceException>();
            errorContext.And.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            errorContext.And.IsTransient.Should().BeFalse();

            // rearrange, open device client and set direct method callback
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient();
            await deviceClient.OpenAsync();
            DirectMethodRequest actualRequest = null;
            await deviceClient.SetDirectMethodCallbackAsync((request) =>
            {
                actualRequest = request;
                return Task.FromResult(new DirectMethodResponse(200));
            }).ConfigureAwait(false);

            // act
            DirectMethodClientResponse response = await serviceClient.DirectMethods.InvokeAsync(testDevice.Id, methodInvocation, cts);

            // assert
            response.Status.Should().Be(200);
            actualRequest.Should().NotBeNull();

            await deviceClient.CloseAsync();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DirectMethodsClient_InvokeAsycn_MethodDoesNotExist_ModuleId()
        {
            // arrange
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            await using TestModule testModule = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix);
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix);
            var cts = new CancellationToken();

            var methodInvocation = new DirectMethodServiceRequest("someDirectMethod");

            // act
            Func<Task> act = async () => await serviceClient.DirectMethods.InvokeAsync(testDevice.Id, testModule.Id, methodInvocation, cts);

            // assert
            ExceptionAssertions<IotHubServiceException> errorContext = await act.Should().ThrowAsync<IotHubServiceException>();
            errorContext.And.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            errorContext.And.IsTransient.Should().BeFalse();
        }
    }
}
