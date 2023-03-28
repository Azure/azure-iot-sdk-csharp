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
using System.Reflection;
using Microsoft.Identity.Client;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Service")]
    public class DirectMethodE2ETests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(DirectMethodE2ETests)}_";
        private readonly string _modulePrefix = $"{nameof(DirectMethodE2ETests)}_Module_";

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DirectMethodsClient_InvokeAsync_DeviceDoesNotExist()
        {
            // arrange
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;

            var methodInvocation = new DirectMethodServiceRequest("someDirectMethod");

            // act
            Func<Task> act = async () => await serviceClient.DirectMethods.InvokeAsync("someNonexistentDevice", methodInvocation);

            // assert

            ExceptionAssertions<IotHubServiceException> errorContext = await act.Should().ThrowAsync<IotHubServiceException>();
            errorContext.And.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            errorContext.And.IsTransient.Should().BeFalse();
            errorContext.And.ErrorCode.Should().Be(IotHubServiceErrorCode.DeviceNotFound);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DirectMethodsClient_InvokeAsycn_ModuleDoesNotExist()
        {
            // arrange
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;

            // ensure device exists but module does not
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix);
            var methodInvocation = new DirectMethodServiceRequest("someDirectMethod");

            // act
            Func<Task> act = async () => await serviceClient.DirectMethods.InvokeAsync(testDevice.Id, "someNonexistentModule", methodInvocation);

            // assert
            ExceptionAssertions<IotHubServiceException> errorContext = await act.Should().ThrowAsync<IotHubServiceException>();
            errorContext.And.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            errorContext.And.IsTransient.Should().BeFalse();
            errorContext.And.ErrorCode.Should().Be(IotHubServiceErrorCode.ModuleNotFound);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DirectMethodsClient_DeviceClientOpen_DeviceNotSubscribedToDirectMethods()
        {
            // arrange
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix);
            var methodInvocation = new DirectMethodServiceRequest("someDirectMethod");

            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient();
            await testDevice.OpenWithRetryAsync();

            // act
            Func<Task> act = async () => await serviceClient.DirectMethods.InvokeAsync(testDevice.Id, methodInvocation);

            // assert
            ExceptionAssertions<IotHubServiceException> errorContext = await act.Should().ThrowAsync<IotHubServiceException>();
            errorContext.And.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            errorContext.And.ErrorCode.Should().Be(IotHubServiceErrorCode.DeviceNotOnline);
            testDevice.Device.ConnectionState.Should().Be(ClientConnectionState.Disconnected);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DirectMethodsClient_DeviceOnline_ExceedsRequestTimeout()
        {
            // arrange
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix);
            const int timeoutInSeconds = 3;

            var methodInvocation = new DirectMethodServiceRequest("someDirectMethod")
            {
                ConnectionTimeout = TimeSpan.FromSeconds(timeoutInSeconds),
                ResponseTimeout = TimeSpan.FromSeconds(timeoutInSeconds),
            };
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient();
            await testDevice.OpenWithRetryAsync().ConfigureAwait(false);

            // act
            await deviceClient
            .SetDirectMethodCallbackAsync(
                async (methodRequest) =>
                {
                    // force a timeout
                    await Task.Delay(timeoutInSeconds * 1000 * 2).ConfigureAwait(false);
                    var response = new DirectMethodResponse(200);
                    return response;
                })
            .ConfigureAwait(false);

            // assert
            Func<Task> act = async() => await serviceClient.DirectMethods.InvokeAsync(testDevice.Id, methodInvocation);
            ExceptionAssertions<IotHubServiceException> response = await act.Should().ThrowAsync<IotHubServiceException>();
            response.And.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            response.And.ErrorCode.Should().Be(IotHubServiceErrorCode.ArgumentInvalid);
        }
    }
}
