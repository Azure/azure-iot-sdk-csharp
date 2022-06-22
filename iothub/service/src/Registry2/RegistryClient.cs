// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Microsoft.Azure.Devices.Http2;

namespace Microsoft.Azure.Devices.Registry
{
    /// <summary>
    ///
    /// </summary>
    public class RegistryClient : IDisposable
    {
        private string _hostName;
        private IotHubConnectionProperties _credentialProvider;
        private HttpTransportSettings2 _settings;
        private HttpClient _httpClient;

        private const string DeviceRequestUriFormat = "/devices/{0}?{1}";
        private const string ModulesRequestUriFormat = "/devices/{0}/modules/{1}?{2}";
        private const string ModulesOnDeviceRequestUriFormat = "/devices/{0}/modules?{1}";
        private const string StatisticsUriFormat = "/statistics/devices?" + ClientApiVersionHelper.ApiVersionQueryString;

        /// <summary>
        /// Creates an instance of RegistryManager, provided for unit testing purposes only.
        /// </summary>
        public RegistryClient()
        {
        }

        /// <summary>
        /// Creates an instance of RegistryManager, authenticating using an IoT hub connection string, and specifying
        /// HTTP transport settings.
        /// </summary>
        /// <param name="connectionString">The IoT hub connection string.</param>
        /// <param name="transportSettings">The HTTP transport settings.</param>
        /// <returns>A RegistryManager instance.</returns>
        public RegistryClient(string connectionString, HttpTransportSettings2 transportSettings = default)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            if (transportSettings == null)
            {
                throw new ArgumentNullException(nameof(transportSettings));
            }

            var iotHubConnectionString = IotHubConnectionString.Parse(connectionString);
            _credentialProvider = iotHubConnectionString;
            _settings = transportSettings;
            _hostName = iotHubConnectionString.HostName;
            _httpClient = HttpClientFactory.Create(_hostName, _settings);
        }

        /// <summary>
        /// Creates RegistryManager, authenticating using an identity in Azure Active Directory (AAD).
        /// </summary>
        /// <remarks>
        /// For more about information on the options of authenticating using a derived instance of <see cref="TokenCredential"/>, see
        /// <see href="https://docs.microsoft.com/dotnet/api/overview/azure/identity-readme"/>.
        /// For more information on configuring IoT hub with Azure Active Directory, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-dev-guide-azure-ad-rbac"/>
        /// </remarks>
        /// <param name="hostName">IoT hub host name.</param>
        /// <param name="credential">Azure Active Directory (AAD) credentials to authenticate with IoT hub.</param>
        /// <param name="transportSettings">The HTTP transport settings.</param>
        /// <returns>A RegistryManager instance.</returns>
        public RegistryClient(
            string hostName,
            TokenCredential credential,
            HttpTransportSettings2 transportSettings = default)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentException(nameof(hostName));
            }

            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            if (transportSettings == null)
            {
                throw new ArgumentNullException(nameof(transportSettings));
            }

            _credentialProvider = new IotHubTokenCrendentialProperties(hostName, credential);
            _hostName = hostName;
            _settings = transportSettings;
            _httpClient = HttpClientFactory.Create(_hostName, _settings);
        }

        /// <summary>
        /// Creates RegistryManager using a shared access signature provided and refreshed as necessary by the caller.
        /// </summary>
        /// <remarks>
        /// Users may wish to build their own shared access signature (SAS) tokens rather than give the shared key to the SDK and let it manage signing and renewal.
        /// The <see cref="AzureSasCredential"/> object gives the SDK access to the SAS token, while the caller can update it as necessary using the
        /// <see cref="AzureSasCredential.Update(string)"/> method.
        /// </remarks>
        /// <param name="hostName">IoT hub host name.</param>
        /// <param name="credential">Credential that generates a SAS token to authenticate with IoT hub. See <see cref="AzureSasCredential"/>.</param>
        /// <param name="transportSettings">The HTTP transport settings.</param>
        /// <returns>A RegistryManager instance.</returns>
        public RegistryClient(
            string hostName,
            AzureSasCredential credential,
            HttpTransportSettings2 transportSettings = default)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentException(nameof(hostName));
            }

            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            if (transportSettings == null)
            {
                throw new ArgumentNullException(nameof(transportSettings));
            }

            _credentialProvider = new IotHubSasCredentialProperties(hostName, credential);
            _hostName = hostName;
            _settings = transportSettings;
            _httpClient = HttpClientFactory.Create(_hostName, _settings);
        }

        /// <summary>
        /// Register a new device with the system
        /// </summary>
        /// <param name="device">The Device object being registered.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Device object with the generated keys and ETags.</returns>
        public virtual async Task<Device> AddDeviceAsync(Device device, CancellationToken cancellationToken = default)
        {
            Logging.Enter(this, $"Adding device: {device?.Id}", nameof(AddDeviceAsync));

            try
            {
                if (device == null)
                {
                    throw new ArgumentNullException(nameof(device));
                }

                using HttpRequestMessage request = HttpMessageHelper2.CreateRequest(HttpMethod.Put, GetRequestUri(device.Id), _credentialProvider, device);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<Device>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(AddDeviceAsync)} threw an exception: {ex}", nameof(AddDeviceAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Adding device: {device?.Id}", nameof(AddDeviceAsync));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<Device> GetDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            Logging.Enter(this, $"Getting device: {deviceId}", nameof(GetDeviceAsync));

            try
            {
                if (deviceId == null)
                {
                    throw new ArgumentNullException(nameof(deviceId));
                }

                using HttpRequestMessage request = HttpMessageHelper2.CreateRequest(HttpMethod.Put, GetRequestUri(deviceId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<Device>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetDeviceAsync)} threw an exception: {ex}", nameof(GetDeviceAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting device: {deviceId}", nameof(GetDeviceAsync));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="device"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<Device> UpdateDeviceAsync(Device device, CancellationToken cancellationToken = default)
        {
            return await UpdateDeviceAsync(device, false, cancellationToken);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="device"></param>
        /// <param name="forceUpdate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<Device> UpdateDeviceAsync(Device device, bool forceUpdate, CancellationToken cancellationToken = default)
        {
            Logging.Enter(this, $"Updating device: {device?.Id}", nameof(UpdateDeviceAsync));

            try
            {
                if (device == null)
                {
                    throw new ArgumentNullException(nameof(device));
                }

                if (string.IsNullOrWhiteSpace(device.ETag) && !forceUpdate)
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingDevice);
                }

                using HttpRequestMessage request = HttpMessageHelper2.CreateRequest(HttpMethod.Put, GetRequestUri(device.Id), _credentialProvider);
                request.Headers.Add(HttpRequestHeader.IfMatch.ToString(), device.ETag);

                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<Device>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(UpdateDeviceAsync)} threw an exception: {ex}", nameof(UpdateDeviceAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Updating device: {device?.Id}", nameof(UpdateDeviceAsync));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task RemoveDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentException(nameof(deviceId));
            }

            var device = new Device(deviceId);
            device.ETag = "*";
            await RemoveDeviceAsync(device, cancellationToken);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="device"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task RemoveDeviceAsync(Device device, CancellationToken cancellationToken = default)
        {
            Logging.Enter(this, $"Removing device: {device?.Id}", nameof(RemoveDeviceAsync));

            try
            {
                if (device == null)
                {
                    throw new ArgumentNullException(nameof(device));
                }

                if (device.ETag == null)
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingDevice);
                }

                using HttpRequestMessage request = HttpMessageHelper2.CreateRequest(HttpMethod.Delete, GetRequestUri(device.Id), _credentialProvider);
                request.Headers.Add(HttpRequestHeader.IfMatch.ToString(), device.ETag);

                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.NoContent, response);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(RemoveDeviceAsync)} threw an exception: {ex}", nameof(RemoveDeviceAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Removing device: {device?.Id}", nameof(RemoveDeviceAsync));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<Module> AddModuleAsync(Module module, CancellationToken cancellationToken = default)
        {
            Logging.Enter(this, $"Adding module: {module?.Id} to device: {module?.DeviceId}", nameof(AddModuleAsync));

            try
            {
                if (module == null)
                {
                    throw new ArgumentNullException(nameof(module));
                }

                using HttpRequestMessage request = HttpMessageHelper2.CreateRequest(HttpMethod.Put, GetModulesRequestUri(module.DeviceId, module.Id), _credentialProvider, module);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<Module>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(AddModuleAsync)} threw an exception: {ex}", nameof(AddModuleAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Adding module: {module?.Id} to device: {module?.DeviceId}", nameof(AddModuleAsync));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<Module> GetModuleAsync(string deviceId, string moduleId, CancellationToken cancellationToken = default)
        {
            Logging.Enter(this, $"Getting module: {moduleId} from device: {deviceId}", nameof(GetModuleAsync));

            try
            {
                if (string.IsNullOrEmpty(deviceId))
                {
                    throw new ArgumentException(nameof(deviceId));
                }

                if (string.IsNullOrEmpty(moduleId))
                {
                    throw new ArgumentException(nameof(moduleId));
                }

                using HttpRequestMessage request = HttpMessageHelper2.CreateRequest(HttpMethod.Put, GetModulesRequestUri(deviceId, moduleId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<Module>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetModuleAsync)} threw an exception: {ex}", nameof(GetModuleAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting module: {moduleId} from device: {deviceId}", nameof(GetModuleAsync));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<Module> UpdateModuleAsync(Module module, CancellationToken cancellationToken = default)
        {
            return await UpdateModuleAsync(module, false, cancellationToken);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="module"></param>
        /// <param name="forceUpdate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<Module> UpdateModuleAsync(Module module, bool forceUpdate, CancellationToken cancellationToken = default)
        {
            Logging.Enter(this, $"Updating module: {module?.Id} on device: {module?.DeviceId}", nameof(UpdateModuleAsync));

            try
            {
                if (module == null)
                {
                    throw new ArgumentNullException(nameof(module));
                }

                if (string.IsNullOrWhiteSpace(module.ETag) && !forceUpdate)
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingDevice);
                }

                using HttpRequestMessage request = HttpMessageHelper2.CreateRequest(HttpMethod.Put, GetModulesRequestUri(module.DeviceId, module.Id), _credentialProvider);

                request.Headers.Add(HttpRequestHeader.IfMatch.ToString(), module.ETag);

                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<Module>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(UpdateModuleAsync)} threw an exception: {ex}", nameof(UpdateModuleAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Updating module: {module?.Id} on device: {module?.DeviceId}", nameof(UpdateModuleAsync));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task RemoveModuleAsync(string deviceId, string moduleId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentException(nameof(deviceId));
            }

            if (string.IsNullOrEmpty(moduleId))
            {
                throw new ArgumentException(nameof(moduleId));
            }

            var module = new Module(deviceId, moduleId);
            module.ETag = "*";
            await RemoveModuleAsync(module, cancellationToken);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task RemoveModuleAsync(Module module, CancellationToken cancellationToken = default)
        {
            Logging.Enter(this, $"Removing module: {module?.Id} from device: {module?.DeviceId}", nameof(RemoveDeviceAsync));

            try
            {
                if (module == null)
                {
                    throw new ArgumentNullException(nameof(module));
                }

                if (module.ETag == null)
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingDevice);
                }

                using HttpRequestMessage request = HttpMessageHelper2.CreateRequest(HttpMethod.Delete, GetModulesRequestUri(module.DeviceId, module.Id), _credentialProvider);
                request.Headers.Add(HttpRequestHeader.IfMatch.ToString(), module.ETag);

                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.NoContent, response);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(RemoveDeviceAsync)} threw an exception: {ex}", nameof(RemoveDeviceAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Removing module: {module?.Id} from device: {module?.DeviceId}", nameof(RemoveDeviceAsync));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<Module>> GetModulesOnDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            Logging.Enter(this, $"Getting modules on device: {deviceId}", nameof(GetModulesOnDeviceAsync));

            try
            {
                if (string.IsNullOrEmpty(deviceId))
                {
                    throw new ArgumentException(nameof(deviceId));
                }

                using HttpRequestMessage request = HttpMessageHelper2.CreateRequest(HttpMethod.Get, GetModulesOnDeviceRequestUri(deviceId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<IEnumerable<Module>>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetModulesOnDeviceAsync)} threw an exception: {ex}", nameof(GetModulesOnDeviceAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting modules on device: {deviceId}", nameof(GetModulesOnDeviceAsync));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="devices"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<BulkRegistryOperationResult> AddDevicesAsync(IEnumerable<Device> devices, CancellationToken cancellationToken = default)
        {
            Logging.Enter(this, $"Adding {devices?.Count()} devices", nameof(AddDevicesAsync));

            try
            {
                IEnumerable<ExportImportDevice> exportImportDevices = GenerateExportImportDeviceListForBulkOperations(devices, ImportMode.Create);
                return await BulkDeviceOperationAsync(exportImportDevices, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(AddDevicesAsync)} threw an exception: {ex}", nameof(AddDevicesAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Adding {devices?.Count()} devices", nameof(AddDevicesAsync));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="devices"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<BulkRegistryOperationResult> UpdateDevicesAsync(IEnumerable<Device> devices, CancellationToken cancellationToken = default)
        {
            return await UpdateDevicesAsync(devices, false, cancellationToken);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="devices"></param>
        /// <param name="forceUpdate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<BulkRegistryOperationResult> UpdateDevicesAsync(IEnumerable<Device> devices, bool forceUpdate, CancellationToken cancellationToken = default)
        {
            Logging.Enter(this, $"Updating multiple devices: count: {devices?.Count()} - Force update: {forceUpdate}", nameof(UpdateDevicesAsync));

            try
            {
                ImportMode importMode = forceUpdate ? ImportMode.Update : ImportMode.UpdateIfMatchETag;
                IEnumerable<ExportImportDevice> exportImportDevices = GenerateExportImportDeviceListForBulkOperations(devices, importMode);
                return await BulkDeviceOperationAsync(exportImportDevices, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(UpdateDevicesAsync)} threw an exception: {ex}", nameof(UpdateDevicesAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Updating multiple devices: count: {devices?.Count()} - Force update: {forceUpdate}", nameof(UpdateDevicesAsync));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="devices"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<BulkRegistryOperationResult> RemoveDevicesAsync(IEnumerable<Device> devices, CancellationToken cancellationToken = default)
        {
            return await RemoveDevicesAsync(devices, false, cancellationToken);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="devices"></param>
        /// <param name="forceRemove"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<BulkRegistryOperationResult> RemoveDevicesAsync(IEnumerable<Device> devices, bool forceRemove, CancellationToken cancellationToken = default)
        {
            Logging.Enter(this, $"Removing devices : count: {devices?.Count()} - Force remove: {forceRemove}", nameof(RemoveDevicesAsync));

            try
            {
                ImportMode importMode = forceRemove ? ImportMode.Delete : ImportMode.DeleteIfMatchETag;
                IEnumerable<ExportImportDevice> exportImportDevices = GenerateExportImportDeviceListForBulkOperations(devices, importMode);
                return await BulkDeviceOperationAsync(exportImportDevices, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(RemoveDevicesAsync)} threw an exception: {ex}", nameof(RemoveDevicesAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Removing devices : count: {devices?.Count()} - Force remove: {forceRemove}", nameof(RemoveDevicesAsync));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="device"></param>
        /// <param name="twin"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<BulkRegistryOperationResult> AddDeviceWithTwinAsync(Device device, Twin twin, CancellationToken cancellationToken = default)
        {
            Logging.Enter(this, $"Adding device with twin: {device?.Id}", nameof(AddDeviceWithTwinAsync));

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
                Logging.Error(this, $"{nameof(AddDeviceWithTwinAsync)} threw an exception: {ex}", nameof(AddDeviceWithTwinAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Adding device with twin: {device?.Id}", nameof(AddDeviceWithTwinAsync));
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<RegistryStatistics> GetRegistryStatisticsAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Getting registry statistics", nameof(GetRegistryStatisticsAsync));

            try
            {
                using HttpRequestMessage request = HttpMessageHelper2.CreateRequest(HttpMethod.Get, GetStatisticsUri(), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<RegistryStatistics>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetRegistryStatisticsAsync)} threw an exception: {ex}", nameof(GetRegistryStatisticsAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting registry statistics", nameof(GetRegistryStatisticsAsync));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private static Uri GetRequestUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(DeviceRequestUriFormat.FormatInvariant(deviceId, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetModulesRequestUri(string deviceId, string moduleId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            moduleId = WebUtility.UrlEncode(moduleId);
            return new Uri(ModulesRequestUriFormat.FormatInvariant(deviceId, moduleId, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetModulesOnDeviceRequestUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(ModulesOnDeviceRequestUriFormat.FormatInvariant(deviceId, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetStatisticsUri()
        {
            return new Uri(StatisticsUriFormat, UriKind.Relative);
        }

        private static Uri GetBulkRequestUri()
        {
            return new Uri(DeviceRequestUriFormat.FormatInvariant(string.Empty, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }

        private static IEnumerable<ExportImportDevice> GenerateExportImportDeviceListForBulkOperations(IEnumerable<Device> devices, ImportMode importMode)
        {
            if (devices == null)
            {
                throw new ArgumentNullException(nameof(devices));
            }

            if (!devices.Any())
            {
                throw new ArgumentException($"Parameter {nameof(devices)} cannot be empty.");
            }

            var exportImportDeviceList = new List<ExportImportDevice>(devices.Count());
            foreach (Device device in devices)
            {
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
            Logging.Enter(this, $"Performing bulk device operation on : {devices?.Count()} devices.", nameof(BulkDeviceOperationAsync));
            try
            {
                if (devices == null)
                {
                    throw new ArgumentNullException(nameof(devices));
                }

                using HttpRequestMessage request = HttpMessageHelper2.CreateRequest(HttpMethod.Post, GetBulkRequestUri(), _credentialProvider, devices);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<BulkRegistryOperationResult>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(BulkDeviceOperationAsync)} threw an exception: {ex}", nameof(BulkDeviceOperationAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Performing bulk device operation on : {devices?.Count()} devices.", nameof(BulkDeviceOperationAsync));
            }
        }
    }
}
