// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> that handles all device registry operations including
    /// getting/creating/setting/deleting device identities, getting modules on a device, and getting device
    /// registry statistics.
    /// </summary>
    public class DevicesClient
    {
        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;

        private const string DeviceRequestUriFormat = "/devices/{0}";
        private const string ModulesOnDeviceRequestUriFormat = "/devices/{0}/modules";
        private const string JobsGetUriFormat = "/jobs/{0}";
        private const string JobsListUriFormat = "/jobs";
        private const string AdminUriFormat = "/$admin/{0}";
        private const string JobsCreateUriFormat = "/jobs/create";
        private const string DeviceStatisticsUriFormat = "/statistics/devices";
        private const string ServiceStatisticsUriFormat = "/statistics/service";
        private const string ETagSetWhileRegisteringDevice = "ETagSetWhileRegisteringDevice";
        private const string InvalidImportMode = "InvalidImportMode";
        private const string ETagNotSetWhileUpdatingDevice = "ETagNotSetWhileUpdatingDevice";
        private const string ETagNotSetWhileDeletingDevice = "ETagNotSetWhileDeletingDevice";

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected DevicesClient()
        {
        }

        internal DevicesClient(string hostName, IotHubConnectionProperties credentialProvider, HttpClient httpClient, HttpRequestMessageFactory httpRequestMessageFactory)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
        }

        /// <summary>
        /// Create a device identity in your IoT hub's registry.
        /// </summary>
        /// <param name="device">The device identity to register.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The registered device with the generated keys and ETags.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided device is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<Device> CreateAsync(Device device, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Creating device: {device?.Id}", nameof(CreateAsync));

            try
            {
                Argument.AssertNotNull(device, nameof(device));

                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetRequestUri(device.Id), _credentialProvider, device);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Device>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(CreateAsync)} threw an exception: {ex}", nameof(CreateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Creating device: {device?.Id}", nameof(CreateAsync));
            }
        }

        /// <summary>
        /// Get a device identity by its Id.
        /// </summary>
        /// <param name="deviceId">The unique identifier of the device identity to retrieve.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The retrieved device identity.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided device Id is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided device Id is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<Device> GetAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting device: {deviceId}", nameof(GetAsync));

            try
            {
                Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));

                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetRequestUri(deviceId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Device>(response).ConfigureAwait(false);
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
        /// <param name="onlyIfUnchanged">
        /// If false, this update operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.PreconditionFailed"/>
        /// if the provided device identity has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The newly updated device identity including its new ETag.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided device is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<Device> SetAsync(Device device, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating device: {device?.Id}", nameof(SetAsync));

            try
            {
                Argument.AssertNotNull(device, nameof(device));

                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(device.ETag.ToString()) && onlyIfUnchanged)
                {
                    throw new ArgumentException(ETagNotSetWhileUpdatingDevice);
                }

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetRequestUri(device.Id), _credentialProvider, device);
                HttpMessageHelper.ConditionallyInsertETag(request, device.ETag, onlyIfUnchanged);

                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Device>(response).ConfigureAwait(false);
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
        /// <exception cref="ArgumentNullException">Thrown when the provided device Id is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided device Id is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task DeleteAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));

            var device = new Device(deviceId);
            await DeleteAsync(device, default, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete the device identity with the provided Id from your IoT hub's registry.
        /// </summary>
        /// <param name="device">
        /// The device identity to delete from your IoT hub's registry. If the provided device's ETag
        /// is out of date, this operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.PreconditionFailed"/>
        /// An up-to-date ETag can be retrieved using <see cref="GetAsync(string, CancellationToken)"/>.
        /// To force the operation to execute regardless of ETag, set the device identity's ETag to "*" or
        /// use <see cref="DeleteAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="onlyIfUnchanged">
        /// If false, this delete operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.PreconditionFailed"/>
        /// if the provided device identity has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided device is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task DeleteAsync(Device device, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Deleting device: {device?.Id} - only if changed: {onlyIfUnchanged}", nameof(DeleteAsync));

            try
            {
                Argument.AssertNotNull(device, nameof(device));

                if (string.IsNullOrWhiteSpace(device.ETag.ToString()) && onlyIfUnchanged)
                {
                    throw new ArgumentException(ETagNotSetWhileDeletingDevice);
                }

                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Delete, GetRequestUri(device.Id), _credentialProvider);
                HttpMessageHelper.ConditionallyInsertETag(request, device.ETag, onlyIfUnchanged);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.NoContent, response).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Deleting device: {device?.Id} - only if changed: {onlyIfUnchanged}", nameof(DeleteAsync));
            }
        }

        /// <summary>
        /// Create a device identity in your IoT hub's registry with an initial twin state.
        /// </summary>
        /// <remarks>
        /// This API uses the same underlying service API as the bulk create/set/delete APIs defined in
        /// this client such as <see cref="CreateAsync(IEnumerable{Device}, CancellationToken)"/>.
        /// </remarks>
        /// <param name="device">The device identity to register.</param>
        /// <param name="twin">The initial twin state for the device.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The result of the bulk operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided device or twin is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<BulkRegistryOperationResult> CreateWithTwinAsync(Device device, Twin twin, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Creating device with twin: {device?.Id}", nameof(CreateWithTwinAsync));

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

                return await BulkDeviceOperationAsync(exportImportDeviceList, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(CreateWithTwinAsync)} threw an exception: {ex}", nameof(CreateWithTwinAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Creating device with twin: {device?.Id}", nameof(CreateWithTwinAsync));
            }
        }

        /// <summary>
        /// Get all the modules that are registered on a particular device.
        /// </summary>
        /// <param name="deviceId">The Id of the device to get the modules of.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The modules that are registered on the specified device.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided device Id is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided device Id is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<IEnumerable<Module>> GetModulesAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting modules on device: {deviceId}", nameof(GetModulesAsync));

            try
            {
                Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));

                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetModulesOnDeviceRequestUri(deviceId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<IEnumerable<Module>>(response).ConfigureAwait(false);
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
        /// Create up to 100 new device identities in your IoT hub's registry in bulk.
        /// </summary>
        /// <remarks>
        /// For larger scale operations, consider using <see cref="ImportAsync(string, string, CancellationToken)"/>
        /// which allows you to import devices from an Azure Storage container.
        /// </remarks>
        /// <param name="devices">The device identities to create in your IoT hub's registry. May not exceed 100 devices.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The result of the bulk operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided device collection is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided device collection is empty.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<BulkRegistryOperationResult> CreateAsync(IEnumerable<Device> devices, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Creating {devices?.Count()} devices", nameof(CreateAsync));

            try
            {
                IEnumerable<ExportImportDevice> exportImportDevices = GenerateExportImportDeviceListForBulkOperations(devices, ImportMode.Create);
                return await BulkDeviceOperationAsync(exportImportDevices, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(CreateAsync)} threw an exception: {ex}", nameof(CreateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Creating {devices?.Count()} devices", nameof(CreateAsync));
            }
        }

        /// <summary>
        /// Update up to 100 device identities in your IoT hub's registry in bulk.
        /// </summary>
        /// <param name="devices">The device identities to update to your IoT hub's registry. May not exceed 100 devices.</param>
        /// <param name="onlyIfUnchanged">
        /// If false, this update operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.PreconditionFailed"/>
        /// if the provided device identity has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The result of the bulk operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided device collection is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided device collection is empty.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<BulkRegistryOperationResult> SetAsync(IEnumerable<Device> devices, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating multiple devices: count: {devices?.Count()} - only if changed: {onlyIfUnchanged}", nameof(SetAsync));

            try
            {
                ImportMode importMode = onlyIfUnchanged ? ImportMode.UpdateIfMatchETag : ImportMode.Update;
                IEnumerable<ExportImportDevice> exportImportDevices = GenerateExportImportDeviceListForBulkOperations(devices, importMode);
                return await BulkDeviceOperationAsync(exportImportDevices, cancellationToken).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Updating multiple devices: count: {devices?.Count()} - only if changed: {onlyIfUnchanged}", nameof(SetAsync));
            }
        }

        /// <summary>
        /// Delete up to 100 device identities from your IoT hub's registry in bulk.
        /// </summary>
        /// <param name="devices">The device identities to delete from your IoT hub's registry. May not exceed 100 devices.</param>
        /// <param name="onlyIfUnchanged">
        /// If false, this delete operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.PreconditionFailed"/>
        /// if the provided device identity has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The result of the bulk operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided device collection is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided device collection is empty.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<BulkRegistryOperationResult> DeleteAsync(IEnumerable<Device> devices, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Deleting devices : count: {devices?.Count()} - only if changed: {onlyIfUnchanged}", nameof(DeleteAsync));

            try
            {
                ImportMode importMode = onlyIfUnchanged ? ImportMode.DeleteIfMatchETag : ImportMode.Delete;
                IEnumerable<ExportImportDevice> exportImportDevices = GenerateExportImportDeviceListForBulkOperations(devices, importMode);
                return await BulkDeviceOperationAsync(exportImportDevices, cancellationToken).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Deleting devices : count: {devices?.Count()} - only if changed: {onlyIfUnchanged}", nameof(DeleteAsync));
            }
        }

        /// <summary>
        /// Copies registered device data to a set of blobs in a specific container in a storage account.
        /// </summary>
        /// <param name="storageAccountConnectionString">ConnectionString to the destination StorageAccount.</param>
        /// <param name="containerName">Destination blob container name.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided connection string or container name is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided connection string or container name is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task ExportAsync(string storageAccountConnectionString, string containerName, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Exporting registry", nameof(ExportAsync));
            try
            {
                Argument.AssertNotNullOrWhiteSpace(storageAccountConnectionString, nameof(storageAccountConnectionString));
                Argument.AssertNotNullOrWhiteSpace(containerName, nameof(containerName));

                cancellationToken.ThrowIfCancellationRequested();

                var payload = new ExportImportRequest
                {
                    ContainerName = containerName,
                    StorageConnectionString = storageAccountConnectionString,
                };

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, GetAdminUri("exportRegistry"), _credentialProvider, payload);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.NoContent, response).ConfigureAwait(false);
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
        /// <exception cref="ArgumentNullException">Thrown when the provided connection string or container name is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided connection string or container name is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task ImportAsync(string storageAccountConnectionString, string containerName, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Importing registry", nameof(ImportAsync));

            try
            {
                Argument.AssertNotNullOrWhiteSpace(storageAccountConnectionString, nameof(storageAccountConnectionString));
                Argument.AssertNotNullOrWhiteSpace(containerName, nameof(containerName));

                cancellationToken.ThrowIfCancellationRequested();

                var payload = new ExportImportRequest
                {
                    ContainerName = containerName,
                    StorageConnectionString = storageAccountConnectionString,
                };

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, GetAdminUri("importRegistry"), _credentialProvider, payload);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.NoContent, response).ConfigureAwait(false);
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
        /// <exception cref="ArgumentNullException">Thrown when the provided container URI is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual Task<JobProperties> ExportAsync(Uri exportBlobContainerUri, bool excludeKeys, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(exportBlobContainerUri, nameof(exportBlobContainerUri));

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
        /// <exception cref="ArgumentNullException">Thrown when the provided container URI or blob name is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the output blob name is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual Task<JobProperties> ExportAsync(Uri exportBlobContainerUri, string outputBlobName, bool excludeKeys, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(exportBlobContainerUri, nameof(exportBlobContainerUri));

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
        /// <exception cref="ArgumentNullException">Thrown when the provided job properties instance is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual Task<JobProperties> ExportAsync(JobProperties jobParameters, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(jobParameters, nameof(jobParameters));

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
        /// <exception cref="ArgumentNullException">Thrown when the provided import or output container URI is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual Task<JobProperties> ImportAsync(Uri importBlobContainerUri, Uri outputBlobContainerUri, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(importBlobContainerUri, nameof(importBlobContainerUri));
            Argument.AssertNotNullOrWhiteSpace(outputBlobContainerUri, nameof(outputBlobContainerUri));

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
        /// <exception cref="ArgumentNullException">Thrown when the provided import or output container URI is null or when the input blob name is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided input blob name is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual Task<JobProperties> ImportAsync(Uri importBlobContainerUri, Uri outputBlobContainerUri, string inputBlobName, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(importBlobContainerUri, nameof(importBlobContainerUri));
            Argument.AssertNotNullOrWhiteSpace(outputBlobContainerUri, nameof(outputBlobContainerUri));

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
        /// <exception cref="ArgumentNullException">Thrown when the provided job properties instance is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual Task<JobProperties> ImportAsync(JobProperties jobParameters, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(jobParameters, nameof(jobParameters));

            if (Logging.IsEnabled)
                Logging.Enter(this, $"Import Job running with {jobParameters}", nameof(ImportAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

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
        /// <exception cref="ArgumentNullException">Thrown when the provided job Id is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided job Id is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<JobProperties> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting job {jobId}", nameof(GetJobsAsync));

            try
            {
                Argument.AssertNotNull(jobId, nameof(jobId));

                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetJobUri(jobId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<JobProperties>(response).ConfigureAwait(false);
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
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<IEnumerable<JobProperties>> GetJobsAsync(CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting job", nameof(GetJobsAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetListJobsUri(), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<IEnumerable<JobProperties>>(response).ConfigureAwait(false);
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
        /// <exception cref="ArgumentNullException">Thrown when the provided job Id is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided job Id is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task CancelJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Canceling job: {jobId}", nameof(CancelJobAsync));

            try
            {
                Argument.AssertNotNull(jobId, nameof(jobId));

                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Delete, GetJobUri(jobId), _credentialProvider);
                HttpMessageHelper.ConditionallyInsertETag(request, new ETag(jobId), false);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.NoContent, response).ConfigureAwait(false);
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
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<RegistryStatistics> GetRegistryStatisticsAsync(CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting registry statistics", nameof(GetRegistryStatisticsAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetDeviceStatisticsUri(), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<RegistryStatistics>(response).ConfigureAwait(false);
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

        /// <summary>
        /// Gets service statistics for the IoT hub. This call is made over HTTP.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The service statistics that can be retrieved from IoT hub, eg. the number of devices connected to the hub.</returns>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubStatusCode.ThrottlingException"/> is thrown. 
        /// For a complete list of possible error cases, see <see cref="Common.Exceptions.IotHubStatusCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<ServiceStatistics> GetServiceStatisticsAsync(CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting service statistics", nameof(GetServiceStatisticsAsync));

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetServiceStatisticsUri(), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<ServiceStatistics>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(GetServiceStatisticsAsync)} threw an exception: {ex}", nameof(GetServiceStatisticsAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting service statistics", nameof(GetServiceStatisticsAsync));
            }
        }

        private static Uri GetRequestUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(string.Format(CultureInfo.InvariantCulture, DeviceRequestUriFormat, deviceId), UriKind.Relative);
        }

        private static Uri GetModulesOnDeviceRequestUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(string.Format(CultureInfo.InvariantCulture, ModulesOnDeviceRequestUriFormat, deviceId), UriKind.Relative);
        }

        private static Uri GetBulkRequestUri()
        {
            return new Uri(string.Format(CultureInfo.InvariantCulture, DeviceRequestUriFormat, string.Empty), UriKind.Relative);
        }

        private static Uri GetAdminUri(string operation)
        {
            return new Uri(string.Format(CultureInfo.InvariantCulture, AdminUriFormat, operation), UriKind.Relative);
        }

        private static Uri GetJobUri(string jobId)
        {
            return new Uri(string.Format(CultureInfo.InvariantCulture, JobsGetUriFormat, jobId), UriKind.Relative);
        }

        private static Uri GetListJobsUri()
        {
            return new Uri(JobsListUriFormat, UriKind.Relative);
        }

        private static Uri GetCreateJobsUri()
        {
            return new Uri(JobsCreateUriFormat, UriKind.Relative);
        }

        private static Uri GetDeviceStatisticsUri()
        {
            return new Uri(DeviceStatisticsUriFormat, UriKind.Relative);
        }

        private static Uri GetServiceStatisticsUri()
        {
            return new Uri(ServiceStatisticsUriFormat, UriKind.Relative);
        }

        private static IEnumerable<ExportImportDevice> GenerateExportImportDeviceListForBulkOperations(IEnumerable<Device> devices, ImportMode importMode)
        {
            Argument.AssertNotNullOrEmpty(devices, nameof(devices));

            var exportImportDeviceList = new List<ExportImportDevice>(devices.Count());
            foreach (Device device in devices)
            {
                Argument.AssertNotNull(device, nameof(device));

                switch (importMode)
                {
                    case ImportMode.Create:
                        if (!string.IsNullOrWhiteSpace(device.ETag.ToString()))
                        {
                            throw new ArgumentException(ETagSetWhileRegisteringDevice);
                        }
                        break;

                    case ImportMode.Update:
                        // No preconditions
                        break;

                    case ImportMode.UpdateIfMatchETag:
                        if (string.IsNullOrWhiteSpace(device.ETag.ToString()))
                        {
                            throw new ArgumentException(ETagNotSetWhileUpdatingDevice);
                        }
                        break;

                    case ImportMode.Delete:
                        // No preconditions
                        break;

                    case ImportMode.DeleteIfMatchETag:
                        if (string.IsNullOrWhiteSpace(device.ETag.ToString()))
                        {
                            throw new ArgumentException(ETagNotSetWhileDeletingDevice);
                        }
                        break;

                    default:
                        throw new ArgumentException($"{InvalidImportMode} {importMode}.");
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
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, GetBulkRequestUri(), _credentialProvider, devices);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<BulkRegistryOperationResult>(response).ConfigureAwait(false);
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
            cancellationToken.ThrowIfCancellationRequested();

            using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, GetCreateJobsUri(), _credentialProvider, jobProperties);
            HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
            return await HttpMessageHelper.DeserializeResponseAsync<JobProperties>(response).ConfigureAwait(false);
        }
    }
}
