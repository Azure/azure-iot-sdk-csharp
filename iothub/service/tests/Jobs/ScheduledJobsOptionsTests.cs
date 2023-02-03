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
    public class ScheduledJobsOptionsTests
    {
        [TestMethod]
        public void ScheduledJobsOptions_FieldInitialization()
        {
            // arrange
            var options = new ScheduledJobsOptions();

            // act
            options.MaxExecutionTime = TimeSpan.FromSeconds(1);

            // assert
            options.MaxExecutionTimeInSeconds.Should().Be(1);
        }
    }
}
