﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class RegistryManagerImportDevicesTests : E2EMsTestBase
    {
        // A bug in either Storage or System.Diagnostics causes an exception during container creation
        // so for now, we need to use the older storage nuget.
        // https://github.com/Azure/azure-sdk-for-net/issues/10476

        private const string ImportFileNameDefault = "devices.txt";
        private const int MaxIterationWait = 30;
        private static readonly TimeSpan s_waitDuration = TimeSpan.FromSeconds(5);

        [DataTestMethod]
        [TestCategory("LongRunning")]
        [Timeout(LongRunningTestTimeoutMilliseconds)] // the number of jobs that can be run at a time are limited anyway
        [DoNotParallelize]
        [DataRow(StorageAuthenticationType.KeyBased, false)]
        [DataRow(StorageAuthenticationType.IdentityBased, false)]
        [DataRow(StorageAuthenticationType.IdentityBased, true)]
        public async Task RegistryManager_ImportDevices(StorageAuthenticationType storageAuthenticationType, bool isUserAssignedMsi)
        {
            // arrange

            const string idPrefix = nameof(RegistryManager_ImportDevices);

            string deviceId = $"{idPrefix}-device-{StorageContainer.GetRandomSuffix(4)}";
            string configId = $"{idPrefix}-config-{StorageContainer.GetRandomSuffix(4)}".ToLower(); // Configuration Id characters must be all lower-case.
            VerboseTestLogger.WriteLine($"Using Ids {deviceId} and {configId}.");

            string devicesFileName = $"{idPrefix}-devices-{StorageContainer.GetRandomSuffix(4)}.txt";
            string configsFileName = $"{idPrefix}-configs-{StorageContainer.GetRandomSuffix(4)}.txt";

            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);

            try
            {
                string containerName = StorageContainer.BuildContainerName(nameof(RegistryManager_ImportDevices));
                using StorageContainer storageContainer = await StorageContainer.GetInstanceAsync(containerName).ConfigureAwait(false);
                VerboseTestLogger.WriteLine($"Using devices container {storageContainer.Uri}");

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
                            TargetCondition = "deviceId='fakeDevice'",
                            Content =
                            {
                                DeviceContent = { { "properties.desired.x", 5L } },
                            },
                            Metrics =
                            {
                                Queries = { { "successfullyConfigured", "select deviceId from devices where properties.reported.x = 5" } }
                            },
                        },
                    });
                await UploadFileAndConfirmAsync(storageContainer, configsStream, configsFileName).ConfigureAwait(false);

                ManagedIdentity identity = isUserAssignedMsi
                    ? new ManagedIdentity
                    {
                        UserAssignedIdentity = TestConfiguration.IotHub.UserAssignedMsiResourceId
                    }
                    : null;

                // act

                JobProperties importJobResponse = await CreateAndWaitForJobAsync(
                        storageAuthenticationType,
                        devicesFileName,
                        configsFileName,
                        registryManager,
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
                        device = await registryManager.GetDeviceAsync(deviceId).ConfigureAwait(false);
                        config = await registryManager.GetConfigurationAsync(configId).ConfigureAwait(false);
                        break;
                    }
                    catch (Exception ex)
                    {
                        VerboseTestLogger.WriteLine($"Could not find device/config on iteration {i} due to [{ex.Message}]");
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
                    await registryManager.RemoveDeviceAsync(deviceId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    VerboseTestLogger.WriteLine($"Failed to clean up device {deviceId} due to {ex.Message}");
                }

                try
                {
                    await registryManager.RemoveConfigurationAsync(configId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    VerboseTestLogger.WriteLine($"Failed to clean up config {configId} due to {ex.Message}");
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
            RegistryManager registryManager,
            Uri containerUri,
            ManagedIdentity identity)
        {
            JobProperties jobProperties = JobProperties.CreateForImportJob(
                containerUri.ToString(),
                containerUri.ToString(),
                devicesFileName,
                storageAuthenticationType,
                identity);
            jobProperties.ConfigurationsBlobName = configsFileName;
            jobProperties.IncludeConfigurations = true;

            var sw = Stopwatch.StartNew();

            while (!jobProperties.IsFinished)
            {
                try
                {
                    jobProperties = await registryManager.ImportDevicesAsync(jobProperties).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(jobProperties.FailureReason))
                    {
                        VerboseTestLogger.WriteLine($"Job failed due to {jobProperties.FailureReason}");
                    }
                    break;
                }
                // Concurrent jobs can be rejected, so implement a retry mechanism to handle conflicts with other tests running jobs.
                catch (JobQuotaExceededException)
                {
                    VerboseTestLogger.WriteLine($"JobQuotaExceededException... waiting after {sw.Elapsed}.");
                    await Task.Delay(s_waitDuration).ConfigureAwait(false);
                    continue;
                }
            }

            sw.Stop();
            VerboseTestLogger.WriteLine($"Job started after {sw.Elapsed}.");

            sw.Restart();

            // Wait for job to complete
            while (!jobProperties.IsFinished)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                jobProperties = await registryManager.GetJobAsync(jobProperties.JobId).ConfigureAwait(false);
                VerboseTestLogger.WriteLine($"Job {jobProperties.JobId} is {jobProperties.Status} with progress {jobProperties.Progress}% after {sw.Elapsed}.");
            }

            return jobProperties;
        }
    }
}
