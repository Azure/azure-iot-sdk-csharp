
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests.Scenarios
{
    public class ImportIotHubConfigTest : PerfScenario
    {
        public ImportIotHubConfigTest(PerfScenarioConfig c) : base(c) { }

        public override Task RunTestAsync(CancellationToken ct)
        {
            throw new OperationCanceledException();
        }

        public override async Task SetupAsync(CancellationToken ct)
        {
            if (_id != 0) return;

            try
            {

                using (var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
                {
                    JobProperties importJob = await registryManager.ImportDevicesAsync(
                        Configuration.Stress.ImportExportBlobUri,
                        Configuration.Stress.ImportExportBlobUri).ConfigureAwait(false);

                    // Wait until job is finished
                    while (true)
                    {
                        importJob = await registryManager.GetJobAsync(importJob.JobId).ConfigureAwait(false);
                        Console.WriteLine($"\rImport job '{importJob.JobId}' Status: {importJob.Status} Progress: {importJob.Progress}%]           ");

                        if (importJob.Status == JobStatus.Completed) return;
                        else if (importJob.Status == JobStatus.Failed || importJob.Status == JobStatus.Cancelled)
                        {
                            string error = $"Import job '{importJob.JobId}' failed ({importJob.Progress}% done). Status: {importJob.Status}, Reason: {importJob.FailureReason}";
                            throw new IotHubException(error, isTransient: false);
                        }

                        await Task.Delay(5000).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n\n{ex.Message}\n\n");
                await Task.Delay(10000).ConfigureAwait(false);

                throw;
            }
        }

        public override Task TeardownAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
