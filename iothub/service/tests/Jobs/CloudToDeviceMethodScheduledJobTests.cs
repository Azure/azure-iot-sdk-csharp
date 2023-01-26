// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests.Jobs
{
    [TestClass]
    [TestCategory("Unit")]
    public class CloudToDeviceMethodScheduledJobTests
    {
        [TestMethod]
        public void CloudToDeviceMethodScheduledJob_Ctor_Ok() {
            // arrange - act
            var request = new DirectMethodServiceRequest("TestMethod");
            var job = new CloudToDeviceMethodScheduledJob(request);

            // assert
            job.DirectMethodRequest.Should().BeEquivalentTo(request);
        }
    }
}
