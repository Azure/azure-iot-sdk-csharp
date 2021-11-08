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
        private static readonly TimeSpan s_waitDuration = TimeSpan.FromSeconds(3);

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
        [DoNotParallelize] // the number of jobs that can be run at a time are limited anyway
        [DataRow(StorageAuthenticationType.KeyBased, false)]
        [DataRow(StorageAuthenticationType.IdentityBased, false)]
        [DataRow(StorageAuthenticationType.IdentityBased, true)]
        public async Task RegistryManager_ExportDevices(StorageAuthenticationType storageAuthenticationType, bool isUserAssignedMsi)
        {
            // arrange

            string edgeId1 = $"{nameof(RegistryManager_ExportDevices)}-Edge-{StorageContainer.GetRandomSuffix(4)}";
            string edgeId2 = $"{nameof(RegistryManager_ExportDevices)}-Edge-{StorageContainer.GetRandomSuffix(4)}";
            string deviceId = $"{nameof(RegistryManager_ExportDevices)}-{StorageContainer.GetRandomSuffix(4)}";
            string devicesFileName = $"{nameof(RegistryManager_ExportDevices)}-devicesexport-{StorageContainer.GetRandomSuffix(4)}.txt";
            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            Logger.Trace($"Using deviceId {deviceId}");

            try
            {
                string containerName = StorageContainer.BuildContainerName(nameof(RegistryManager_ExportDevices));
                using StorageContainer storageContainer = await StorageContainer
                    .GetInstanceAsync(containerName)
                    .ConfigureAwait(false);
                Logger.Trace($"Using container {storageContainer.Uri}");

                Uri containerUri = storageAuthenticationType == StorageAuthenticationType.KeyBased
                    ? storageContainer.SasUri
                    : storageContainer.Uri;

                var edge1 = await registryManager
                    .AddDeviceAsync(
                        new Device(edgeId1)
                        {
                            Authentication = new AuthenticationMechanism { Type = AuthenticationType.Sas },
                            Capabilities = new Shared.DeviceCapabilities { IotEdge = true },
                        })
                    .ConfigureAwait(false);

                var edge2 = await registryManager
                    .AddDeviceAsync(
                        new Device(edgeId2)
                        {
                            Authentication = new AuthenticationMechanism { Type = AuthenticationType.Sas },
                            Capabilities = new Shared.DeviceCapabilities { IotEdge = true },
                            ParentScopes = { edge1.Scope },
                        })
                    .ConfigureAwait(false);

                await registryManager
                    .AddDeviceAsync(
                        new Device(deviceId)
                        {
                            Authentication = new AuthenticationMechanism { Type = AuthenticationType.Sas },
                            Scope = edge1.Scope,
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

                        JobProperties jobProperties = JobProperties.CreateForExportJob(
                            containerUri.ToString(),
                            true,
                            devicesFileName,
                            storageAuthenticationType,
                            identity);
                        exportJobResponse = await registryManager.ExportDevicesAsync(jobProperties).ConfigureAwait(false);
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
                    await Task.Delay(s_waitDuration).ConfigureAwait(false);
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

                string devicesContent = await DownloadFileAsync(storageContainer, devicesFileName).ConfigureAwait(false);
                string[] serializedDevices = devicesContent.Split(s_newlines, StringSplitOptions.RemoveEmptyEntries);

                bool foundDeviceInExport = false;
                bool foundEdgeInExport = false;
                foreach (string serializedDevice in serializedDevices)
                {
                    // The first line may be a comment to the user, so skip any lines that don't start with a json object initial character: curly brace
                    if (serializedDevice[0] != '{')
                    {
                        continue;
                    }

                    if (foundEdgeInExport && foundDeviceInExport)
                    {
                        // we're done
                        break;
                    }

                    ExportImportDevice exportedDevice = JsonConvert.DeserializeObject<ExportImportDevice>(serializedDevice);
                    if (StringComparer.Ordinal.Equals(exportedDevice.Id, edgeId2) && exportedDevice.Capabilities.IotEdge)
                    {
                        Logger.Trace($"Found edge2 in export as [{serializedDevice}]");
                        foundEdgeInExport = true;
                        exportedDevice.DeviceScope.Should().Be(edge2.Scope, "Edges retain their own scope");

                        // This is broken. The export doesn't include the ParentScopes property.
                        // Disable this assert until it is fixed in the service.
                        //exportedDevice.ParentScopes.First().Should().Be(edge1.Scope);
                        continue;
                    }

                    if (StringComparer.Ordinal.Equals(exportedDevice.Id, deviceId))
                    {
                        Logger.Trace($"Found device in export as [{serializedDevice}]");
                        foundDeviceInExport = true;
                        exportedDevice.DeviceScope.Should().Be(edge1.Scope);
                        continue;
                    }
                }
                foundEdgeInExport.Should().BeTrue("Expected edge did not appear in the export");
                foundDeviceInExport.Should().BeTrue("Expected device did not appear in the export");
            }
            finally
            {
                try
                {
                    await registryManager.RemoveDeviceAsync(deviceId).ConfigureAwait(false);
                    await registryManager.RemoveDeviceAsync(edgeId2).ConfigureAwait(false);
                    await registryManager.RemoveDeviceAsync(edgeId1).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Trace($"Failed to remove device during cleanup due to {ex}");
                }
            }
        }

        private static async Task<string> DownloadFileAsync(StorageContainer storageContainer, string fileName)
        {
            CloudBlockBlob exportFile = storageContainer.CloudBlobContainer.GetBlockBlobReference(fileName);
            string fileContents = await exportFile.DownloadTextAsync().ConfigureAwait(false);

            return fileContents;
        }
    }
}
