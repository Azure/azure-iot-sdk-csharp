// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class ImportDevicesTests : E2EMsTestBase
    {
        // A bug in either Storage or System.Diagnostics causes an exception during container creation
        // so for now, we need to use the older storage nuget.
        // https://github.com/Azure/azure-sdk-for-net/issues/10476

        private const string ImportFileNameDefault = "devices.txt";
        private const int MaxIterationWait = 30;
        private static readonly TimeSpan s_waitDuration = TimeSpan.FromSeconds(5);

        private static readonly IReadOnlyList<JobStatus> s_incompleteJobs = new[]
        {
            JobStatus.Running,
            JobStatus.Enqueued,
            JobStatus.Queued,
            JobStatus.Scheduled,
            JobStatus.Unknown,
        };

        [Ignore]
        [DataTestMethod]
        [TestCategory("LongRunning")]
        [Timeout(LongRunningTestTimeoutMilliseconds)] // the number of jobs that can be run at a time are limited anyway
        [DoNotParallelize]
        [DataRow(StorageAuthenticationType.KeyBased, false)]
        [DataRow(StorageAuthenticationType.IdentityBased, false)]
        [DataRow(StorageAuthenticationType.IdentityBased, true)]
        public async Task DevicesClient_ImportDevices(StorageAuthenticationType storageAuthenticationType, bool isUserAssignedMsi)
        {
            // arrange

            const string idPrefix = nameof(DevicesClient_ImportDevices);

            string deviceId = $"{idPrefix}-device-{StorageContainer.GetRandomSuffix(4)}";
            string configId = $"{idPrefix}-config-{StorageContainer.GetRandomSuffix(4)}".ToLower(); // Configuration Id characters must be all lower-case.
            Logger.Trace($"Using Ids {deviceId} and {configId}.");

            string devicesFileName = $"{idPrefix}-devices-{StorageContainer.GetRandomSuffix(4)}.txt";
            string configsFileName = $"{idPrefix}-configs-{StorageContainer.GetRandomSuffix(4)}.txt";

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);

            try
            {
                string containerName = StorageContainer.BuildContainerName(nameof(DevicesClient_ImportDevices));
                using StorageContainer storageContainer = await StorageContainer.GetInstanceAsync(containerName).ConfigureAwait(false);
                Logger.Trace($"Using devices container {storageContainer.Uri}");

                Uri containerUri = storageAuthenticationType == StorageAuthenticationType.KeyBased
                    ? storageContainer.SasUri
                    : storageContainer.Uri;

                using Stream devicesStream = ImportExportHelpers.BuildImportStream(
                    new List<ExportImportDevice>
                    {
                        new ExportImportDevice(
                            new Device(deviceId)
                            {
                                Authentication = new AuthenticationMechanism { Type = AuthenticationType.Sas }
                            },
                            ImportMode.Create),
                    });
                await UploadFileAndConfirmAsync(storageContainer, devicesStream, devicesFileName).ConfigureAwait(false);

                using Stream configsStream = ImportExportHelpers.BuildImportStream(
                    new List<ImportConfiguration>
                    {
                        new ImportConfiguration(configId)
                        {
                            ImportMode = ConfigurationImportMode.CreateOrUpdateIfMatchETag,
                            Priority = 3,
                            Labels = { { "labelName", "labelValue" } },
                            TargetCondition = "*",
                            Content = new ConfigurationContent
                            {
                                DeviceContent = { { "properties.desired.x", 5L } },
                            },
                            Metrics = new ConfigurationMetrics
                            {
                                Queries = { { "successfullyConfigured", "select deviceId from devices where properties.reported.x = 5" } }
                            },
                        },
                    });
                await UploadFileAndConfirmAsync(storageContainer, configsStream, configsFileName).ConfigureAwait(false);

                ManagedIdentity identity = isUserAssignedMsi
                    ? new ManagedIdentity
                    {
                        UserAssignedIdentity = TestConfiguration.IoTHub.UserAssignedMsiResourceId
                    }
                    : null;

                // act

                JobProperties importJobResponse = await CreateAndWaitForJobAsync(
                        storageAuthenticationType,
                        devicesFileName,
                        configsFileName,
                        serviceClient,
                        containerUri,
                        identity)
                    .ConfigureAwait(false);

                // assert

                importJobResponse.Status.Should().Be(JobStatus.Completed, $"Import failed due to '{importJobResponse.FailureReason}'.");
                importJobResponse.FailureReason.Should().BeNullOrEmpty("Import failed.");

                // should not throw due to 404, but device may not immediately appear in registry
                Device device = null;
                Configuration config = null;
                for (int i = 0; i < MaxIterationWait; ++i)
                {
                    await Task.Delay(s_waitDuration).ConfigureAwait(false);
                    try
                    {
                        device = await serviceClient.Devices.GetAsync(deviceId).ConfigureAwait(false);
                        config = await serviceClient.Configurations.GetAsync(configId).ConfigureAwait(false);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Trace($"Could not find device/config on iteration {i} due to [{ex.Message}]");
                    }
                }
                if (device == null)
                {
                    Assert.Fail($"Device {deviceId} not found in registry manager");
                }
                if (config == null)
                {
                    Assert.Fail($"Config {configId} not found in registry manager");
                }
            }
            finally
            {
                try
                {
                    await serviceClient.Devices.DeleteAsync(deviceId).ConfigureAwait(false);
                    await serviceClient.Configurations.DeleteAsync(configId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Trace($"Failed to clean up device/config due to {ex}");
                }
            }
        }

        private static async Task UploadFileAndConfirmAsync(StorageContainer storageContainer, Stream fileContents, string fileName)
        {
            CloudBlockBlob cloudBlob = storageContainer.CloudBlobContainer.GetBlockBlobReference(fileName);
            await cloudBlob.UploadFromStreamAsync(fileContents).ConfigureAwait(false);

            // wait for blob to be written
            bool foundBlob = false;
            for (int i = 0; i < MaxIterationWait; ++i)
            {
                await Task.Delay(s_waitDuration).ConfigureAwait(false);
                if (await cloudBlob.ExistsAsync().ConfigureAwait(false))
                {
                    foundBlob = true;
                    break;
                }
            }
            foundBlob.Should().BeTrue($"Failed to find {fileName} in storage container - required for test.");
        }

        private async Task<JobProperties> CreateAndWaitForJobAsync(
            StorageAuthenticationType storageAuthenticationType,
            string devicesFileName,
            string configsFileName,
            IotHubServiceClient serviceClient,
            Uri containerUri,
            ManagedIdentity identity)
        {
            int tryCount = 0;
            JobProperties importJobResponse = null;

            JobProperties jobProperties = JobProperties.CreateForImportJob(
                containerUri,
                containerUri,
                devicesFileName,
                storageAuthenticationType,
                identity);
            jobProperties.ConfigurationsBlobName = configsFileName;
            jobProperties.IncludeConfigurations = true;

            while (tryCount < MaxIterationWait)
            {
                try
                {
                    importJobResponse = await serviceClient.Devices.ImportAsync(jobProperties).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(importJobResponse.FailureReason))
                    {
                        Logger.Trace($"Job failed due to {importJobResponse.FailureReason}");
                    }
                    break;
                }
                // Concurrent jobs can be rejected, so implement a retry mechanism to handle conflicts with other tests
                catch (JobQuotaExceededException) when (++tryCount < MaxIterationWait)
                {
                    Logger.Trace($"JobQuotaExceededException... waiting.");
                    await Task.Delay(s_waitDuration).ConfigureAwait(false);
                    continue;
                }
            }

            // Wait for job to complete
            for (int i = 0; i < MaxIterationWait; ++i)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                importJobResponse = await serviceClient.Devices.GetJobAsync(importJobResponse?.JobId).ConfigureAwait(false);
                Logger.Trace($"Job {importJobResponse.JobId} is {importJobResponse.Status} with progress {importJobResponse.Progress}%");
                if (!s_incompleteJobs.Contains(importJobResponse.Status))
                {
                    break;
                }
            }

            return importJobResponse;
        }
    }
}
