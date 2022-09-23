// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples.JobsSample
{
    public class JobsSample
    {
        private const string DeviceId = "JobsSample_Device";
        private const string TestTagName = "JobsSample_Tag";
        private const int TestTagValue = 100;

        private readonly IotHubServiceClient _jobClient;

        public JobsSample(IotHubServiceClient jobClient)
        {
            _jobClient = jobClient ?? throw new ArgumentNullException(nameof(jobClient));
        }

        public async Task RunSampleAsync()
        {
            string jobId = "JOBSAMPLE" + Guid.NewGuid().ToString();

            // The query condition can also be on a single device Id or on a list of device Ids.
            // https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-query-language covers 
            //   IoT hub query language in additional detail.
            string query = $"DeviceId IN ['{DeviceId}']";

            var twin = new Twin(DeviceId)
            {
                Tags = new TwinCollection()
            };
            twin.Tags[TestTagName] = TestTagValue;


            // *************************************** Schedule twin job ***************************************
            // Prepare to catch Throttling exception if more than 1 job is already running.
            // https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-quotas-throttling#other-limits
            try
            {
                Console.WriteLine($"Schedule twin job {jobId} for {DeviceId}...");

                var scheduledTwinUpdate = new ScheduledTwinUpdate
                {
                    QueryCondition = query,
                    Twin = twin,
                    StartOnUtc = DateTime.UtcNow,
                };

                var jobOptions = new ScheduledJobsOptions
                {
                    JobId = jobId,
                    MaxExecutionTime = TimeSpan.FromMinutes(2)
                };


                ScheduledJob createJobResponse = await _jobClient.ScheduledJobs
                    .ScheduleTwinUpdateAsync(
                        scheduledTwinUpdate,
                        jobOptions);

                Console.WriteLine("Schedule response");
                Console.WriteLine(JsonSerializer.Serialize(createJobResponse, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine();
            }
            catch (IotHubServiceException ex) when (ex.ErrorCode == IotHubErrorCode.ThrottlingException)
            {
                Console.WriteLine("Too many jobs scheduled at this given time. Please try again later.");
                return;
            }

            // *************************************** Get all Jobs ***************************************
            QueryResponse<ScheduledJob> queryResults = await _jobClient.ScheduledJobs.CreateQueryAsync();

            IEnumerable<ScheduledJob> getJobs = queryResults.CurrentPage;

            foreach (ScheduledJob job in getJobs)
            {
                Console.WriteLine(JsonSerializer.Serialize(job, new JsonSerializerOptions { WriteIndented = true }));
            }

            Console.WriteLine();

            // *************************************** Check completion ***************************************
            Console.WriteLine("Monitoring jobClient for job completion...");
            ScheduledJob jobResponse = await _jobClient.ScheduledJobs.GetAsync(jobId);

            Console.WriteLine("First result");
            Console.WriteLine(JsonSerializer.Serialize(jobResponse, new JsonSerializerOptions { WriteIndented = true }));

            Console.Write("Waiting for completion ");
            while (jobResponse.Status != JobStatus.Completed)
            {
                Console.Write(". ");
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                jobResponse = await _jobClient.ScheduledJobs.GetAsync(jobId);
            }

            Console.WriteLine("DONE");

            Console.WriteLine($"Job ends with status {jobResponse.Status}");
        }
    }
}
