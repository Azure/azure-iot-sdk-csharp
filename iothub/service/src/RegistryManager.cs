// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains methods that services can use to perform create, remove, update and delete operations on devices.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "Cannot change parameter names as it is considered a breaking change.")]
    public abstract class RegistryManager : IDisposable
    {
        /// <summary>
        /// Creates a RegistryManager from the IoT Hub connection string.
        /// </summary>
        /// <param name="connectionString">The IoT Hub connection string.</param>
        /// <returns>An RegistryManager instance.</returns>
        public static RegistryManager CreateFromConnectionString(string connectionString)
        {
            return CreateFromConnectionString(connectionString, new HttpTransportSettings());
        }

        /// <summary>
        /// Creates a RegistryManager from the IoT Hub connection string and transport settings
        /// </summary>
        /// <param name="connectionString">The IoT Hub connection string.</param>
        /// <param name="transportSettings">The HTTP transport settings.</param>
        /// <returns>An RegistryManager instance.</returns>
        public static RegistryManager CreateFromConnectionString(string connectionString, HttpTransportSettings transportSettings)
        {
            if (transportSettings == null)
            {
                throw new ArgumentNullException(nameof(transportSettings), "The HTTP transport settings cannot be null.");
            }

            JsonConvert.DefaultSettings = JsonSerializerSettingsInitializer.GetDefaultJsonSerializerSettings();
            TlsVersions.Instance.SetLegacyAcceptableVersions();

            var iotHubConnectionString = IotHubConnectionString.Parse(connectionString);
            return new HttpRegistryManager(iotHubConnectionString, transportSettings);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        { }

        /// <summary>
        /// Explicitly open the RegistryManager instance.
        /// </summary>
        public abstract Task OpenAsync();

        /// <summary>
        /// Closes the RegistryManager instance and disposes its resources.
        /// </summary>
        public abstract Task CloseAsync();

        /// <summary>
        /// Register a new device with the system
        /// </summary>
        /// <param name="device">The Device object being registered.</param>
        /// <returns>The Device object with the generated keys and ETags.</returns>
        public abstract Task<Device> AddDeviceAsync(Device device);

        /// <summary>
        /// Register a new device with the system
        /// </summary>
        /// <param name="device">The Device object being registered.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Device object with the generated keys and ETags.</returns>
        public abstract Task<Device> AddDeviceAsync(Device device, CancellationToken cancellationToken);

        /// <summary>
        /// Register a new module with device in the system
        /// </summary>
        /// <param name="module">The Module object being registered.</param>
        /// <returns>The Module object with the generated keys and ETags.</returns>
        public abstract Task<Module> AddModuleAsync(Module module);

        /// <summary>
        /// Register a new module with device in the system
        /// </summary>
        /// <param name="module">The Module object being registered.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Module object with the generated keys and ETags.</returns>
        public abstract Task<Module> AddModuleAsync(Module module, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a Device with Twin information
        /// </summary>
        /// <param name="device">The device to add.</param>
        /// <param name="twin">The twin information for the device being added.</param>
        /// <returns>The result of the add operation.</returns>
        public abstract Task<BulkRegistryOperationResult> AddDeviceWithTwinAsync(Device device, Twin twin);

        /// <summary>
        /// Adds a Device with Twin information
        /// </summary>
        /// <param name="device">The device to add.</param>
        /// <param name="twin">The twin information for the device being added.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The result of the add operation.</returns>
        public abstract Task<BulkRegistryOperationResult> AddDeviceWithTwinAsync(Device device, Twin twin, CancellationToken cancellationToken);

        /// <summary>
        /// Register a list of new devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being registered.</param>
        /// <returns>Returns a string array of error messages.</returns>
        [Obsolete("Use AddDevices2Async")]
        public abstract Task<string[]> AddDevicesAsync(IEnumerable<Device> devices);

        /// <summary>
        /// Register a list of new devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being registered.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>Returns a string array of error messages.</returns>
        [Obsolete("Use AddDevices2Async")]
        public abstract Task<string[]> AddDevicesAsync(IEnumerable<Device> devices, CancellationToken cancellationToken);

        /// <summary>
        /// Register a list of new devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being registered.</param>
        /// <returns>Returns a BulkRegistryOperationResult object.</returns>
        public abstract Task<BulkRegistryOperationResult> AddDevices2Async(IEnumerable<Device> devices);

        /// <summary>
        /// Register a list of new devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being registered.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>Returns a BulkRegistryOperationResult object.</returns>
        public abstract Task<BulkRegistryOperationResult> AddDevices2Async(IEnumerable<Device> devices, CancellationToken cancellationToken);

        /// <summary>
        /// Update the mutable fields of the device registration
        /// </summary>
        /// <param name="device">The Device object with updated fields.</param>
        /// <returns>The Device object with updated ETag.</returns>
        public abstract Task<Device> UpdateDeviceAsync(Device device);

        /// <summary>
        /// Update the mutable fields of the device registration
        /// </summary>
        /// <param name="device">The Device object with updated fields.</param>
        /// <param name="forceUpdate">Forces the device object to be replaced without regard for an ETag match.</param>
        /// <returns>The Device object with updated ETag.</returns>
        public abstract Task<Device> UpdateDeviceAsync(Device device, bool forceUpdate);

        /// <summary>
        /// Update the mutable fields of the device registration
        /// </summary>
        /// <param name="device">The Device object with updated fields.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Device object with updated ETag.</returns>
        public abstract Task<Device> UpdateDeviceAsync(Device device, CancellationToken cancellationToken);

        /// <summary>
        /// Update the mutable fields of the device registration
        /// </summary>
        /// <param name="device">The Device object with updated fields.</param>
        /// <param name="forceUpdate">Forces the device object to be replaced even if it was updated since it was retrieved last time.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Device object with updated ETags.</returns>
        public abstract Task<Device> UpdateDeviceAsync(Device device, bool forceUpdate, CancellationToken cancellationToken);

        /// <summary>
        /// Update the mutable fields of the module registration
        /// </summary>
        /// <param name="module">The Module object with updated fields.</param>
        /// <returns>The Module object with updated ETags.</returns>
        public abstract Task<Module> UpdateModuleAsync(Module module);

        /// <summary>
        /// Update the mutable fields of the module registration
        /// </summary>
        /// <param name="module">The Module object with updated fields.</param>
        /// <param name="forceUpdate">Forces the device object to be replaced without regard for an ETag match.</param>
        /// <returns>The Module object with updated ETags.</returns>
        public abstract Task<Module> UpdateModuleAsync(Module module, bool forceUpdate);

        /// <summary>
        /// Update the mutable fields of the module registration
        /// </summary>
        /// <param name="module">The Module object with updated fields.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Module object with updated ETags.</returns>
        public abstract Task<Module> UpdateModuleAsync(Module module, CancellationToken cancellationToken);

        /// <summary>
        /// Update the mutable fields of the module registration
        /// </summary>
        /// <param name="module">The Module object with updated fields.</param>
        /// <param name="forceUpdate">Forces the module object to be replaced even if it was updated since it was retrieved last time.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Module object with updated ETags.</returns>
        public abstract Task<Module> UpdateModuleAsync(Module module, bool forceUpdate, CancellationToken cancellationToken);

        /// <summary>
        /// Update a list of devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being updated.</param>
        /// <returns>Returns a string array of error messages.</returns>
        [Obsolete("Use UpdateDevices2Async")]
        public abstract Task<string[]> UpdateDevicesAsync(IEnumerable<Device> devices);

        /// <summary>
        /// Update a list of devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being updated.</param>
        /// <param name="forceUpdate">Forces the device object to be replaced even if it was updated since it was retrieved last time.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>Returns a string array of error messages.</returns>
        [Obsolete("Use UpdateDevices2Async")]
        public abstract Task<string[]> UpdateDevicesAsync(IEnumerable<Device> devices, bool forceUpdate, CancellationToken cancellationToken);

        /// <summary>
        /// Update a list of devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being updated.</param>
        /// <returns>Returns a BulkRegistryOperationResult object.</returns>
        public abstract Task<BulkRegistryOperationResult> UpdateDevices2Async(IEnumerable<Device> devices);

        /// <summary>
        /// Update a list of devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being updated.</param>
        /// <param name="forceUpdate">Forces the device object to be replaced even if it was updated since it was retrieved last time.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>Returns a BulkRegistryOperationResult object.</returns>
        public abstract Task<BulkRegistryOperationResult> UpdateDevices2Async(IEnumerable<Device> devices, bool forceUpdate, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="deviceId">The id of the device being deleted.</param>
        public abstract Task RemoveDeviceAsync(string deviceId);

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="deviceId">The id of the device being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public abstract Task RemoveDeviceAsync(string deviceId, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="device">The device being deleted.</param>
        public abstract Task RemoveDeviceAsync(Device device);

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="device">The device being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public abstract Task RemoveDeviceAsync(Device device, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a previously registered module from device in the system.
        /// </summary>
        /// <param name="deviceId">The id of the device being deleted.</param>
        /// <param name="moduleId">The id of the moduleId being deleted.</param>
        public abstract Task RemoveModuleAsync(string deviceId, string moduleId);

        /// <summary>
        /// Deletes a previously registered module from device in the system.
        /// </summary>
        /// <param name="deviceId">The id of the device being deleted.</param>
        /// <param name="moduleId">The id of the moduleId being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public abstract Task RemoveModuleAsync(string deviceId, string moduleId, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a previously registered module from device in the system.
        /// </summary>
        /// <param name="module">The module being deleted.</param>
        public abstract Task RemoveModuleAsync(Module module);

        /// <summary>
        /// Deletes a previously registered module from device in the system.
        /// </summary>
        /// <param name="module">The module being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public abstract Task RemoveModuleAsync(Module module, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a list of previously registered devices from the system.
        /// </summary>
        /// <param name="devices">The devices being deleted.</param>
        [Obsolete("Use RemoveDevices2Async")]
        public abstract Task<string[]> RemoveDevicesAsync(IEnumerable<Device> devices);

        /// <summary>
        /// Deletes a list of previously registered devices from the system.
        /// </summary>
        /// <param name="devices">The devices being deleted.</param>
        /// <param name="forceRemove">Forces the device object to be removed without regard for an ETag match.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        [Obsolete("Use RemoveDevices2Async")]
        public abstract Task<string[]> RemoveDevicesAsync(IEnumerable<Device> devices, bool forceRemove, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a list of previously registered devices from the system.
        /// </summary>
        /// <param name="devices">The devices being deleted.</param>
        /// <returns>Returns a BulkRegistryOperationResult object.</returns>
        public abstract Task<BulkRegistryOperationResult> RemoveDevices2Async(IEnumerable<Device> devices);

        /// <summary>
        /// Deletes a list of previously registered devices from the system.
        /// </summary>
        /// <param name="devices">The devices being deleted.</param>
        /// <param name="forceRemove">Forces the device object to be removed even if it was updated since it was retrieved last time.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>Returns a BulkRegistryOperationResult object.</returns>
        public abstract Task<BulkRegistryOperationResult> RemoveDevices2Async(IEnumerable<Device> devices, bool forceRemove, CancellationToken cancellationToken);

        /// <summary>
        /// Gets usage statistics for the IoT Hub.
        /// </summary>
        public abstract Task<RegistryStatistics> GetRegistryStatisticsAsync();

        /// <summary>
        /// Gets usage statistics for the IoT Hub.
        /// </summary>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public abstract Task<RegistryStatistics> GetRegistryStatisticsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the specified Device object.
        /// </summary>
        /// <param name="deviceId">The id of the device being retrieved.</param>
        /// <returns>The Device object.</returns>
        public abstract Task<Device> GetDeviceAsync(string deviceId);

        /// <summary>
        /// Retrieves the specified Device object.
        /// </summary>
        /// <param name="deviceId">The id of the device being retrieved.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Device object.</returns>
        public abstract Task<Device> GetDeviceAsync(string deviceId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the specified Module object.
        /// </summary>
        /// <param name="deviceId">The id of the device being retrieved.</param>
        /// <param name="moduleId">The id of the module being retrieved.</param>
        /// <returns>The Module object.</returns>
        public abstract Task<Module> GetModuleAsync(string deviceId, string moduleId);

        /// <summary>
        /// Retrieves the specified Module object.
        /// </summary>
        /// <param name="deviceId">The id of the device being retrieved.</param>
        /// <param name="moduleId">The id of the module being retrieved.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Module object.</returns>
        public abstract Task<Module> GetModuleAsync(string deviceId, string moduleId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the module identities on device
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <returns>List of modules on device.</returns>
        public abstract Task<IEnumerable<Module>> GetModulesOnDeviceAsync(string deviceId);

        /// <summary>
        /// Retrieves the module identities on device
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>List of modules on device.</returns>
        public abstract Task<IEnumerable<Module>> GetModulesOnDeviceAsync(string deviceId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves specified number of devices from every IoT Hub partition.
        /// This is an approximation and not a definitive list. Results are not ordered.
        /// </summary>
        /// <returns>The list of devices.</returns>
        [Obsolete("Use CreateQuery(\"select * from devices\", pageSize);")]
        public abstract Task<IEnumerable<Device>> GetDevicesAsync(int maxCount);

        /// <summary>
        /// Retrieves specified number of devices from every IoT hub partition.
        /// This is an approximation and not a definitive list. Results are not ordered.
        /// </summary>
        /// <returns>The list of devices.</returns>
        [Obsolete("Use CreateQuery(\"select * from devices\", pageSize);")]
        public abstract Task<IEnumerable<Device>> GetDevicesAsync(int maxCount, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a handle through which a result for a given query can be fetched.
        /// </summary>
        /// <param name="sqlQueryString">The SQL query.</param>
        /// <returns>A handle used to fetch results for a SQL query.</returns>
        public abstract IQuery CreateQuery(string sqlQueryString);

        /// <summary>
        /// Retrieves a handle through which a result for a given query can be fetched.
        /// </summary>
        /// <param name="sqlQueryString">The SQL query.</param>
        /// <param name="pageSize">The maximum number of items per page.</param>
        /// <returns>A handle used to fetch results for a SQL query.</returns>
        public abstract IQuery CreateQuery(string sqlQueryString, int? pageSize);

        /// <summary>
        /// Copies registered device data to a set of blobs in a specific container in a storage account.
        /// </summary>
        /// <param name="storageAccountConnectionString">ConnectionString to the destination StorageAccount.</param>
        /// <param name="containerName">Destination blob container name.</param>
        public abstract Task ExportRegistryAsync(string storageAccountConnectionString, string containerName);

        /// <summary>
        /// Copies registered device data to a set of blobs in a specific container in a storage account.
        /// </summary>
        /// <param name="storageAccountConnectionString">ConnectionString to the destination StorageAccount.</param>
        /// <param name="containerName">Destination blob container name.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        public abstract Task ExportRegistryAsync(string storageAccountConnectionString, string containerName, CancellationToken cancellationToken);

        /// <summary>
        /// Imports registered device data from a set of blobs in a specific container in a storage account.
        /// </summary>
        /// <param name="storageAccountConnectionString">ConnectionString to the source StorageAccount.</param>
        /// <param name="containerName">Source blob container name.</param>
        public abstract Task ImportRegistryAsync(string storageAccountConnectionString, string containerName);

        /// <summary>
        /// Imports registered device data from a set of blobs in a specific container in a storage account.
        /// </summary>
        /// <param name="storageAccountConnectionString">ConnectionString to the source StorageAccount.</param>
        /// <param name="containerName">Source blob container name.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        public abstract Task ImportRegistryAsync(string storageAccountConnectionString, string containerName, CancellationToken cancellationToken);

#pragma warning disable CA1054 // Uri parameters should not be strings

        /// <summary>
        /// Creates a new bulk job to export device registrations to the container specified by the provided URI.
        /// </summary>
        /// <param name="exportBlobContainerUri">Destination blob container URI.</param>
        /// <param name="excludeKeys">Specifies whether to exclude the Device's Keys during the export.</param>
        /// <returns>JobProperties of the newly created job.</returns>

        public abstract Task<JobProperties> ExportDevicesAsync(string exportBlobContainerUri, bool excludeKeys);

        /// <summary>
        /// Creates a new bulk job to export device registrations to the container specified by the provided URI.
        /// </summary>
        /// <param name="exportBlobContainerUri">Destination blob container URI.</param>
        /// <param name="excludeKeys">Specifies whether to exclude the Device's Keys during the export.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public abstract Task<JobProperties> ExportDevicesAsync(string exportBlobContainerUri, bool excludeKeys, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new bulk job to export device registrations to the container specified by the provided URI.
        /// </summary>
        /// <param name="exportBlobContainerUri">Destination blob container URI.</param>
        /// <param name="outputBlobName">The name of the blob that will be created in the provided output blob container.</param>
        /// <param name="excludeKeys">Specifies whether to exclude the Device's Keys during the export.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public abstract Task<JobProperties> ExportDevicesAsync(string exportBlobContainerUri, string outputBlobName, bool excludeKeys);

        /// <summary>
        /// Creates a new bulk job to export device registrations to the container specified by the provided URI.
        /// </summary>
        /// <param name="exportBlobContainerUri">Destination blob container URI.</param>
        /// <param name="outputBlobName">The name of the blob that will be created in the provided output blob container.</param>
        /// <param name="excludeKeys">Specifies whether to exclude the Device's Keys during the export.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public abstract Task<JobProperties> ExportDevicesAsync(string exportBlobContainerUri, string outputBlobName, bool excludeKeys, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new bulk job to export device registrations to the container specified by the provided URI.
        /// </summary>
        /// <param name="jobParameters">Parameters for the job.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public abstract Task<JobProperties> ExportDevicesAsync(JobProperties jobParameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new bulk job to import device registrations into the IoT Hub.
        /// </summary>
        /// <param name="importBlobContainerUri">Source blob container URI.</param>
        /// <param name="outputBlobContainerUri">Destination blob container URI.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public abstract Task<JobProperties> ImportDevicesAsync(string importBlobContainerUri, string outputBlobContainerUri);

        /// <summary>
        /// Creates a new bulk job to import device registrations into the IoT Hub.
        /// </summary>
        /// <param name="importBlobContainerUri">Source blob container URI.</param>
        /// <param name="outputBlobContainerUri">Destination blob container URI.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public abstract Task<JobProperties> ImportDevicesAsync(string importBlobContainerUri, string outputBlobContainerUri, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new bulk job to import device registrations into the IoT Hub.
        /// </summary>
        /// <param name="importBlobContainerUri">Source blob container URI.</param>
        /// <param name="outputBlobContainerUri">Destination blob container URI.</param>
        /// <param name="inputBlobName">The blob name to be used when importing from the provided input blob container.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public abstract Task<JobProperties> ImportDevicesAsync(string importBlobContainerUri, string outputBlobContainerUri, string inputBlobName);

        /// <summary>
        /// Creates a new bulk job to import device registrations into the IoT Hub.
        /// </summary>
        /// <param name="importBlobContainerUri">Source blob container URI.</param>
        /// <param name="outputBlobContainerUri">Destination blob container URI.</param>
        /// <param name="inputBlobName">The blob name to be used when importing from the provided input blob container.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public abstract Task<JobProperties> ImportDevicesAsync(string importBlobContainerUri, string outputBlobContainerUri, string inputBlobName, CancellationToken cancellationToken);

#pragma warning restore CA1054 // Uri parameters should not be strings

        /// <summary>
        /// Creates a new bulk job to import device registrations into the IoT Hub.
        /// </summary>
        /// <param name="jobParameters">Parameters for the job.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public abstract Task<JobProperties> ImportDevicesAsync(JobProperties jobParameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the Job object to retrieve.</param>
        /// <returns>JobProperties of the job specified by the provided jobId.</returns>
        public abstract Task<JobProperties> GetJobAsync(string jobId);

        /// <summary>
        /// Gets the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the Job object to retrieve.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>JobProperties of the job specified by the provided jobId.</returns>
        public abstract Task<JobProperties> GetJobAsync(string jobId, CancellationToken cancellationToken);

        /// <summary>
        /// List all jobs for the IoT Hub.
        /// </summary>
        /// <returns>IEnumerable of JobProperties of all jobs for this IoT Hub.</returns>
        public abstract Task<IEnumerable<JobProperties>> GetJobsAsync();

        /// <summary>
        /// List all jobs for the IoT Hub.
        /// </summary>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>IEnumerable of JobProperties of all jobs for this IoT Hub.</returns>
        public abstract Task<IEnumerable<JobProperties>> GetJobsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Cancels/Deletes the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to cancel.</param>
        public abstract Task CancelJobAsync(string jobId);

        /// <summary>
        /// Cancels/Deletes the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to cancel.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        public abstract Task CancelJobAsync(string jobId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets <see cref="Twin"/> from IotHub
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <returns>Twin instance.</returns>
        public abstract Task<Twin> GetTwinAsync(string deviceId);

        /// <summary>
        /// Gets <see cref="Twin"/> from IotHub
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Twin instance.</returns>
        public abstract Task<Twin> GetTwinAsync(string deviceId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets Module's <see cref="Twin"/> from IotHub
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <returns>Twin instance.</returns>
        public abstract Task<Twin> GetTwinAsync(string deviceId, string moduleId);

        /// <summary>
        /// Gets Module's <see cref="Twin"/> from IotHub
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Twin instance.</returns>
        public abstract Task<Twin> GetTwinAsync(string deviceId, string moduleId, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> UpdateTwinAsync(string deviceId, Twin twinPatch, string etag);

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> UpdateTwinAsync(string deviceId, Twin twinPatch, string etag, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="jsonTwinPatch">Twin json with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> UpdateTwinAsync(string deviceId, string jsonTwinPatch, string etag);

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="jsonTwinPatch">Twin json with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> UpdateTwinAsync(string deviceId, string jsonTwinPatch, string etag, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> UpdateTwinAsync(string deviceId, string moduleId, Twin twinPatch, string etag);

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> UpdateTwinAsync(string deviceId, string moduleId, Twin twinPatch, string etag, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="jsonTwinPatch">Twin json with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> UpdateTwinAsync(string deviceId, string moduleId, string jsonTwinPatch, string etag);

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="jsonTwinPatch">Twin json with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> UpdateTwinAsync(string deviceId, string moduleId, string jsonTwinPatch, string etag, CancellationToken cancellationToken);

        /// <summary>
        /// Update the mutable fields for a list of <see cref="Twin"/>s previously created within the system
        /// </summary>
        /// <param name="twins">List of <see cref="Twin"/>s with updated fields.</param>
        /// <returns>Result of the bulk update operation.</returns>
        public abstract Task<BulkRegistryOperationResult> UpdateTwins2Async(IEnumerable<Twin> twins);

        /// <summary>
        /// Update the mutable fields for a list of <see cref="Twin"/>s previously created within the system
        /// </summary>
        /// <param name="twins">List of <see cref="Twin"/>s with updated fields.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Result of the bulk update operation.</returns>
        public abstract Task<BulkRegistryOperationResult> UpdateTwins2Async(IEnumerable<Twin> twins, CancellationToken cancellationToken);

        /// <summary>
        /// Update the mutable fields for a list of <see cref="Twin"/>s previously created within the system
        /// </summary>
        /// <param name="twins">List of <see cref="Twin"/>s with updated fields.</param>
        /// <param name="forceUpdate">Forces the <see cref="Twin"/> object to be updated even if it has changed since it was retrieved last time.</param>
        /// <returns>Result of the bulk update operation.</returns>
        public abstract Task<BulkRegistryOperationResult> UpdateTwins2Async(IEnumerable<Twin> twins, bool forceUpdate);

        /// <summary>
        /// Update the mutable fields for a list of <see cref="Twin"/>s previously created within the system
        /// </summary>
        /// <param name="twins">List of <see cref="Twin"/>s with updated fields.</param>
        /// <param name="forceUpdate">Forces the <see cref="Twin"/> object to be updated even if it has changed since it was retrieved last time.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Result of the bulk update operation.</returns>
        public abstract Task<BulkRegistryOperationResult> UpdateTwins2Async(IEnumerable<Twin> twins, bool forceUpdate, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="newTwin">New Twin object to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> ReplaceTwinAsync(string deviceId, Twin newTwin, string etag);

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="newTwin">New Twin object to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> ReplaceTwinAsync(string deviceId, Twin newTwin, string etag, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="newTwinJson">New Twin json to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> ReplaceTwinAsync(string deviceId, string newTwinJson, string etag);

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="newTwinJson">New Twin json to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> ReplaceTwinAsync(string deviceId, string newTwinJson, string etag, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="newTwin">New Twin object to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> ReplaceTwinAsync(string deviceId, string moduleId, Twin newTwin, string etag);

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="newTwin">New Twin object to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> ReplaceTwinAsync(string deviceId, string moduleId, Twin newTwin, string etag, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="newTwinJson">New Twin json to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> ReplaceTwinAsync(string deviceId, string moduleId, string newTwinJson, string etag);

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="newTwinJson">New Twin json to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public abstract Task<Twin> ReplaceTwinAsync(string deviceId, string moduleId, string newTwinJson, string etag, CancellationToken cancellationToken);

        /// <summary>
        /// Register a new Configuration for Azure IOT Edge in IotHub
        /// </summary>
        /// <param name="configuration">The Configuration object being registered.</param>
        /// <returns>The Configuration object.</returns>
        public abstract Task<Configuration> AddConfigurationAsync(Configuration configuration);

        /// <summary>
        /// Register a new Configuration for Azure IOT Edge in IotHub
        /// </summary>
        /// <param name="configuration">The Configuration object being registered.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object.</returns>
        public abstract Task<Configuration> AddConfigurationAsync(Configuration configuration, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the specified Configuration object.
        /// </summary>
        /// <param name="configurationId">The id of the Configuration being retrieved.</param>
        /// <returns>The Configuration object.</returns>
        public abstract Task<Configuration> GetConfigurationAsync(string configurationId);

        /// <summary>
        /// Retrieves the specified Configuration object.
        /// </summary>
        /// <param name="configurationId">The id of the Configuration being retrieved.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object.</returns>
        public abstract Task<Configuration> GetConfigurationAsync(string configurationId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves specified number of configurations from every IoT Hub partition.
        /// Results are not ordered.
        /// </summary>
        /// <returns>The list of configurations.</returns>
        public abstract Task<IEnumerable<Configuration>> GetConfigurationsAsync(int maxCount);

        /// <summary>
        /// Retrieves specified number of configurations from every IoT hub partition.
        /// Results are not ordered.
        /// </summary>
        /// <returns>The list of configurations.</returns>
        public abstract Task<IEnumerable<Configuration>> GetConfigurationsAsync(int maxCount, CancellationToken cancellationToken);

        /// <summary>
        /// Update the mutable fields of the Configuration registration
        /// </summary>
        /// <param name="configuration">The Configuration object with updated fields.</param>
        /// <returns>The Configuration object with updated ETag.</returns>
        public abstract Task<Configuration> UpdateConfigurationAsync(Configuration configuration);

        /// <summary>
        /// Update the mutable fields of the Configuration registration
        /// </summary>
        /// <param name="configuration">The Configuration object with updated fields.</param>
        /// <param name="forceUpdate">Forces the device object to be replaced without regard for an ETag match.</param>
        /// <returns>The Configuration object with updated ETags.</returns>
        public abstract Task<Configuration> UpdateConfigurationAsync(Configuration configuration, bool forceUpdate);

        /// <summary>
        /// Update the mutable fields of the Configuration registration
        /// </summary>
        /// <param name="configuration">The Configuration object with updated fields.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object with updated ETags.</returns>
        public abstract Task<Configuration> UpdateConfigurationAsync(Configuration configuration, CancellationToken cancellationToken);

        /// <summary>
        /// Update the mutable fields of the Configuration registration
        /// </summary>
        /// <param name="configuration">The Configuration object with updated fields.</param>
        /// <param name="forceUpdate">Forces the Configuration object to be replaced even if it was updated since it was retrieved last time.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object with updated ETags.</returns>
        public abstract Task<Configuration> UpdateConfigurationAsync(Configuration configuration, bool forceUpdate, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="configurationId">The id of the Configuration being deleted.</param>
        public abstract Task RemoveConfigurationAsync(string configurationId);

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="configurationId">The id of the configurationId being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public abstract Task RemoveConfigurationAsync(string configurationId, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="configuration">The Configuration being deleted.</param>
        public abstract Task RemoveConfigurationAsync(Configuration configuration);

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="configuration">The Configuration being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public abstract Task RemoveConfigurationAsync(Configuration configuration, CancellationToken cancellationToken);

        /// <summary>
        /// Applies configuration content to an IoTEdge device.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="content"><see cref="ConfigurationContent"/>The configuration of an IoTEdge device.</param>
        public abstract Task ApplyConfigurationContentOnDeviceAsync(string deviceId, ConfigurationContent content);

        /// <summary>
        /// Applies configuration content to an IoTEdge device.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="content"><see cref="ConfigurationContent"/>The configuration of an IoTEdge device.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public abstract Task ApplyConfigurationContentOnDeviceAsync(string deviceId, ConfigurationContent content, CancellationToken cancellationToken);
    }
}
