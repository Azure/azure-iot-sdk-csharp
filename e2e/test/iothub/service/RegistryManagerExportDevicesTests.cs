// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Iothub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class RegistryManagerExportDevicesTests : E2EMsTestBase
    {
        // A bug in either Azure.Storage.Blob or System.Diagnostics causes an exception during container creation
        // so for now, we need to use the older storage nuget.
        // https://github.com/Azure/azure-sdk-for-net/issues/10476

        private const string ExportFileNameDefault = "devices.txt";
        private const int MaxIterationWait = 30;
        private static readonly TimeSpan _waitDuration = TimeSpan.FromSeconds(5);

        private static readonly char[] s_newlines = new char[]
        {
            '\r',
            '\n',
        };

        private static readonly IReadOnlyList<JobStatus> s_incompleteJobs = new[]
        {
            JobStatus.Running,
            JobStatus.Enqueued,
            JobStatus.Queued,
            JobStatus.Scheduled,
            JobStatus.Unknown,
        };

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        [Timeout(120000)]
        [DoNotParallelize]
        [DataRow(StorageAuthenticationType.KeyBased, false)]
        [DataRow(StorageAuthenticationType.IdentityBased, false)]
        [DataRow(StorageAuthenticationType.IdentityBased, true)]
        public async Task RegistryManager_ExportDevices(StorageAuthenticationType storageAuthenticationType, bool isUserAssignedMsi)
        {
            // arrange

            StorageContainer storageContainer = null;
            string edgeId = $"{nameof(RegistryManager_ExportDevices)}-Edge-{StorageContainer.GetRandomSuffix(4)}";
            string deviceId = $"{nameof(RegistryManager_ExportDevices)}-{StorageContainer.GetRandomSuffix(4)}";
            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            Logger.Trace($"Using deviceId {deviceId}");

            try
            {
                string containerName = StorageContainer.BuildContainerName(nameof(RegistryManager_ExportDevices));
                storageContainer = await StorageContainer
                    .GetInstanceAsync(containerName)
                    .ConfigureAwait(false);
                Logger.Trace($"Using container {storageContainer.Uri}");

                Uri containerUri = storageAuthenticationType == StorageAuthenticationType.KeyBased
                    ? storageContainer.SasUri
                    : storageContainer.Uri;

                var edge = await registryManager
                    .AddDeviceAsync(
                        new Device(edgeId)
                        {
                            Authentication = new AuthenticationMechanism { Type = AuthenticationType.Sas },
                            Capabilities = new Shared.DeviceCapabilities { IotEdge = true },
                        })
                    .ConfigureAwait(false);

                await registryManager
                    .AddDeviceAsync(
                        new Device(deviceId)
                        {
                            Authentication = new AuthenticationMechanism { Type = AuthenticationType.Sas },
                            Scope = edge.Scope,
                        })
                    .ConfigureAwait(false);

                // act

                JobProperties exportJobResponse = null;
                int tryCount = 0;
                while (true)
                {
                    try
                    {
                        ManagedIdentity identity = null;
                        if (isUserAssignedMsi)
                        {
                            string userAssignedMsiResourceId = TestConfiguration.IoTHub.UserAssignedMsiResourceId;
                            identity = new ManagedIdentity
                            {
                                userAssignedIdentity = userAssignedMsiResourceId
                            };
                        }

                        exportJobResponse = await registryManager
                           .ExportDevicesAsync(
                                JobProperties.CreateForExportJob(
                                    containerUri.ToString(),
                                    true,
                                    null,
                                    storageAuthenticationType,
                                    identity))
                           .ConfigureAwait(false);
                        break;
                    }
                    // Concurrent jobs can be rejected, so implement a retry mechanism to handle conflicts with other tests
                    catch (JobQuotaExceededException) when (++tryCount < MaxIterationWait)
                    {
                        Logger.Trace($"JobQuotaExceededException... waiting.");
                        await Task.Delay(_waitDuration).ConfigureAwait(false);
                        continue;
                    }
                }

                // wait for job to complete
                for (int i = 0; i < MaxIterationWait; ++i)
                {
                    await Task.Delay(_waitDuration).ConfigureAwait(false);
                    exportJobResponse = await registryManager.GetJobAsync(exportJobResponse.JobId).ConfigureAwait(false);
                    Logger.Trace($"Job {exportJobResponse.JobId} is {exportJobResponse.Status} with progress {exportJobResponse.Progress}%");
                    if (!s_incompleteJobs.Contains(exportJobResponse.Status))
                    {
                        break;
                    }
                }

                // assert

                exportJobResponse.Status.Should().Be(JobStatus.Completed, "Otherwise import failed");
                exportJobResponse.FailureReason.Should().BeNullOrEmpty("Otherwise import failed");

                string content = await DownloadFileAsync(storageContainer).ConfigureAwait(false);
                string[] serializedDevices = content.Split(s_newlines, StringSplitOptions.RemoveEmptyEntries);

                bool foundDeviceInExport = false;
                foreach (string serializedDeivce in serializedDevices)
                {
                    // The first line may be a comment to the user, so skip any lines that don't start with a json object initial character: curly brace
                    if (serializedDeivce[0] != '{')
                    {
                        continue;
                    }

                    ExportImportDevice device = JsonConvert.DeserializeObject<ExportImportDevice>(serializedDeivce);
                    if (StringComparer.Ordinal.Equals(device.Id, deviceId))
                    {
                        Logger.Trace($"Found device in export as [{serializedDeivce}]");
                        foundDeviceInExport = true;
                        device.DeviceScope.Should().Be(edge.Scope);
                        break;
                    }
                }
                foundDeviceInExport.Should().BeTrue("Expected device did not appear in the export");
            }
            finally
            {
                try
                {
                    storageContainer?.Dispose();

                    await registryManager.RemoveDeviceAsync(deviceId).ConfigureAwait(false);
                    await registryManager.RemoveDeviceAsync(edgeId).ConfigureAwait(false);
                }
                catch { }
            }
        }

        private static async Task<string> DownloadFileAsync(StorageContainer storageContainer)
        {
            CloudBlockBlob exportFile = storageContainer.CloudBlobContainer.GetBlockBlobReference(ExportFileNameDefault);
            return await exportFile.DownloadTextAsync().ConfigureAwait(false);
        }
    }
}
