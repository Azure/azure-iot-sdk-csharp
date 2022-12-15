// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

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
            VerboseTestLogger.WriteLine($"Using Ids {deviceId} and {configId}.");

            string devicesFileName = $"{idPrefix}-devices-{StorageContainer.GetRandomSuffix(4)}.txt";
            string configsFileName = $"{idPrefix}-configs-{StorageContainer.GetRandomSuffix(4)}.txt";

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            try
            {
                string containerName = StorageContainer.BuildContainerName(nameof(DevicesClient_ImportDevices));
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
                                Authentication = new AuthenticationMechanism { Type = ClientAuthenticationType.Sas }
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
                        UserAssignedIdentity = TestConfiguration.IotHub.UserAssignedMsiResourceId
                    }
                    : null;

                // act

                IotHubJobResponse importJobResponse = await ImportDevicesTests.CreateAndWaitForJobAsync(
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
                    await serviceClient.Devices.DeleteAsync(deviceId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    VerboseTestLogger.WriteLine($"Failed to clean up device {deviceId} due to {ex.Message}");
                }

                try
                {
                    await serviceClient.Configurations.DeleteAsync(configId).ConfigureAwait(false);
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

        private static async Task<IotHubJobResponse> CreateAndWaitForJobAsync(
            StorageAuthenticationType storageAuthenticationType,
            string devicesFileName,
            string configsFileName,
            IotHubServiceClient serviceClient,
            Uri containerUri,
            ManagedIdentity identity)
        {
            var importJobProperties = new ImportJobProperties(containerUri)
            {
                InputBlobName = devicesFileName,
                StorageAuthenticationType = storageAuthenticationType,
                Identity = identity,
                ConfigurationsBlobName = configsFileName,
                IncludeConfigurations = true,
            };

            var sw = Stopwatch.StartNew();

            while (!importJobProperties.IsFinished)
            {
                try
                {
                    importJobProperties = await serviceClient.Devices.ImportAsync(importJobProperties).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(importJobProperties.FailureReason))
                    {
                        VerboseTestLogger.WriteLine($"Job failed due to {importJobProperties.FailureReason}");
                    }
                    break;
                }
                // Concurrent jobs can be rejected, so implement a retry mechanism to handle conflicts with other tests running jobs.
                catch (IotHubServiceException ex) when (ex.StatusCode is (HttpStatusCode)429)
                {
                    VerboseTestLogger.WriteLine($"JobQuotaExceededException... waiting after {sw.Elapsed}.");
                    await Task.Delay(s_waitDuration).ConfigureAwait(false);
                    continue;
                }
            }

            sw.Stop();
            VerboseTestLogger.WriteLine($"Job started after {sw.Elapsed}.");

            sw.Restart();
            IotHubJobResponse jobResponse;
            // Wait for job to complete
            while (true)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                jobResponse = await serviceClient.Devices.GetJobAsync(importJobProperties.JobId).ConfigureAwait(false);
                VerboseTestLogger.WriteLine($"Job {jobResponse.JobId} is {jobResponse.Status} after {sw.Elapsed}.");
                if (jobResponse.IsFinished)
                {
                    break;
                }
            }

            return jobResponse;
        }
    }
}
