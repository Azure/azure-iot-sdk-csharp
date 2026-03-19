// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Tests.Jobs
{
    [TestClass]
    [TestCategory("Unit")]
    public class ScheduledJobsOptionsTests
    {
        const int MaxExecutionTime = 1;

        [TestMethod]
        public void ScheduledJobsOptions_FieldInitialization()
        {
            // arrange
            var options = new ScheduledJobsOptions();

            // act
            options.MaxExecutionTime = TimeSpan.FromSeconds(MaxExecutionTime);

            // assert
            options.MaxExecutionTimeInSeconds.Should().Be(MaxExecutionTime);
        }

        [TestMethod]
        public void ScheduledJobOptions_SerializesCorrectly()
        {
            // arrange
            var options = new ScheduledJobsOptions
            {
                JobId = "TestJob",
                MaxExecutionTime = TimeSpan.FromSeconds(MaxExecutionTime),
            };

            // act
            var settings = new JsonSerializerSettings();
            ScheduledJobsOptions deserializedRequest = JsonConvert.DeserializeObject<ScheduledJobsOptions>(JsonConvert.SerializeObject(options, settings));

            // assert
            deserializedRequest.Should().NotBeNull();
            deserializedRequest.JobId.Should().Be("TestJob");
            deserializedRequest.MaxExecutionTime.Should().Be(TimeSpan.FromSeconds(MaxExecutionTime));
        }
    }
}
