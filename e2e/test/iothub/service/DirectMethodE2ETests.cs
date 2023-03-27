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
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DirectMethodsClient_InvokeAsycn_ModuleDoesNotExist()
        {
            // arrange
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;

            var methodInvocation = new DirectMethodServiceRequest("someDirectMethod");

            // act
            Func<Task> act = async () => await serviceClient.DirectMethods.InvokeAsync("someNonexistentDevice", "someNonexistentModule", methodInvocation);

            // assert
            ExceptionAssertions<IotHubServiceException> errorContext = await act.Should().ThrowAsync<IotHubServiceException>();
            errorContext.And.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            errorContext.And.IsTransient.Should().BeFalse();
        }
    }
}
