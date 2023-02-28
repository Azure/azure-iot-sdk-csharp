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
        const long MaxExecutionTime = 5L;
        private static DirectMethodServiceRequest s_directMethodRequest = new("update");
        private static ClientTwin s_updateTwin = new ClientTwin("TestTwin");
        private static DateTimeOffset s_startOn = new DateTimeOffset(new DateTime());
        private static TimeSpan s_MaxExecutionTime = new();

        [TestMethod]
        public void JobRequest_FieldInitialization()
        {
            // arrange
            var request = new JobRequest();

            // act
            long? maxExecutionTimeInSeconds = request.MaxExecutionTimeInSeconds;

            // assert
            maxExecutionTimeInSeconds.Should().BeNull();

            // rearrange
            request.MaxExecutionTimeInSeconds = MaxExecutionTime;

            // assert
            request.MaxExecutionTimeInSeconds.Should().Be(MaxExecutionTime);
        }

        [TestMethod]
        public void JobRequest_SerializesCorrectly()
        {
            // arrange
            var request = new JobRequest()
            {
                JobId = "TestJob",
                JobType = JobType.ScheduleDeviceMethod,
                DirectMethodRequest = s_directMethodRequest,
                UpdateTwin = s_updateTwin,
                QueryCondition = "TestQuery",
                StartOn = s_startOn,
                MaxExecutionTime = s_MaxExecutionTime
            };

            // act
            var settings = new JsonSerializerSettings();
            JobRequest deserializedRequest = JsonConvert.DeserializeObject<JobRequest>(JsonConvert.SerializeObject(request, settings));

            // assert
            deserializedRequest.Should().NotBeNull();
            deserializedRequest.JobId.Should().Be("TestJob");
            deserializedRequest.JobType.Should().Be(JobType.ScheduleDeviceMethod);
            deserializedRequest.DirectMethodRequest.Should().BeEquivalentTo(s_directMethodRequest);
            deserializedRequest.UpdateTwin.Should().BeEquivalentTo(s_updateTwin);
            deserializedRequest.QueryCondition.Should().Be("TestQuery");
            deserializedRequest.StartOn.Should().Be(s_startOn);
            deserializedRequest.MaxExecutionTime.Should().Be(s_MaxExecutionTime);
            deserializedRequest.MaxExecutionTimeInSeconds.Should().Be(s_MaxExecutionTime.Seconds);
        }
    }
}
