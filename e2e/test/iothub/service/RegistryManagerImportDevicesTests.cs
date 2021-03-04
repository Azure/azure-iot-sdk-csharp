// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Iothub.Service
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

        private static readonly IReadOnlyList<JobStatus> s_incompleteJobs = new[]
        {
            JobStatus.Running,
            JobStatus.Enqueued,
            JobStatus.Queued,
            JobStatus.Scheduled,
            JobStatus.Unknown,
        };

        [DataTestMethod]
        [TestCategory("LongRunning")]
        [Timeout(120000)]
        [DoNotParallelize]
        [DataRow(StorageAuthenticationType.KeyBased)]
        [DataRow(StorageAuthenticationType.IdentityBased)]
        public async Task RegistryManager_ImportDevices(StorageAuthenticationType storageAuthenticationType)
        {
            // arrange

            StorageContainer storageContainer = null;
            string deviceId = $"{nameof(RegistryManager_ImportDevices)}-{StorageContainer.GetRandomSuffix(4)}";
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            Logger.Trace($"Using deviceId {deviceId}");

            try
            {
                string containerName = StorageContainer.BuildContainerName(nameof(RegistryManager_ImportDevices));
                storageContainer = await StorageContainer
                    .GetInstanceAsync(containerName)
                    .ConfigureAwait(false);
                Logger.Trace($"Using container {storageContainer.Uri}");

                Uri containerUri = storageAuthenticationType == StorageAuthenticationType.KeyBased
                    ? storageContainer.SasUri
                    : storageContainer.Uri;

                Stream devicesFile = ImportExportDevicesHelpers.BuildDevicesStream(
                    new List<ExportImportDevice>
                    {
                        new ExportImportDevice(
                            new Device(deviceId)
                            {
                                Authentication = new AuthenticationMechanism { Type = AuthenticationType.Sas }
                            },
                            ImportMode.Create),
                    });
                await UploadFileAndConfirmAsync(storageContainer, devicesFile).ConfigureAwait(false);

                // act

                JobProperties importJobResponse = null;
                int tryCount = 0;
                while (true)
                {
                    try
                    {
                        importJobResponse = await registryManager
                            .ImportDevicesAsync(
                                JobProperties.CreateForImportJob(
                                    containerUri.ToString(),
                                    containerUri.ToString(),
                                    null,
                                    storageAuthenticationType))
                            .ConfigureAwait(false);
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

                // wait for job to complete
                for (int i = 0; i < MaxIterationWait; ++i)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    importJobResponse = await registryManager.GetJobAsync(importJobResponse.JobId).ConfigureAwait(false);
                    Logger.Trace($"Job {importJobResponse.JobId} is {importJobResponse.Status} with progress {importJobResponse.Progress}%");
                    if (!s_incompleteJobs.Contains(importJobResponse.Status))
                    {
                        break;
                    }
                }

                // assert

                importJobResponse.Status.Should().Be(JobStatus.Completed, "Otherwise import failed");
                importJobResponse.FailureReason.Should().BeNullOrEmpty("Otherwise import failed");

                // should not throw due to 404, but device may not immediately appear in registry
                Device device = null;
                for (int i = 0; i < MaxIterationWait; ++i)
                {
                    await Task.Delay(s_waitDuration).ConfigureAwait(false);
                    try
                    {
                        device = await registryManager.GetDeviceAsync(deviceId).ConfigureAwait(false);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Trace($"Could not find device on iteration {i} due to [{ex.Message}]");
                    }
                }
                if (device == null)
                {
                    Assert.Fail($"Device {deviceId} not found in registry manager");
                }
            }
            finally
            {
                try
                {
                    storageContainer?.Dispose();

                    await registryManager.RemoveDeviceAsync(deviceId).ConfigureAwait(false);
                }
                catch { }
            }
        }

        private static async Task UploadFileAndConfirmAsync(StorageContainer storageContainer, Stream devicesFile)
        {
            CloudBlockBlob cloudBlob = storageContainer.CloudBlobContainer.GetBlockBlobReference(ImportFileNameDefault);
            await cloudBlob.UploadFromStreamAsync(devicesFile).ConfigureAwait(false);

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
            foundBlob.Should().BeTrue($"Failed to find {ImportFileNameDefault} in storage container, required for test.");
        }
    }
}
