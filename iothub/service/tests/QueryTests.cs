// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Api.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using Microsoft.Azure.Devices.Shared;
    using FluentAssertions;

    [TestClass]
    [TestCategory("Unit")]
    public class QueryTests
    {
        [TestMethod]
        public void QueryResultCastContentToTwinNoContinuationTest()
        {
            // simulate json serialize/deserialize
            var serverQueryResult = new QueryResult()
            {
                Type = QueryResultType.Twin,
                Items = new List<Twin>()
                {
                    new Twin()
                    {
                        DeviceId = "test",
                    }
                }
            };
            // serialize
            var jsonQueryResult = JsonConvert.SerializeObject(serverQueryResult);
            Assert.AreEqual("{\"type\":\"twin\",\"items\":[{\"deviceId\":\"test\",\"etag\":null,\"version\":null,\"properties\":{\"desired\":{},\"reported\":{}}}],\"continuationToken\":null}", jsonQueryResult);

            // deserialize
            var clientQueryResult = JsonConvert.DeserializeObject<QueryResult>(jsonQueryResult);

            // test
            IQuery q = new Query((t) => Task.FromResult<QueryResult>(clientQueryResult));

            // validate
            Assert.IsTrue(q.HasMoreResults);

            IEnumerable<Twin> r = q.GetNextAsTwinAsync().Result;
            Assert.AreEqual(1, r.Count());
            Assert.IsInstanceOfType(r.ElementAt(0), typeof(Twin));
            Assert.AreEqual("test", r.ElementAt(0).DeviceId);
            Assert.IsFalse(q.HasMoreResults);
        }

        [TestMethod]
        public void QueryResultCastContentToTwinContinuationTest()
        {
            // simulate json serialize/deserialize
            var serverQueryResult = new QueryResult()
            {
                Type = QueryResultType.Twin,
                Items = new List<Twin>()
                {
                    new Twin()
                    {
                        DeviceId = "test",
                    }
                },
                ContinuationToken = "GYUVJDBJFKJ"
            };
            // serialize
            var jsonQueryResult = JsonConvert.SerializeObject(serverQueryResult);
            Assert.AreEqual("{\"type\":\"twin\",\"items\":[{\"deviceId\":\"test\",\"etag\":null,\"version\":null,\"properties\":{\"desired\":{},\"reported\":{}}}],\"continuationToken\":\"GYUVJDBJFKJ\"}", jsonQueryResult);
            // deserialize
            var clientQueryResult = JsonConvert.DeserializeObject<QueryResult>(jsonQueryResult);

            // test
            IQuery q = new Query((t) => Task.FromResult<QueryResult>(clientQueryResult));

            // validate
            Assert.IsTrue(q.HasMoreResults);

            IEnumerable<Twin> r = q.GetNextAsTwinAsync().Result;
            Assert.AreEqual(1, r.Count());
            Assert.IsInstanceOfType(r.ElementAt(0), typeof(Twin));
            Assert.AreEqual("test", r.ElementAt(0).DeviceId);
            Assert.IsTrue(q.HasMoreResults);
        }

        [TestMethod]
        public void QueryResultCastContentToDeviceJobTest()
        {
            // simulate json serialize/deserialize
            IEnumerable<DeviceJob> jobs = new List<DeviceJob>()
            {
                new DeviceJob()
                {
                    DeviceId = "123456",
                    JobId = "789",
                    JobType = DeviceJobType.ScheduleUpdateTwin,
                    Status = DeviceJobStatus.Running
                }
            };

            var serverQueryResult = new QueryResult()
            {
                Type = QueryResultType.DeviceJob,
                Items = jobs
            };

            // serialize
            var jsonQueryResult = JsonConvert.SerializeObject(serverQueryResult);
            Assert.AreEqual("{\"type\":\"deviceJob\",\"items\":[{\"deviceId\":\"123456\",\"jobId\":\"789\",\"jobType\":\"scheduleUpdateTwin\",\"status\":\"running\",\"startTimeUtc\":\"0001-01-01T00:00:00\",\"endTimeUtc\":\"0001-01-01T00:00:00\",\"createdDateTimeUtc\":\"0001-01-01T00:00:00\",\"lastUpdatedDateTimeUtc\":\"0001-01-01T00:00:00\"}],\"continuationToken\":null}", jsonQueryResult);

            // deserialize
            var clientQueryResult = JsonConvert.DeserializeObject<QueryResult>(jsonQueryResult);

            // test
            IQuery q = new Query((t) => Task.FromResult<QueryResult>(clientQueryResult));

            // validate
            Assert.IsTrue(q.HasMoreResults);
            IEnumerable<DeviceJob> content = q.GetNextAsDeviceJobAsync().Result;

            Assert.AreEqual(1, content.Count());
            Assert.IsInstanceOfType(content.ElementAt(0), typeof(DeviceJob));
            Assert.AreEqual("123456", content.ElementAt(0).DeviceId);
            Assert.AreEqual("789", content.ElementAt(0).JobId);
            Assert.AreEqual(DeviceJobStatus.Running, content.ElementAt(0).Status);
        }

        [TestMethod]
        public void QueryResultCastContentToJobResponseTest()
        {
            // simulate json serialize/deserialize
            IEnumerable<JobResponse> jobs = new List<JobResponse>()
            {
                new JobResponse()
                {
                    DeviceId = "123456",
                    JobId = "789",
                    Type = JobType.ScheduleUpdateTwin,
                    Status = JobStatus.Completed,
                    StartTimeUtc = DateTime.MinValue,
                    EndTimeUtc = DateTime.MinValue
                }
            };

            var serverQueryResult = new QueryResult()
            {
                Type = QueryResultType.JobResponse,
                Items = jobs
            };

            // serialize
            var jsonQueryResult = JsonConvert.SerializeObject(serverQueryResult);
            Assert.AreEqual("{\"type\":\"jobResponse\",\"items\":[{\"jobId\":\"789\",\"startTime\":\"0001-01-01T00:00:00\",\"endTime\":\"0001-01-01T00:00:00\",\"type\":\"scheduleUpdateTwin\",\"status\":\"completed\",\"deviceId\":\"123456\"}],\"continuationToken\":null}", jsonQueryResult);

            // deserialize
            var clientQueryResult = JsonConvert.DeserializeObject<QueryResult>(jsonQueryResult);

            // test
            IQuery q = new Query((t) => Task.FromResult<QueryResult>(clientQueryResult));

            // validate
            Assert.IsTrue(q.HasMoreResults);
            IEnumerable<JobResponse> content = q.GetNextAsJobResponseAsync().Result;

            Assert.AreEqual(1, content.Count());
            Assert.IsInstanceOfType(content.ElementAt(0), typeof(JobResponse));
            Assert.AreEqual("123456", content.ElementAt(0).DeviceId);
            Assert.AreEqual("789", content.ElementAt(0).JobId);
            Assert.AreEqual(JobType.ScheduleUpdateTwin, content.ElementAt(0).Type);
            Assert.AreEqual(JobStatus.Completed, content.ElementAt(0).Status);
            Assert.AreEqual(DateTime.MinValue, content.ElementAt(0).StartTimeUtc);
            Assert.AreEqual(DateTime.MinValue, content.ElementAt(0).EndTimeUtc);
        }

        [TestMethod]
        public void QueryResultCastContentToJsonTest()
        {
            // simulate json serialize/deserialize
            IEnumerable<DeviceJob> jobs = new List<DeviceJob>()
            {
                new DeviceJob()
                {
                    DeviceId = "123456",
                    JobId = "789",
                    JobType = DeviceJobType.ScheduleDeviceMethod,
                    Status = DeviceJobStatus.Running
                }
            };

            var serverQueryResult = new QueryResult()
            {
                Type = QueryResultType.DeviceJob,
                Items = jobs
            };

            // serialize
            var jsonQueryResult = JsonConvert.SerializeObject(serverQueryResult);
            Assert.AreEqual("{\"type\":\"deviceJob\",\"items\":[{\"deviceId\":\"123456\",\"jobId\":\"789\",\"jobType\":\"scheduleDeviceMethod\",\"status\":\"running\",\"startTimeUtc\":\"0001-01-01T00:00:00\",\"endTimeUtc\":\"0001-01-01T00:00:00\",\"createdDateTimeUtc\":\"0001-01-01T00:00:00\",\"lastUpdatedDateTimeUtc\":\"0001-01-01T00:00:00\"}],\"continuationToken\":null}", jsonQueryResult);
            // deserialize
            var clientQueryResult = JsonConvert.DeserializeObject<QueryResult>(jsonQueryResult);

            // test
            IQuery q = new Query((t) => Task.FromResult<QueryResult>(clientQueryResult));

            // validate
            Assert.IsTrue(q.HasMoreResults);
            IEnumerable<DeviceJob> content = q.GetNextAsDeviceJobAsync().Result;

            Assert.AreEqual(1, content.Count());
            Assert.IsInstanceOfType(content.ElementAt(0), typeof(DeviceJob));
            Assert.AreEqual("123456", content.ElementAt(0).DeviceId);
            Assert.AreEqual("789", content.ElementAt(0).JobId);
            Assert.AreEqual(DeviceJobStatus.Running, content.ElementAt(0).Status);
        }

        [TestMethod]
        public void QueryResultCallNextOutsideWhileTest()
        {
            // simulate json serialize/deserialize
            IEnumerable<DeviceJob> jobs = new List<DeviceJob>()
            {
                new DeviceJob()
                {
                    DeviceId = "123456",
                    JobId = "789",
                    JobType = DeviceJobType.ScheduleDeviceMethod,
                    Status = DeviceJobStatus.Running
                }
            };

            var serverQueryResult = new QueryResult()
            {
                Type = QueryResultType.DeviceJob,
                Items = jobs
            };

            var jsonQueryResult = JsonConvert.SerializeObject(serverQueryResult);
            var clientQueryResult = JsonConvert.DeserializeObject<QueryResult>(jsonQueryResult);

            // test
            IQuery q = new Query((t) => Task.FromResult<QueryResult>(clientQueryResult));

            while (q.HasMoreResults)
            {
                q.GetNextAsJsonAsync().Wait();
            }

            // validate
            // after a while, q.HasMoreResult should be false and GetNext() should return an empty enumerable
            Assert.IsFalse(q.HasMoreResults);
            Assert.IsFalse(q.GetNextAsJsonAsync().Result.Any());
        }

        [TestMethod]
        public void QueryResultUserSuppliedContinuationTest()
        {
            // simulate json serialize/deserialize
            var serverQueryResult = new QueryResult()
            {
                Type = QueryResultType.Twin,
                Items = new List<Twin>()
                {
                    new Twin()
                    {
                        DeviceId = "test",
                    }
                },
                ContinuationToken = "GYUVJDBJFKJ"
            };

            var clientQueryResult = JsonConvert.DeserializeObject<QueryResult>(JsonConvert.SerializeObject(serverQueryResult));

            // test
            string requestToken = string.Empty;
            IQuery q = new Query(t =>
            {
                requestToken = t;
                return Task.FromResult<QueryResult>(clientQueryResult);
            });

            // validate
            Assert.IsTrue(q.HasMoreResults);

            string userToken = "AEJGURIOJQ=";
            QueryResponse<Twin> r = q.GetNextAsTwinAsync(new QueryOptions { ContinuationToken = userToken }).Result;
            Assert.AreEqual(userToken, requestToken);
            Assert.AreEqual(serverQueryResult.ContinuationToken, r.ContinuationToken);
            Assert.AreEqual(1, r.Count());
            Assert.IsInstanceOfType(r.ElementAt(0), typeof(Twin));
            Assert.AreEqual("test", r.ElementAt(0).DeviceId);
            Assert.IsTrue(q.HasMoreResults);
        }

        [TestMethod]
        public void QueryResult_OverrideDefaultJsonSerializer_ExceedMaxDepthThrows()
        {
            var settings = new JsonSerializerSettings { MaxDepth = 2 };
            // simulate json deserialize
            const string jsonString = @"
{
""type"":""twin"",
""items"":
    [{
        ""deviceId"": ""test"",
        ""etag"":null,
        ""version"":null,
        ""properties"":
            {
                ""desired"":{},
                ""reported"":{}
            }
    }],
""continuationToken"":""GYUVJDBJFKJ""
}";
            // deserialize
            // act
            Func<QueryResult> act = () => JsonConvert.DeserializeObject<QueryResult>(jsonString, settings);

            // assert
            act.Should().Throw<Newtonsoft.Json.JsonReaderException>();
        }
    }
}
