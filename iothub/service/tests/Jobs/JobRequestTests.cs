// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Tests.Jobs
{
    [TestClass]
    [TestCategory("Unit")]
    public class JobRequestTests
    {
        [TestMethod]
        public void JobRequest_FieldInitialization()
        {
            // arrange
            var request = new JobRequest();

            // act
            long? maxExecutionTimeInSeconds = request.MaxExecutionTimeInSeconds;

            // assert
            maxExecutionTimeInSeconds.Should().Be(null);

            // rearrange
            request.MaxExecutionTimeInSeconds = 5L;

            // assert
            request.MaxExecutionTimeInSeconds.Should().Be(5L);
        }

        [TestMethod]
        public void JobRequest_SerializesCorrectly()
        {
            // arrange
            var directMethodRequest = new DirectMethodServiceRequest("update");
            var updateTwin = new ClientTwin("TestTwin");
            var startOn = new DateTimeOffset(new DateTime());
            var maxExecutionTime = new TimeSpan();

            var request = new JobRequest()
            {
                JobId = "TestJob",
                JobType = JobType.ScheduleDeviceMethod,
                DirectMethodRequest = directMethodRequest,
                UpdateTwin = updateTwin,
                QueryCondition = "TestQuery",
                StartOn = startOn,
                MaxExecutionTime = maxExecutionTime
            };

            // act
            var settings = new JsonSerializerSettings();
            JobRequest deserializedRequest = JsonConvert.DeserializeObject<JobRequest>(JsonConvert.SerializeObject(request, settings));

            // assert
            deserializedRequest.Should().NotBeNull();
            deserializedRequest.JobId.Should().Be("TestJob");
            deserializedRequest.JobType.Should().Be(JobType.ScheduleDeviceMethod);
            deserializedRequest.DirectMethodRequest.Should().BeEquivalentTo(directMethodRequest);
            deserializedRequest.UpdateTwin.Should().BeEquivalentTo(updateTwin);
            deserializedRequest.QueryCondition.Should().Be("TestQuery");
            deserializedRequest.StartOn.Should().Be(startOn);
            deserializedRequest.MaxExecutionTime.Should().Be(maxExecutionTime);
            deserializedRequest.MaxExecutionTimeInSeconds.Should().Be(maxExecutionTime.Seconds);
        }
    }
}
