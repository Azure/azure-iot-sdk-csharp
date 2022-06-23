// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Registry;
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
        private const int MaxIterationWait = 60;
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

            const string idPrefix = nameof(RegistryManager_ExportDevices);

            string edgeId1 = $"{idPrefix}-Edge-{StorageContainer.GetRandomSuffix(4)}";
            string edgeId2 = $"{idPrefix}-Edge-{StorageContainer.GetRandomSuffix(4)}";
            string deviceId = $"{idPrefix}-{StorageContainer.GetRandomSuffix(4)}";
            string configurationId = (idPrefix + Guid.NewGuid()).ToLower(); // Configuration Id characters must be all lower-case.
            Logger.Trace($"Using Ids {deviceId}, {edgeId1}, {edgeId2}, and {configurationId}");

            string devicesFileName = $"{idPrefix}-devicesexport-{StorageContainer.GetRandomSuffix(4)}.txt";
            string configsFileName = $"{idPrefix}-configsexport-{StorageContainer.GetRandomSuffix(4)}.txt";

            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            using RegistryClient registryClient = new RegistryClient(TestConfiguration.IoTHub.ConnectionString);

            try
            {
                string containerName = StorageContainer.BuildContainerName(idPrefix);
                using StorageContainer storageContainer = await StorageContainer
                    .GetInstanceAsync(containerName)
                    .ConfigureAwait(false);
                Logger.Trace($"Using container {storageContainer.Uri}");

                Uri containerUri = storageAuthenticationType == StorageAuthenticationType.KeyBased
                    ? storageContainer.SasUri
                    : storageContainer.Uri;

                Device edge1 = await registryClient
                    .AddDeviceAsync(
                        new Device(edgeId1)
                        {
                            Authentication = new AuthenticationMechanism { Type = AuthenticationType.Sas },
                            Capabilities = new DeviceCapabilities { IotEdge = true },
                        })
                    .ConfigureAwait(false);

                Device edge2 = await registryClient
                    .AddDeviceAsync(
                        new Device(edgeId2)
                        {
                            Authentication = new AuthenticationMechanism { Type = AuthenticationType.Sas },
                            Capabilities = new DeviceCapabilities { IotEdge = true },
                            ParentScopes = { edge1.Scope },
                        })
                    .ConfigureAwait(false);

                Device device = await registryClient
                    .AddDeviceAsync(
                        new Device(deviceId)
                        {
                            Authentication = new AuthenticationMechanism { Type = AuthenticationType.Sas },
                            Scope = edge1.Scope,
                        })
                    .ConfigureAwait(false);

                Configuration configuration = await registryManager
                    .AddConfigurationAsync(
                        new Configuration(configurationId)
                        {
                            Priority = 2,
                            Labels = { { "labelName", "labelValue" } },
                            TargetCondition = "*",
                            Content = new ConfigurationContent
                            {
                                DeviceContent = { { "properties.desired.x", 4L } },
                            },
                            Metrics = new ConfigurationMetrics
                            {
                                Queries = { { "successfullyConfigured", "select deviceId from devices where properties.reported.x = 4" } }
                            },
                        })
                    .ConfigureAwait(false);

                // act

                JobProperties exportJobResponse = await CreateAndWaitForJobAsync(
                        storageAuthenticationType,
                        isUserAssignedMsi,
                        devicesFileName,
                        configsFileName,
                        registryClient,
                        containerUri)
                    .ConfigureAwait(false);

                // assert
                await ValidateDevicesAsync(
                        devicesFileName,
                        storageContainer,
                        edge1,
                        edge2,
                        device)
                .ConfigureAwait(false);
                await ValidateConfigurationsAsync(
                        configsFileName,
                        storageContainer,
                        configuration)
                    .ConfigureAwait(false);
            }
            finally
            {
                await CleanUpDevicesAsync(edgeId1, edgeId2, deviceId, configurationId, registryManager, registryClient).ConfigureAwait(false);
            }
        }

        private async Task<JobProperties> CreateAndWaitForJobAsync(
            StorageAuthenticationType storageAuthenticationType,
            bool isUserAssignedMsi,
            string devicesFileName,
            string configsFileName,
            RegistryClient registryClient,
            Uri containerUri)
        {
            int tryCount = 0;

            ManagedIdentity identity = isUserAssignedMsi
                ? new ManagedIdentity
                {
                    UserAssignedIdentity = TestConfiguration.IoTHub.UserAssignedMsiResourceId
                }
                : null;

            JobProperties exportJobResponse = JobProperties.CreateForExportJob(
                containerUri.ToString(),
                true,
                devicesFileName,
                storageAuthenticationType,
                identity);
            exportJobResponse.IncludeConfigurations = true;
            exportJobResponse.ConfigurationsBlobName = configsFileName;

            while (tryCount < MaxIterationWait)
            {
                try
                {
                    exportJobResponse = await registryClient.ExportDevicesAsync(exportJobResponse).ConfigureAwait(false);
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

            for (int i = 0; i < MaxIterationWait; ++i)
            {
                await Task.Delay(s_waitDuration).ConfigureAwait(false);
                exportJobResponse = await registryClient.GetJobAsync(exportJobResponse.JobId).ConfigureAwait(false);
                Logger.Trace($"Job {exportJobResponse.JobId} is {exportJobResponse.Status} with progress {exportJobResponse.Progress}%");
                if (!s_incompleteJobs.Contains(exportJobResponse.Status))
                {
                    break;
                }
            }

            exportJobResponse.Status.Should().Be(JobStatus.Completed, "Otherwise import failed");
            exportJobResponse.FailureReason.Should().BeNullOrEmpty("Otherwise import failed");

            return exportJobResponse;
        }

        private async Task ValidateDevicesAsync(
            string devicesFileName,
            StorageContainer storageContainer,
            Device edge1,
            Device edge2,
            Device device)
        {
            string devicesContent = await DownloadFileAsync(storageContainer, devicesFileName).ConfigureAwait(false);
            string[] serializedDevices = devicesContent.Split(s_newlines, StringSplitOptions.RemoveEmptyEntries);

            bool foundEdge1InExport = false;
            bool foundEdge2InExport = false;
            bool foundDeviceInExport = false;

            foreach (string serializedDevice in serializedDevices)
            {
                // The first line may be a comment to the user, so skip any lines that don't start with a json object initial character: curly brace
                if (serializedDevice[0] != '{')
                {
                    continue;
                }

                if (foundEdge1InExport
                    && foundEdge2InExport
                    && foundDeviceInExport)
                {
                    // we're done
                    break;
                }

                ExportImportDevice exportedDevice = JsonConvert.DeserializeObject<ExportImportDevice>(serializedDevice);

                if (StringComparer.Ordinal.Equals(exportedDevice.Id, edge1.Id) && exportedDevice.Capabilities.IotEdge)
                {
                    Logger.Trace($"Found edge1 in export as [{serializedDevice}]");
                    foundEdge1InExport = true;
                    exportedDevice.DeviceScope.Should().Be(edge1.Scope, "Edges retain their own scope");
                    continue;
                }

                if (StringComparer.Ordinal.Equals(exportedDevice.Id, edge2.Id) && exportedDevice.Capabilities.IotEdge)
                {
                    Logger.Trace($"Found edge2 in export as [{serializedDevice}]");
                    foundEdge2InExport = true;
                    exportedDevice.DeviceScope.Should().Be(edge2.Scope, "Edges retain their own scope");
                    continue;
                }

                if (StringComparer.Ordinal.Equals(exportedDevice.Id, device.Id))
                {
                    Logger.Trace($"Found device in export as [{serializedDevice}]");
                    foundDeviceInExport = true;
                    exportedDevice.DeviceScope.Should().Be(edge1.Scope);
                    continue;
                }
            }
            foundEdge1InExport.Should().BeTrue("Expected edge did not appear in the export");
            foundEdge2InExport.Should().BeTrue("Expected edge did not appear in the export");
            foundDeviceInExport.Should().BeTrue("Expected device did not appear in the export");
        }

        private async Task ValidateConfigurationsAsync(
            string configsFileName,
            StorageContainer storageContainer,
            Configuration configuration)
        {
            string configsContent = await DownloadFileAsync(storageContainer, configsFileName).ConfigureAwait(false);
            string[] serializedConfigs = configsContent.Split(s_newlines, StringSplitOptions.RemoveEmptyEntries);

            bool foundConfig = false;
            foreach (string serializedConfig in serializedConfigs)
            {
                Configuration exportedConfig = JsonConvert.DeserializeObject<Configuration>(serializedConfig);
                if (StringComparer.Ordinal.Equals(exportedConfig.Id, configuration.Id))
                {
                    Logger.Trace($"Found config in export as [{serializedConfig}]");
                    foundConfig = true;
                }
            }

            foundConfig.Should().BeTrue();
        }

        private static async Task<string> DownloadFileAsync(StorageContainer storageContainer, string fileName)
        {
            CloudBlockBlob exportFile = storageContainer.CloudBlobContainer.GetBlockBlobReference(fileName);
            return await exportFile.DownloadTextAsync().ConfigureAwait(false);
        }

        private async Task CleanUpDevicesAsync(
            string edgeId1,
            string edgeId2,
            string deviceId,
            string configurationId,
            RegistryManager registryManager,
            RegistryClient registryClient)
        {
            try
            {
                await registryClient.RemoveDeviceAsync(deviceId).ConfigureAwait(false);
                await registryClient.RemoveDeviceAsync(edgeId2).ConfigureAwait(false);
                await registryClient.RemoveDeviceAsync(edgeId1).ConfigureAwait(false);
                await registryManager.RemoveConfigurationAsync(configurationId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Trace($"Failed to remove device/config during cleanup due to {ex}");
            }
        }
    }
}
