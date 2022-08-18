// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
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

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
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

                Device edge1 = await registryManager
                    .AddDeviceAsync(
                        new Device(edgeId1)
                        {
                            Authentication = new AuthenticationMechanism { Type = AuthenticationType.Sas },
                            Capabilities = new Shared.DeviceCapabilities { IotEdge = true },
                        })
                    .ConfigureAwait(false);

                Device edge2 = await registryManager
                    .AddDeviceAsync(
                        new Device(edgeId2)
                        {
                            Authentication = new AuthenticationMechanism { Type = AuthenticationType.Sas },
                            Capabilities = new Shared.DeviceCapabilities { IotEdge = true },
                            ParentScopes = { edge1.Scope },
                        })
                    .ConfigureAwait(false);

                Device device = await registryManager
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
                            Content =
                            {
                                DeviceContent = { { "properties.desired.x", 4L } },
                            },
                            Metrics =
                            {
                                Queries = { { "successfullyConfigured", "select deviceId from devices where properties.reported.x = 4" } }
                            },
                        })
                    .ConfigureAwait(false);

                // act

                JobProperties jobProperties = await CreateAndWaitForJobAsync(
                        storageAuthenticationType,
                        isUserAssignedMsi,
                        devicesFileName,
                        configsFileName,
                        registryManager,
                        containerUri)
                    .ConfigureAwait(false);

                // assert

                jobProperties.Status.Should().Be(JobStatus.Completed, $"Export failed due to '{jobProperties.FailureReason}'.");
                jobProperties.FailureReason.Should().BeNullOrEmpty("Otherwise export failed");

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
                await CleanUpDevicesAsync(edgeId1, edgeId2, deviceId, configurationId, registryManager).ConfigureAwait(false);
            }
        }

        private async Task<JobProperties> CreateAndWaitForJobAsync(
            StorageAuthenticationType storageAuthenticationType,
            bool isUserAssignedMsi,
            string devicesFileName,
            string configsFileName,
            RegistryManager registryManager,
            Uri containerUri)
        {
            ManagedIdentity identity = isUserAssignedMsi
                ? new ManagedIdentity
                {
                    UserAssignedIdentity = TestConfiguration.IoTHub.UserAssignedMsiResourceId
                }
                : null;

            JobProperties jobProperties = JobProperties.CreateForExportJob(
                containerUri.ToString(),
                true,
                devicesFileName,
                storageAuthenticationType,
                identity);
            jobProperties.IncludeConfigurations = true;
            jobProperties.ConfigurationsBlobName = configsFileName;

            var sw = Stopwatch.StartNew();

            while (!jobProperties.IsFinished)
            {
                try
                {
                    jobProperties = await registryManager.ExportDevicesAsync(jobProperties).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(jobProperties.FailureReason))
                    {
                        Logger.Trace($"Job failed due to {jobProperties.FailureReason}");
                    }
                    break;
                }
                // Concurrent jobs can be rejected, so implement a retry mechanism to handle conflicts with other tests
                catch (JobQuotaExceededException)
                {
                    Logger.Trace($"JobQuotaExceededException... waiting after {sw.Elapsed}.");
                    await Task.Delay(s_waitDuration).ConfigureAwait(false);
                    continue;
                }
            }

            sw.Stop();
            Logger.Trace($"Job started after {sw.Elapsed}.");

            sw.Restart();

            while (!jobProperties.IsFinished)
            {
                await Task.Delay(s_waitDuration).ConfigureAwait(false);
                jobProperties = await registryManager.GetJobAsync(jobProperties.JobId).ConfigureAwait(false);
                Logger.Trace($"Job {jobProperties.JobId} is {jobProperties.Status} with progress {jobProperties.Progress}% after {sw.Elapsed}.");
            }

            return jobProperties;
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
            Storage.Blob.CloudBlockBlob exportFile = storageContainer.CloudBlobContainer.GetBlockBlobReference(fileName);
            return await exportFile.DownloadTextAsync().ConfigureAwait(false);
        }

        private async Task CleanUpDevicesAsync(
            string edgeId1,
            string edgeId2,
            string deviceId,
            string configurationId,
            RegistryManager registryManager)
        {
            try
            {
                await registryManager.RemoveDeviceAsync(deviceId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Trace($"Failed to remove device during cleanup due to {ex.Message}");
            }

            try
            {
                await registryManager.RemoveDeviceAsync(edgeId2).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Trace($"Failed to remove device during cleanup due to {ex.Message}");
            }

            try
            {
                await registryManager.RemoveDeviceAsync(edgeId1).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Trace($"Failed to remove device during cleanup due to {ex.Message}");
            }

            try
            {
                await registryManager.RemoveConfigurationAsync(configurationId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Trace($"Failed to remove config during cleanup due to {ex.Message}");
            }
        }
    }
}
