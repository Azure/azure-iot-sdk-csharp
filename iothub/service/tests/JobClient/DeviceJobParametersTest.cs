﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Api.Test
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Unit")]
    public class DeviceJobParametersTest
    {
        [TestMethod]
        public void ConstructorTest()
        {
            new DeviceJobParameters(JobType.ScheduleDeviceMethod, "deviceId");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructorWithNullDeviceIdTest()
        {
            new DeviceJobParameters(JobType.ScheduleDeviceMethod, (string)null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructorWithEmptyDeviceIdTest()
        {
            new DeviceJobParameters(JobType.ScheduleDeviceMethod, "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructorWithSomeEmptyDeviceIdsTest()
        {
            var deviceList = new List<string>() { "a", null, "b" };
            new DeviceJobParameters(JobType.ScheduleDeviceMethod, deviceList);
        }
    }
}
