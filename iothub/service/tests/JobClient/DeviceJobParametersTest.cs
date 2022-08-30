// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Api.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class DeviceJobParametersTest
    {
        [TestMethod]
        public void ConstructorTest()
        {
            // should not throw
            _ = new DeviceJobParameters(JobType.ScheduleDeviceMethod, "deviceId");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorWithNullDeviceIdTest()
        {
            _ = new DeviceJobParameters(JobType.ScheduleDeviceMethod, deviceId: null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorWithNullDeviceIdsTest()
        {
            _ = new DeviceJobParameters(JobType.ScheduleDeviceMethod, deviceIds: null);
        }
    }
}
