// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const string DeviceRequestUriFormat = "/devices/{0}";
        private const string InvalidImportMode = "InvalidImportMode";

        private static readonly Uri s_createJobsUri = new("/jobs/create", UriKind.Relative);
        private static readonly Uri s_getJobsUri = new("/jobs", UriKind.Relative);
        private static readonly Uri s_getDeviceStatsUri = new("/statistics/devices", UriKind.Relative);
        private static readonly Uri s_getServiceStatsUri = new("/statistics/service", UriKind.Relative);

        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;

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
        /// <exception cref="ArgumentNullException">When the provided device is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
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

            Argument.AssertNotNull(device, nameof(device));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetRequestUri(device.Id), _credentialProvider, device);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Device>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Creating device threw an exception: {ex}", nameof(CreateAsync));
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
        /// <exception cref="ArgumentNullException">When the provided device Id is null.</exception>
        /// <exception cref="ArgumentException">When the provided device Id is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<Device> GetAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting device {deviceId}", nameof(GetAsync));

            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetRequestUri(deviceId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Device>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Getting device threw an exception: {ex}", nameof(GetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting device {deviceId}", nameof(GetAsync));
            }
        }

        /// <summary>
        /// Replace a device identity's state with the provided device identity's state.
        /// </summary>
        /// <param name="device">The device identity's new state.</param>
        /// <param name="onlyIfUnchanged">
        /// If false, this update operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.PreconditionFailed"/>
        /// if the provided device identity has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The newly updated device identity including its new ETag.</returns>
        /// <exception cref="ArgumentNullException">When the provided device is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<Device> SetAsync(Device device, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating device {device?.Id}", nameof(SetAsync));

            Argument.AssertNotNull(device, nameof(device));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetRequestUri(device.Id), _credentialProvider, device);
                HttpMessageHelper.ConditionallyInsertETag(request, device.ETag, onlyIfUnchanged);

                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Device>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Updating device threw an exception: {ex}", nameof(SetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Updating device {device?.Id}", nameof(SetAsync));
            }
        }

        /// <summary>
        /// Delete the device identity with the provided Id from your IoT hub's registry.
        /// </summary>
        /// <param name="deviceId">The Id of the device identity to be deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <exception cref="ArgumentNullException">When the provided device Id is null.</exception>
        /// <exception cref="ArgumentException">When the provided device Id is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task DeleteAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));

            await DeleteAsync(new Device(deviceId), default, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete the device identity with the provided Id from your IoT hub's registry.
        /// </summary>
        /// <param name="device">
        /// The device identity to delete from your IoT hub's registry. If the provided device's ETag
        /// is out of date, this operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.PreconditionFailed"/>
        /// An up-to-date ETag can be retrieved using <see cref="GetAsync(string, CancellationToken)"/>.
        /// To force the operation to execute regardless of ETag, set the device identity's ETag to "*" or
        /// use <see cref="DeleteAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="onlyIfUnchanged">
        /// If false, this delete operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.PreconditionFailed"/>
        /// if the provided device identity has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <exception cref="ArgumentNullException">When the provided device is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task DeleteAsync(Device device, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Deleting device {device?.Id}", nameof(DeleteAsync));

            Argument.AssertNotNull(device, nameof(device));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Delete, GetRequestUri(device.Id), _credentialProvider);
                HttpMessageHelper.ConditionallyInsertETag(request, device.ETag, onlyIfUnchanged);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.NoContent, response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Deleting device threw an exception: {ex}", nameof(DeleteAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Deleting device {device?.Id}", nameof(DeleteAsync));
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
        /// <exception cref="ArgumentNullException">When the provided device or twin is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<BulkRegistryOperationResult> CreateWithTwinAsync(Device device, Twin twin, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Creating device with twin {device?.Id}", nameof(CreateWithTwinAsync));

            Argument.AssertNotNull(device, nameof(device));
            Argument.AssertNotNull(twin, nameof(twin));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var exportImportDeviceList = new List<ExportImportDevice>
                {
                    new ExportImportDevice(device, ImportMode.Create)
                    {
                        Tags = twin.Tags,
                        Properties = new ExportImportDevice.PropertyContainer
                        {
                            DesiredProperties = twin.Properties.Desired,
                            ReportedProperties = twin.Properties.Reported,
                        }
                    }
                }; ;

                return await BulkDeviceOperationAsync(exportImportDeviceList, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Creating device with twin threw an exception: {ex}", nameof(CreateWithTwinAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Creating device with twin {device?.Id}", nameof(CreateWithTwinAsync));
            }
        }

        /// <summary>
        /// Get all the modules that are registered on a particular device.
        /// </summary>
        /// <param name="deviceId">The Id of the device to get the modules of.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The modules that are registered on the specified device.</returns>
        /// <exception cref="ArgumentNullException">When the provided device Id is null.</exception>
        /// <exception cref="ArgumentException">When the provided device Id is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<IEnumerable<Module>> GetModulesAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting modules on device {deviceId}", nameof(GetModulesAsync));

            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetModulesOnDeviceRequestUri(deviceId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<IEnumerable<Module>>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Getting modules on device threw an exception: {ex}", nameof(GetModulesAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting modules on device {deviceId}", nameof(GetModulesAsync));
            }
        }

        /// <summary>
        /// Create up to 100 new device identities in your IoT hub's registry in bulk.
        /// </summary>
        /// <remarks>
        /// For larger scale operations, consider using <see cref="ImportAsync(ImportJobProperties, CancellationToken)"/>
        /// which allows you to import devices from an Azure Storage container.
        /// </remarks>
        /// <param name="devices">The device identities to create in your IoT hub's registry. May not exceed 100 devices.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The result of the bulk operation.</returns>
        /// <exception cref="ArgumentNullException">When the provided device collection is null.</exception>
        /// <exception cref="ArgumentException">When the provided device collection is empty.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<BulkRegistryOperationResult> CreateAsync(IEnumerable<Device> devices, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Creating devices", nameof(CreateAsync));

            Argument.AssertNotNullOrEmpty(devices, nameof(devices));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                IEnumerable<ExportImportDevice> exportImportDevices = GenerateExportImportDeviceListForBulkOperations(devices, ImportMode.Create);
                return await BulkDeviceOperationAsync(exportImportDevices, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Creating devices threw an exception: {ex}", nameof(CreateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Creating devices", nameof(CreateAsync));
            }
        }

        /// <summary>
        /// Update up to 100 device identities in your IoT hub's registry in bulk.
        /// </summary>
        /// <param name="devices">The device identities to update to your IoT hub's registry. May not exceed 100 devices.</param>
        /// <param name="onlyIfUnchanged">
        /// If false, this update operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.PreconditionFailed"/>
        /// if the provided device identity has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The result of the bulk operation.</returns>
        /// <exception cref="ArgumentNullException">When the provided device collection is null.</exception>
        /// <exception cref="ArgumentException">When the provided device collection is empty.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<BulkRegistryOperationResult> SetAsync(IEnumerable<Device> devices, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating multiple devices ", nameof(SetAsync));

            Argument.AssertNotNullOrEmpty(devices, nameof(devices));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                ImportMode importMode = onlyIfUnchanged ? ImportMode.UpdateIfMatchETag : ImportMode.Update;
                IEnumerable<ExportImportDevice> exportImportDevices = GenerateExportImportDeviceListForBulkOperations(devices, importMode);
                return await BulkDeviceOperationAsync(exportImportDevices, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Updating multiple devices threw an exception: {ex}", nameof(SetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Updating multiple devices", nameof(SetAsync));
            }
        }

        /// <summary>
        /// Delete up to 100 device identities from your IoT hub's registry in bulk.
        /// </summary>
        /// <param name="devices">The device identities to delete from your IoT hub's registry. May not exceed 100 devices.</param>
        /// <param name="onlyIfUnchanged">
        /// If false, this delete operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.PreconditionFailed"/>
        /// if the provided device identity has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The result of the bulk operation.</returns>
        /// <exception cref="ArgumentNullException">When the provided device collection is null.</exception>
        /// <exception cref="ArgumentException">When the provided device collection is empty.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<BulkRegistryOperationResult> DeleteAsync(IEnumerable<Device> devices, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Deleting devices", nameof(DeleteAsync));

            Argument.AssertNotNullOrEmpty(devices, nameof(devices));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                ImportMode importMode = onlyIfUnchanged ? ImportMode.DeleteIfMatchETag : ImportMode.Delete;
                IEnumerable<ExportImportDevice> exportImportDevices = GenerateExportImportDeviceListForBulkOperations(devices, importMode);
                return await BulkDeviceOperationAsync(exportImportDevices, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Deleting devices threw an exception: {ex}", nameof(DeleteAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Deleting devices", nameof(DeleteAsync));
            }
        }

        /// <summary>
        /// Creates a new bulk job to import device registrations into the IoT hub.
        /// </summary>
        /// <param name="jobParameters">Parameters for the job.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <exception cref="ArgumentNullException">When the provided connection string or container name is null.</exception>
        /// <exception cref="ArgumentException">When the provided connection string or container name is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<JobProperties> ImportAsync(ImportJobProperties jobParameters, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Running import job", nameof(ImportAsync));

            Argument.AssertNotNull(jobParameters, nameof(jobParameters));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await CreateJobAsync(jobParameters, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Running import job threw an exception: {ex}", nameof(ImportAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Running import job", nameof(ImportAsync));
            }
        }

        /// <summary>
        /// Creates a new bulk job to export device registrations to the container specified by the provided URI.
        /// </summary>
        /// <param name="jobParameters">Parameters for the job.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <remarks>Conditionally includes configurations, if specified.</remarks>
        /// <returns>JobProperties of the newly created job.</returns>
        /// <exception cref="ArgumentNullException">When the provided job properties instance is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<JobProperties> ExportAsync(ExportJobProperties jobParameters, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Running export job", nameof(ExportAsync));

            Argument.AssertNotNull(jobParameters, nameof(jobParameters));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await CreateJobAsync(jobParameters, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Running export job threw an exception: {ex}", nameof(ExportAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Running export job", nameof(ExportAsync));
            }
        }

        /// <summary>
        /// Gets the registry job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the registry job to retrieve.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>ImportJobProperties of the job specified by the provided jobId.</returns>
        /// <exception cref="ArgumentNullException">When the provided job Id is null.</exception>
        /// <exception cref="ArgumentException">When the provided job Id is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<JobProperties> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting job {jobId}", nameof(GetJobAsync));

            Argument.AssertNotNullOrWhiteSpace(jobId, nameof(jobId));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetJobUri(jobId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<JobProperties>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Getting job {jobId} threw an exception: {ex}", nameof(GetJobAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting job {jobId}", nameof(GetJobAsync));
            }
        }

        /// <summary>
        /// List all registry jobs for the IoT hub.
        /// </summary>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>IEnumerable of ImportJobProperties of all jobs for this IoT hub.</returns>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<IEnumerable<JobProperties>> GetJobsAsync(CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting jobs", nameof(GetJobsAsync));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, s_getJobsUri, _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<IEnumerable<JobProperties>>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Getting jobs threw an exception: {ex}", nameof(GetJobsAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting jobs", nameof(GetJobsAsync));
            }
        }

        /// <summary>
        /// Cancels/deletes the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to cancel.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <exception cref="ArgumentNullException">When the provided job Id is null.</exception>
        /// <exception cref="ArgumentException">When the provided job Id is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task CancelJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Canceling job {jobId}", nameof(CancelJobAsync));

            Argument.AssertNotNullOrWhiteSpace(jobId, nameof(jobId));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Delete, GetJobUri(jobId), _credentialProvider);
                HttpMessageHelper.ConditionallyInsertETag(request, new ETag(jobId), false);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.NoContent, response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Canceling job {jobId} threw an exception: {ex}", nameof(CancelJobAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Canceling job {jobId}", nameof(CancelJobAsync));
            }
        }

        /// <summary>
        /// Gets the registry statistics for your IoT hub.
        /// </summary>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The registry statistics for you Iot hub.</returns>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
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

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, s_getDeviceStatsUri, _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<RegistryStatistics>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Getting registry statistics threw an exception: {ex}", nameof(GetRegistryStatisticsAsync));
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
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
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

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, s_getServiceStatsUri, _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<ServiceStatistics>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Getting service statistics threw an exception: {ex}", nameof(GetServiceStatisticsAsync));
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
            return new Uri(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DeviceRequestUriFormat,
                    deviceId),
                UriKind.Relative);
        }

        private static Uri GetModulesOnDeviceRequestUri(string deviceId)
        {
            const string modulesOnDeviceRequestUriFormat = "/devices/{0}/modules";

            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(
                string.Format(
                    CultureInfo.InvariantCulture,
                    modulesOnDeviceRequestUriFormat,
                    deviceId),
                UriKind.Relative);
        }

        private static Uri GetBulkRequestUri()
        {
            return new Uri(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DeviceRequestUriFormat,
                    string.Empty),
                UriKind.Relative);
        }

        private static Uri GetJobUri(string jobId)
        {
            const string jobsGetUriFormat = "/jobs/{0}";

            return new Uri(
                string.Format(
                    CultureInfo.InvariantCulture,
                    jobsGetUriFormat,
                    jobId),
                UriKind.Relative);
        }

        private static IEnumerable<ExportImportDevice> GenerateExportImportDeviceListForBulkOperations(IEnumerable<Device> devices, ImportMode importMode)
        {
            const string eTagNotSetWhileUpdatingDevice = "ETagNotSetWhileUpdatingDevice";
            const string eTagNotSetWhileDeletingDevice = "ETagNotSetWhileDeletingDevice";

            var exportImportDeviceList = new List<ExportImportDevice>(devices.Count());
            foreach (Device device in devices)
            {
                if (device == null)
                {
                    throw new InvalidOperationException("Devices in the bulk operation list must not be null.");
                }

                switch (importMode)
                {
                    case ImportMode.Create:
                        if (!string.IsNullOrWhiteSpace(device.ETag.ToString()))
                        {
                            throw new InvalidOperationException("The ETag must not be set when creating a device.");
                        }
                        break;

                    case ImportMode.Update:
                        // No preconditions
                        break;

                    case ImportMode.UpdateIfMatchETag:
                        if (string.IsNullOrWhiteSpace(device.ETag.ToString()))
                        {
                            throw new ArgumentException(eTagNotSetWhileUpdatingDevice);
                        }
                        break;

                    case ImportMode.Delete:
                        // No preconditions
                        break;

                    case ImportMode.DeleteIfMatchETag:
                        if (string.IsNullOrWhiteSpace(device.ETag.ToString()))
                        {
                            throw new ArgumentException(eTagNotSetWhileDeletingDevice);
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
            Debug.Assert(devices != null, $"{nameof(BulkDeviceOperationAsync)} called with null for devices.");

            cancellationToken.ThrowIfCancellationRequested();

            using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, GetBulkRequestUri(), _credentialProvider, devices);
            HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
            return await HttpMessageHelper.DeserializeResponseAsync<BulkRegistryOperationResult>(response).ConfigureAwait(false);
        }

        private async Task<JobProperties> CreateJobAsync(JobProperties jobProperties, CancellationToken cancellationToken)
        {
            Debug.Assert(jobProperties != null, $"{nameof(CreateJobAsync)} called with null for jobProperties.");

            cancellationToken.ThrowIfCancellationRequested();

            using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, s_createJobsUri, _credentialProvider, jobProperties);
            HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
            return await HttpMessageHelper.DeserializeResponseAsync<JobProperties>(response).ConfigureAwait(false);
        }
    }
}
