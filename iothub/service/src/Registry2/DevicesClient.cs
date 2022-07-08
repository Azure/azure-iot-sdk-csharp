// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Http2;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="ServiceClient2"/> that handles all device registry operations including
    /// getting/adding/setting/deleting device identities, getting modules on a device, and getting device
    /// registry statistics.
    /// </summary>
    public class DevicesClient
    {
        private string _hostName;
        private IotHubConnectionProperties _credentialProvider;
        private HttpClient _httpClient;
        private HttpRequestMessageFactory _httpRequestMessageFactory;

        private const string DeviceRequestUriFormat = "/devices/{0}";
        private const string ModulesOnDeviceRequestUriFormat = "/devices/{0}/modules";
        private const string JobsGetUriFormat = "/jobs/{0}";
        private const string JobsListUriFormat = "/jobs";
        private const string JobsCreateUriFormat = "/jobs/create";
        private const string StatisticsUriFormat = "/statistics/devices";
        private const string AdminUriFormat = "/$admin/{0}";

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected DevicesClient()
        {
        }

        internal DevicesClient(string hostName, IotHubConnectionProperties credentialProvider, HttpClient httpClient, HttpRequestMessageFactory httpRequestMessageFactory)
        {
            _credentialProvider = credentialProvider;
            _hostName = hostName;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
        }

        /// <summary>
        /// Add a device identity to your IoT hub's registry.
        /// </summary>
        /// <param name="device">The device identity to register.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The registered device with the generated keys and ETags.</returns>
        public virtual async Task<Device> AddAsync(Device device, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Adding device: {device?.Id}", nameof(AddAsync));

            try
            {
                Argument.RequireNotNull(device, nameof(device));

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetRequestUri(device.Id), _credentialProvider, device);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<Device>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(AddAsync)} threw an exception: {ex}", nameof(AddAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Adding device: {device?.Id}", nameof(AddAsync));
            }
        }

        /// <summary>
        /// Get a device identity by its Id.
        /// </summary>
        /// <param name="deviceId">The unique identifier of the device identity to retrieve.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The retrieved device identity.</returns>
        public virtual async Task<Device> GetAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting device: {deviceId}", nameof(GetAsync));

            try
            {
                Argument.RequireNotNullOrEmpty(deviceId, nameof(deviceId));

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetRequestUri(deviceId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<Device>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(GetAsync)} threw an exception: {ex}", nameof(GetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting device: {deviceId}", nameof(GetAsync));
            }
        }

        /// <summary>
        /// Replace a device identity's state with the provided device identity's state.
        /// </summary>
        /// <param name="device">The device identity's new state.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The newly updated device identity including its new ETag.</returns>
        public virtual async Task<Device> SetAsync(Device device, CancellationToken cancellationToken = default)
        {
            return await SetAsync(device, false, cancellationToken);
        }

        /// <summary>
        /// Replace a device identity's state with the provided device identity's state.
        /// </summary>
        /// <param name="device">The device identity's new state.</param>
        /// <param name="forceUpdate">
        /// If true, this update operation will execute even if the provided device identity has
        /// an out of date ETag. If false, the operation will throw a <see cref="PreconditionFailedException"/>
        /// if the provided device identity has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The newly updated device identity including its new ETag.</returns>
        public virtual async Task<Device> SetAsync(Device device, bool forceUpdate, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating device: {device?.Id}", nameof(SetAsync));

            try
            {
                Argument.RequireNotNull(device, nameof(device));

                if (string.IsNullOrWhiteSpace(device.ETag) && !forceUpdate)
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingDevice);
                }

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetRequestUri(device.Id), _credentialProvider);
                HttpMessageHelper2.InsertEtag(request, device.ETag);

                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<Device>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(SetAsync)} threw an exception: {ex}", nameof(SetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Updating device: {device?.Id}", nameof(SetAsync));
            }
        }

        /// <summary>
        /// Delete the device identity with the provided Id from your IoT hub's registry.
        /// </summary>
        /// <param name="deviceId">The Id of the device identity to be deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual async Task DeleteAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            Argument.RequireNotNullOrEmpty(deviceId, nameof(deviceId));

            var device = new Device(deviceId);
            device.ETag = HttpMessageHelper2.ETagForce;
            await DeleteAsync(device, cancellationToken);
        }

        /// <summary>
        /// Delete the device identity with the provided Id from your IoT hub's registry.
        /// </summary>
        /// <param name="device">
        /// The device identity to delete from your IoT hub's registry. If the provided device's ETag
        /// is out of date, this operation will throw a <see cref="PreconditionFailedException"/>
        /// An up-to-date ETag can be retrieved using <see cref="GetAsync(string, CancellationToken)"/>.
        /// To force the operation to execute regardless of ETag, set the device identity's ETag to "*" or
        /// use <see cref="DeleteAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual async Task DeleteAsync(Device device, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Deleting device: {device?.Id}", nameof(DeleteAsync));

            try
            {
                Argument.RequireNotNull(device, nameof(device));

                if (device.ETag == null)
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingDevice);
                }

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Delete, GetRequestUri(device.Id), _credentialProvider);
                HttpMessageHelper2.InsertEtag(request, device.ETag);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.NoContent, response);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(DeleteAsync)} threw an exception: {ex}", nameof(DeleteAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Deleting device: {device?.Id}", nameof(DeleteAsync));
            }
        }

        /// <summary>
        /// Add a device identity to your IoT hub's registry with an initial twin state.
        /// </summary>
        /// <remarks>
        /// This API uses the same underlying service API as the bulk add/update/delete APIs defined in
        /// this client.
        /// </remarks>
        /// <param name="device">The device identity to register.</param>
        /// <param name="twin">The initial twin state for the device.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The result of the bulk operation.</returns>
        public virtual async Task<BulkRegistryOperationResult> AddWithTwinAsync(Device device, Twin twin, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Adding device with twin: {device?.Id}", nameof(AddWithTwinAsync));

            try
            {
                var exportImportDeviceList = new List<ExportImportDevice>(1);

                var exportImportDevice = new ExportImportDevice(device, ImportMode.Create)
                {
                    Tags = twin?.Tags,
                    Properties = new ExportImportDevice.PropertyContainer
                    {
                        DesiredProperties = twin?.Properties.Desired,
                        ReportedProperties = twin?.Properties.Reported,
                    }
                };

                exportImportDeviceList.Add(exportImportDevice);

                return await BulkDeviceOperationAsync(exportImportDeviceList, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(AddWithTwinAsync)} threw an exception: {ex}", nameof(AddWithTwinAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Adding device with twin: {device?.Id}", nameof(AddWithTwinAsync));
            }
        }

        /// <summary>
        /// Get all the modules that are registered on a particular device.
        /// </summary>
        /// <param name="deviceId">The Id of the device to get the modules of.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The modules that are registered on the specified device.</returns>
        public virtual async Task<IEnumerable<Module>> GetModulesAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting modules on device: {deviceId}", nameof(GetModulesAsync));

            try
            {
                Argument.RequireNotNullOrEmpty(deviceId, nameof(deviceId));

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetModulesOnDeviceRequestUri(deviceId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<IEnumerable<Module>>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(GetModulesAsync)} threw an exception: {ex}", nameof(GetModulesAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting modules on device: {deviceId}", nameof(GetModulesAsync));
            }
        }

        /// <summary>
        /// Add up to 100 device identities to your IoT hub's registry in bulk.
        /// </summary>
        /// <remarks>
        /// For larger scale operations, consider using <see cref="ImportAsync(string, string, CancellationToken)"/>
        /// which allows you to import devices from an Azure Storage container.
        /// </remarks>
        /// <param name="devices">The device identities to add to your IoT hub's registry. May not exceed 100 devices.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The result of the bulk operation.</returns>
        public virtual async Task<BulkRegistryOperationResult> AddAsync(IEnumerable<Device> devices, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Adding {devices?.Count()} devices", nameof(AddAsync));

            try
            {
                IEnumerable<ExportImportDevice> exportImportDevices = GenerateExportImportDeviceListForBulkOperations(devices, ImportMode.Create);
                return await BulkDeviceOperationAsync(exportImportDevices, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(AddAsync)} threw an exception: {ex}", nameof(AddAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Adding {devices?.Count()} devices", nameof(AddAsync));
            }
        }

        /// <summary>
        /// Forceably update up to 100 device identities in your IoT hub's registry in bulk.
        /// </summary>
        /// <param name="devices">The device identities to update to your IoT hub's registry. May not exceed 100 devices.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The result of the bulk operation.</returns>
        public virtual async Task<BulkRegistryOperationResult> SetAsync(IEnumerable<Device> devices, CancellationToken cancellationToken = default)
        {
            return await SetAsync(devices, false, cancellationToken);
        }

        /// <summary>
        /// Update up to 100 device identities in your IoT hub's registry in bulk.
        /// </summary>
        /// <param name="devices">The device identities to update to your IoT hub's registry. May not exceed 100 devices.</param>
        /// <param name="forceUpdate">
        /// If true, device identities will be updated even if they have an out-of-date ETag. If false,
        /// only the devices that have an up-to-date ETag will be updated. An up-to-date ETag can be retrieved
        /// using <see cref="GetAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The result of the bulk operation.</returns>
        public virtual async Task<BulkRegistryOperationResult> SetAsync(IEnumerable<Device> devices, bool forceUpdate, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating multiple devices: count: {devices?.Count()} - Force update: {forceUpdate}", nameof(SetAsync));

            try
            {
                ImportMode importMode = forceUpdate ? ImportMode.Update : ImportMode.UpdateIfMatchETag;
                IEnumerable<ExportImportDevice> exportImportDevices = GenerateExportImportDeviceListForBulkOperations(devices, importMode);
                return await BulkDeviceOperationAsync(exportImportDevices, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(SetAsync)} threw an exception: {ex}", nameof(SetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Updating multiple devices: count: {devices?.Count()} - Force update: {forceUpdate}", nameof(SetAsync));
            }
        }

        /// <summary>
        /// Forceably delete up to 100 device identities from your IoT hub's registry in bulk.
        /// </summary>
        /// <param name="devices">The device identities to delete from your IoT hub's registry. May not exceed 100 devices.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The result of the bulk operation.</returns>
        public virtual async Task<BulkRegistryOperationResult> DeleteAsync(IEnumerable<Device> devices, CancellationToken cancellationToken = default)
        {
            return await DeleteAsync(devices, false, cancellationToken);
        }

        /// <summary>
        /// Delete up to 100 device identities from your IoT hub's registry in bulk.
        /// </summary>
        /// <param name="devices">The device identities to delete from your IoT hub's registry. May not exceed 100 devices.</param>
        /// <param name="forceDelete">
        /// If true, device identities will be deleted even if they have an out-of-date ETag. If false,
        /// only the devices that have an up-to-date ETag will be deleted. An up-to-date ETag can be retrieved
        /// using <see cref="GetAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The result of the bulk operation.</returns>
        public virtual async Task<BulkRegistryOperationResult> DeleteAsync(IEnumerable<Device> devices, bool forceDelete, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Deleting devices : count: {devices?.Count()} - Force delete: {forceDelete}", nameof(DeleteAsync));

            try
            {
                ImportMode importMode = forceDelete ? ImportMode.Delete : ImportMode.DeleteIfMatchETag;
                IEnumerable<ExportImportDevice> exportImportDevices = GenerateExportImportDeviceListForBulkOperations(devices, importMode);
                return await BulkDeviceOperationAsync(exportImportDevices, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(DeleteAsync)} threw an exception: {ex}", nameof(DeleteAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Deleting devices : count: {devices?.Count()} - Force delete: {forceDelete}", nameof(DeleteAsync));
            }
        }

        /// <summary>
        /// Copies registered device data to a set of blobs in a specific container in a storage account.
        /// </summary>
        /// <param name="storageAccountConnectionString">ConnectionString to the destination StorageAccount.</param>
        /// <param name="containerName">Destination blob container name.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual async Task ExportAsync(string storageAccountConnectionString, string containerName, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Exporting registry", nameof(ExportAsync));
            try
            {
                var payload = new ExportImportRequest
                {
                    ContainerName = containerName,
                    StorageConnectionString = storageAccountConnectionString,
                };

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, GetAdminUri("exportRegistry"), _credentialProvider, payload);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.NoContent, response);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ExportAsync)} threw an exception: {ex}", nameof(ExportAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Exporting registry", nameof(ExportAsync));
            }
        }

        /// <summary>
        /// Imports registered device data from a set of blobs in a specific container in a storage account.
        /// </summary>
        /// <param name="storageAccountConnectionString">ConnectionString to the source StorageAccount.</param>
        /// <param name="containerName">Source blob container name.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual async Task ImportAsync(string storageAccountConnectionString, string containerName, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Importing registry", nameof(ImportAsync));

            try
            {
                var payload = new ExportImportRequest
                {
                    ContainerName = containerName,
                    StorageConnectionString = storageAccountConnectionString,
                };

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, GetAdminUri("importRegistry"), _credentialProvider, payload);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.NoContent, response);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ImportAsync)} threw an exception: {ex}", nameof(ImportAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Importing registry", nameof(ImportAsync));
            }
        }

        /// <summary>
        /// Creates a new bulk job to export device registrations to the container specified by the provided URI.
        /// </summary>
        /// <param name="exportBlobContainerUri">Destination blob container URI.</param>
        /// <param name="excludeKeys">Specifies whether to exclude the Device's Keys during the export.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public virtual Task<JobProperties> ExportAsync(Uri exportBlobContainerUri, bool excludeKeys, CancellationToken cancellationToken = default)
        {
            return ExportAsync(
                JobProperties.CreateForExportJob(
                    exportBlobContainerUri,
                    excludeKeys),
                cancellationToken);
        }

        /// <summary>
        /// Creates a new bulk job to export device registrations to the container specified by the provided URI.
        /// </summary>
        /// <param name="exportBlobContainerUri">Destination blob container URI.</param>
        /// <param name="outputBlobName">The name of the blob that will be created in the provided output blob container.</param>
        /// <param name="excludeKeys">Specifies whether to exclude the Device's Keys during the export.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public virtual Task<JobProperties> ExportAsync(Uri exportBlobContainerUri, string outputBlobName, bool excludeKeys, CancellationToken cancellationToken = default)
        {
            return ExportAsync(
                JobProperties.CreateForExportJob(
                    exportBlobContainerUri,
                    excludeKeys,
                    outputBlobName),
                cancellationToken);
        }

        /// <summary>
        /// Creates a new bulk job to export device registrations to the container specified by the provided URI.
        /// </summary>
        /// <param name="jobParameters">Parameters for the job.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <remarks>Conditionally includes configurations, if specified.</remarks>
        /// <returns>JobProperties of the newly created job.</returns>
        public virtual Task<JobProperties> ExportAsync(JobProperties jobParameters, CancellationToken cancellationToken = default)
        {
            Argument.RequireNotNull(jobParameters, nameof(jobParameters));

            if (Logging.IsEnabled)
                Logging.Enter(this, $"Export Job running with {jobParameters}", nameof(ExportAsync));

            try
            {
                jobParameters.Type = JobType.ExportDevices;
                return CreateJobAsync(jobParameters, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ExportAsync)} threw an exception: {ex}", nameof(ExportAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Export Job running with {jobParameters}", nameof(ExportAsync));
            }
        }

        /// <summary>
        /// Creates a new bulk job to import device registrations into the IoT hub.
        /// </summary>
        /// <param name="importBlobContainerUri">Source blob container URI.</param>
        /// <param name="outputBlobContainerUri">Destination blob container URI.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public virtual Task<JobProperties> ImportAsync(Uri importBlobContainerUri, Uri outputBlobContainerUri, CancellationToken cancellationToken = default)
        {
            return ImportAsync(
               JobProperties.CreateForImportJob(
                   importBlobContainerUri,
                   outputBlobContainerUri),
               cancellationToken);
        }

        /// <summary>
        /// Creates a new bulk job to import device registrations into the IoT hub.
        /// </summary>
        /// <param name="importBlobContainerUri">Source blob container URI.</param>
        /// <param name="outputBlobContainerUri">Destination blob container URI.</param>
        /// <param name="inputBlobName">The blob name to be used when importing from the provided input blob container.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public virtual Task<JobProperties> ImportAsync(Uri importBlobContainerUri, Uri outputBlobContainerUri, string inputBlobName, CancellationToken cancellationToken = default)
        {
            return ImportAsync(
               JobProperties.CreateForImportJob(
                   importBlobContainerUri,
                   outputBlobContainerUri,
                   inputBlobName),
               cancellationToken);
        }

        /// <summary>
        /// Creates a new bulk job to import device registrations into the IoT hub.
        /// </summary>
        /// <param name="jobParameters">Parameters for the job.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <remarks>Conditionally includes configurations, if specified.</remarks>
        /// <returns>JobProperties of the newly created job.</returns>
        public virtual Task<JobProperties> ImportAsync(JobProperties jobParameters, CancellationToken cancellationToken = default)
        {
            Argument.RequireNotNull(jobParameters, nameof(jobParameters));

            if (Logging.IsEnabled)
                Logging.Enter(this, $"Import Job running with {jobParameters}", nameof(ImportAsync));
            try
            {
                jobParameters.Type = JobType.ImportDevices;
                return CreateJobAsync(jobParameters, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ExportAsync)} threw an exception: {ex}", nameof(ImportAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Import Job running with {jobParameters}", nameof(ImportAsync));
            }
        }

        /// <summary>
        /// Gets the registry job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the registry job to retrieve.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>JobProperties of the job specified by the provided jobId.</returns>
        public virtual async Task<JobProperties> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting job {jobId}", nameof(GetJobsAsync));

            try
            {
                Argument.RequireNotNull(jobId, nameof(jobId));

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetJobUri(jobId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<JobProperties>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(GetJobsAsync)} threw an exception: {ex}", nameof(GetJobsAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting job {jobId}", nameof(GetJobsAsync));
            }
        }

        /// <summary>
        /// List all registry jobs for the IoT hub.
        /// </summary>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>IEnumerable of JobProperties of all jobs for this IoT hub.</returns>
        public virtual async Task<IEnumerable<JobProperties>> GetJobsAsync(CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting job", nameof(GetJobsAsync));

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetListJobsUri(), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<IEnumerable<JobProperties>>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(GetJobsAsync)} threw an exception: {ex}", nameof(GetJobsAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting job", nameof(GetJobsAsync));
            }
        }

        /// <summary>
        /// Cancels/Deletes the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to cancel.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual async Task CancelJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Canceling job: {jobId}", nameof(CancelJobAsync));

            try
            {
                Argument.RequireNotNull(jobId, nameof(jobId));

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Delete, GetJobUri(jobId), _credentialProvider);
                HttpMessageHelper2.InsertEtag(request, jobId);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.NoContent, response);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(GetJobsAsync)} threw an exception: {ex}", nameof(GetJobsAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting job {jobId}", nameof(GetJobsAsync));
            }
        }

        /// <summary>
        /// Gets the registry statistics for your IoT hub.
        /// </summary>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The registry statistics for you Iot hub.</returns>
        public virtual async Task<RegistryStatistics> GetRegistryStatisticsAsync(CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting registry statistics", nameof(GetRegistryStatisticsAsync));

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetStatisticsUri(), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<RegistryStatistics>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(GetRegistryStatisticsAsync)} threw an exception: {ex}", nameof(GetRegistryStatisticsAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting registry statistics", nameof(GetRegistryStatisticsAsync));
            }
        }

        private static Uri GetRequestUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(DeviceRequestUriFormat.FormatInvariant(deviceId), UriKind.Relative);
        }

        private static Uri GetModulesOnDeviceRequestUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(ModulesOnDeviceRequestUriFormat.FormatInvariant(deviceId), UriKind.Relative);
        }

        private static Uri GetBulkRequestUri()
        {
            return new Uri(DeviceRequestUriFormat.FormatInvariant(string.Empty), UriKind.Relative);
        }

        private static Uri GetAdminUri(string operation)
        {
            return new Uri(AdminUriFormat.FormatInvariant(operation), UriKind.Relative);
        }

        private static Uri GetJobUri(string jobId)
        {
            return new Uri(JobsGetUriFormat.FormatInvariant(jobId), UriKind.Relative);
        }

        private static Uri GetListJobsUri()
        {
            return new Uri(JobsListUriFormat.FormatInvariant(), UriKind.Relative);
        }

        private static Uri GetCreateJobsUri()
        {
            return new Uri(JobsCreateUriFormat.FormatInvariant(), UriKind.Relative);
        }

        private static Uri GetStatisticsUri()
        {
            return new Uri(StatisticsUriFormat.FormatInvariant(ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }

        private static IEnumerable<ExportImportDevice> GenerateExportImportDeviceListForBulkOperations(IEnumerable<Device> devices, ImportMode importMode)
        {
            Argument.RequireNotNullOrEmpty(devices, nameof(devices));

            var exportImportDeviceList = new List<ExportImportDevice>(devices.Count());
            foreach (Device device in devices)
            {
                Argument.RequireNotNull(device, nameof(device));

                switch (importMode)
                {
                    case ImportMode.Create:
                        if (!string.IsNullOrWhiteSpace(device.ETag))
                        {
                            throw new ArgumentException(ApiResources.ETagSetWhileRegisteringDevice);
                        }
                        break;

                    case ImportMode.Update:
                        // No preconditions
                        break;

                    case ImportMode.UpdateIfMatchETag:
                        if (string.IsNullOrWhiteSpace(device.ETag))
                        {
                            throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingDevice);
                        }
                        break;

                    case ImportMode.Delete:
                        // No preconditions
                        break;

                    case ImportMode.DeleteIfMatchETag:
                        if (string.IsNullOrWhiteSpace(device.ETag))
                        {
                            throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingDevice);
                        }
                        break;

                    default:
                        throw new ArgumentException(IotHubApiResources.GetString(ApiResources.InvalidImportMode, importMode));
                }

                var exportImportDevice = new ExportImportDevice(device, importMode);
                exportImportDeviceList.Add(exportImportDevice);
            }

            return exportImportDeviceList;
        }

        private async Task<BulkRegistryOperationResult> BulkDeviceOperationAsync(IEnumerable<ExportImportDevice> devices, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Performing bulk device operation on : {devices?.Count()} devices.", nameof(BulkDeviceOperationAsync));

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, GetBulkRequestUri(), _credentialProvider, devices);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<BulkRegistryOperationResult>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(BulkDeviceOperationAsync)} threw an exception: {ex}", nameof(BulkDeviceOperationAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Performing bulk device operation on : {devices?.Count()} devices.", nameof(BulkDeviceOperationAsync));
            }
        }

        private async Task<JobProperties> CreateJobAsync(JobProperties jobProperties, CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, GetCreateJobsUri(), _credentialProvider, jobProperties);
            HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
            return await HttpMessageHelper2.DeserializeResponse<JobProperties>(response, cancellationToken);
        }
    }
}
