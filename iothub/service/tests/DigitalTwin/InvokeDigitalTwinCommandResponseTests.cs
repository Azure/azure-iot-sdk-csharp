// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests.DigitalTwin
{
    [TestClass]
    [TestCategory("Unit")]
    public class InvokeDigitalTwinCommandResponseTests
    {
        [TestMethod]
        public void InvokeDigitalTwinCommandResponse_Ctor_Ok()
        {
            // arrange - act
            const int Status = 0;
            const string Payload = "Hello World";
            const string RequestId = "1234";
            var invokeDigitalTwinCommandResponse = new InvokeDigitalTwinCommandResponse
            {
                Status= Status,
                Payload= Payload,
                RequestId = RequestId
            };

            // assert
            invokeDigitalTwinCommandResponse.Status.Should().Be(0);
            invokeDigitalTwinCommandResponse.Payload.Should().Be(Payload);
            invokeDigitalTwinCommandResponse.RequestId.Should().Be(RequestId);
        }
    }
}
