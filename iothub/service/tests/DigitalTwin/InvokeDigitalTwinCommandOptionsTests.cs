// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Tests.DigitalTwin
{
    [TestClass]
    [TestCategory("Unit")]
    public class InvokeDigitalTwinCommandOptionsTests
    {
        [TestMethod]
        public void InvokeDigitalTwinCommandOptions_Ctor_Ok()
        {
            // arrange - act
            int samplePayload = 1;
            var connectTime = TimeSpan.FromSeconds(1);
            var requestTimeout = TimeSpan.FromSeconds(1);
            var options = new InvokeDigitalTwinCommandOptions
            {
                Payload = JsonConvert.SerializeObject(samplePayload),
                ConnectTimeout = connectTime,
                ResponseTimeout = requestTimeout
            };
            
            // assert
            JsonConvert.DeserializeObject(options.Payload).Should().Be(samplePayload);
            options.ConnectTimeout.Should().Be(connectTime);
            options.ResponseTimeout.Should().Be(requestTimeout);
        }

        [TestMethod]
        public void InvokeDigitalTwinCommandOptions_Ctor_Default_ok()
        {
            // arrange - act
            var options = new InvokeDigitalTwinCommandOptions();

            // assert
            options.Payload.Should().Be(null);
            options.ConnectTimeout.Should().Be(null);
            options.ResponseTimeout.Should().Be(null);
        }
    }
}
