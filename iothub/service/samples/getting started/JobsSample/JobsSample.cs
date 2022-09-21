// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;

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
                JobResponse createJobResponse = await _jobClient
                    .ScheduleTwinUpdateAsync(
                        jobId,
                        query,
                        twin,
                        DateTime.UtcNow,
                        (long)TimeSpan.FromMinutes(2).TotalSeconds);

                Console.WriteLine("Schedule response");
                Console.WriteLine(JsonSerializer.Serialize(createJobResponse, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine();
            }
            catch (ThrottlingException)
            {
                Console.WriteLine("Too many jobs scheduled at this given time. Please try again later.");
                return;
            }

            // *************************************** Get all Jobs ***************************************
            IEnumerable<JobResponse> queryResults = await _jobClient.CreateQuery().GetNextAsJobResponseAsync();

            List<JobResponse> getJobs = queryResults.ToList();
            Console.WriteLine($"getJobs return {getJobs.Count} result(s)");

            foreach (JobResponse job in getJobs)
            {
                Console.WriteLine(JsonSerializer.Serialize(job, new JsonSerializerOptions { WriteIndented = true }));
            }

            Console.WriteLine();

            // *************************************** Check completion ***************************************
            Console.WriteLine("Monitoring jobClient for job completion...");
            JobResponse jobResponse = await _jobClient.GetJobAsync(jobId);

            Console.WriteLine("First result");
            Console.WriteLine(JsonSerializer.Serialize(jobResponse, new JsonSerializerOptions { WriteIndented = true }));

            Console.Write("Waiting for completion ");
            while (jobResponse.Status != JobStatus.Completed)
            {
                Console.Write(". ");
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                jobResponse = await _jobClient.GetJobAsync(jobId);
            }

            Console.WriteLine("DONE");

            Console.WriteLine($"Job ends with status {jobResponse.Status}");
        }
    }
}
