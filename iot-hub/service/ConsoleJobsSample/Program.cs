// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Devices.Samples
{
    class Program
    {
        // Either set the IOTHUB_DEVICE_CONN_STRING environment variable or within launchSettings.json:
        private static string connectionString = Environment.GetEnvironmentVariable("IOTHUB_CONN_STRING_CSHARP");

        const string deviceId = "new_device";
        const string TestTagName = "Tag1";
        const int TestTagValue = 100;

        static void Main(string[] args)
        {
            string jobId = "DHCMD" + Guid.NewGuid().ToString();

            // The query condition can also be on a single device Id or on a list of device Ids.
            // https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-query-language covers 
            //   IoT Hub query language in additional detail.
            string query = $"DeviceId IN ['{deviceId}']";

            Twin twin = new Twin(deviceId);
            twin.Tags = new TwinCollection();
            twin.Tags[TestTagName] = TestTagValue;

            // *************************************** Start JobClient ***************************************
            Console.WriteLine($"Create JobClient from the connectionString...");
            JobClient jobClient = JobClient.CreateFromConnectionString(connectionString);
            Console.WriteLine($"JobClient created with success");
            Console.WriteLine();

            // *************************************** Schedule twin job ***************************************
            Console.WriteLine($"Schedule twin job {jobId} for {deviceId}...");
            JobResponse createJobResponse = jobClient.ScheduleTwinUpdateAsync(
                jobId, query, twin, DateTime.UtcNow, (long)TimeSpan.FromMinutes(2).TotalSeconds).Result;
            Console.WriteLine($"Schedule response");
            Console.WriteLine(JsonConvert.SerializeObject(createJobResponse, Formatting.Indented));
            Console.WriteLine();

            // *************************************** Get all Jobs ***************************************
            IEnumerable<JobResponse> queryResults = jobClient.CreateQuery().GetNextAsJobResponseAsync().Result;
            var getJobs = queryResults.ToList();

            Console.WriteLine($"getJobs return {getJobs.Count} result(s)");

            foreach (JobResponse job in getJobs)
            {
                Console.WriteLine(JsonConvert.SerializeObject(job, Formatting.Indented));

                if (job.Status != JobStatus.Completed)
                {
                    Console.WriteLine($"Incorrect query jobs result");
                    return;
                }
            }
            Console.WriteLine();

            // *************************************** Check completion ***************************************
            Console.WriteLine($"Monitoring jobClient for job completion...");
            JobResponse jobResponse = jobClient.GetJobAsync(jobId).Result;
            Console.WriteLine($"First result");
            Console.WriteLine(JsonConvert.SerializeObject(jobResponse, Formatting.Indented));
            while(jobResponse.Status != JobStatus.Completed)
            {
                Task.Delay(TimeSpan.FromMilliseconds(100));
                jobResponse = jobClient.GetJobAsync(jobId).Result;
            }
            Console.WriteLine($"Job ends with status {jobResponse.Status}");
        }

    }
}
