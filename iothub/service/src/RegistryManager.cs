// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

#if !NET451

using Azure;
using Azure.Core;

#endif

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains methods that services can use to perform create, remove, update and delete operations on devices.
    /// </summary>
    /// <remarks>
    /// For more information, see <see href="https://github.com/Azure/azure-iot-sdk-csharp#iot-hub-service-sdk"/>
    /// </remarks>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "Cannot change parameter names as it is considered a breaking change.")]
    public class RegistryManager : IDisposable
    {
        private const string AdminUriFormat = "/$admin/{0}?{1}";
        private const string RequestUriFormat = "/devices/{0}?{1}";
        private const string JobsUriFormat = "/jobs{0}?{1}";
        private const string StatisticsUriFormat = "/statistics/devices?" + ClientApiVersionHelper.ApiVersionQueryString;
        private const string DevicesRequestUriFormat = "/devices/?top={0}&{1}";
        private const string DevicesQueryUriFormat = "/devices/query?" + ClientApiVersionHelper.ApiVersionQueryString;
        private const string WildcardEtag = "*";

        private const string ContinuationTokenHeader = "x-ms-continuation";
        private const string PageSizeHeader = "x-ms-max-item-count";

        private const string TwinUriFormat = "/twins/{0}?{1}";

        private const string ModulesRequestUriFormat = "/devices/{0}/modules/{1}?{2}";
        private const string ModulesOnDeviceRequestUriFormat = "/devices/{0}/modules?{1}";
        private const string ModuleTwinUriFormat = "/twins/{0}/modules/{1}?{2}";

        private const string ConfigurationRequestUriFormat = "/configurations/{0}?{1}";
        private const string ConfigurationsRequestUriFormat = "/configurations/?top={0}&{1}";

        private const string ApplyConfigurationOnDeviceUriFormat = "/devices/{0}/applyConfigurationContent?" + ClientApiVersionHelper.ApiVersionQueryString;

        private const string DeviceGroupUriFormat = "/deviceGroups/{0}?" + ClientApiVersionHelper.MqttApiVersionQueryString;
        private const string DeviceGroupsUriFormat = "/deviceGroups?" + ClientApiVersionHelper.MqttApiVersionQueryString;

        private static readonly TimeSpan s_regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);

        private static readonly Regex s_deviceIdRegex = new Regex(
            @"^[A-Za-z0-9\-:.+%_#*?!(),=@;$']{1,128}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            s_regexTimeoutMilliseconds);

        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromSeconds(100);
        private static readonly TimeSpan s_defaultGetDevicesOperationTimeout = TimeSpan.FromSeconds(120);

        private readonly string _iotHubName;
        private IHttpClientHelper _httpClientHelper;

        /// <summary>
        /// Creates an instance of RegistryManager, provided for unit testing purposes only.
        /// </summary>
        public RegistryManager()
        {
        }

        internal RegistryManager(IotHubConnectionProperties connectionProperties, HttpTransportSettings transportSettings)
        {
            _iotHubName = connectionProperties.IotHubName;
            _httpClientHelper = new HttpClientHelper(
                connectionProperties.HttpsEndpoint,
                connectionProperties,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                s_defaultOperationTimeout,
                transportSettings.Proxy,
                transportSettings.ConnectionLeaseTimeoutMilliseconds);
        }

        // internal test helper
        internal RegistryManager(string iotHubName, IHttpClientHelper httpClientHelper)
        {
            _iotHubName = iotHubName;
            _httpClientHelper = httpClientHelper ?? throw new ArgumentNullException(nameof(httpClientHelper));
        }

        /// <summary>
        /// Creates RegistryManager from an IoT hub connection string.
        /// </summary>
        /// <param name="connectionString">The IoT hub connection string.</param>
        /// <returns>A RegistryManager instance.</returns>
        public static RegistryManager CreateFromConnectionString(string connectionString)
        {
            return CreateFromConnectionString(connectionString, new HttpTransportSettings());
        }

        /// <summary>
        /// Creates an instance of RegistryManager, authenticating using an IoT hub connection string, and specifying
        /// HTTP transport settings.
        /// </summary>
        /// <param name="connectionString">The IoT hub connection string.</param>
        /// <param name="transportSettings">The HTTP transport settings.</param>
        /// <returns>A RegistryManager instance.</returns>
        public static RegistryManager CreateFromConnectionString(string connectionString, HttpTransportSettings transportSettings)
        {
            if (transportSettings == null)
            {
                throw new ArgumentNullException(nameof(transportSettings), "The HTTP transport settings cannot be null.");
            }
            TlsVersions.Instance.SetLegacyAcceptableVersions();

            var iotHubConnectionString = IotHubConnectionString.Parse(connectionString);
            return new RegistryManager(iotHubConnectionString, transportSettings);
        }

#if !NET451

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
        public static RegistryManager Create(
            string hostName,
            TokenCredential credential,
            HttpTransportSettings transportSettings = default)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentNullException($"{nameof(hostName)},  Parameter cannot be null or empty");
            }

            if (credential == null)
            {
                throw new ArgumentNullException($"{nameof(credential)},  Parameter cannot be null");
            }

            var tokenCredentialProperties = new IotHubTokenCrendentialProperties(hostName, credential);
            return new RegistryManager(tokenCredentialProperties, transportSettings ?? new HttpTransportSettings());
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
        public static RegistryManager Create(
            string hostName,
            AzureSasCredential credential,
            HttpTransportSettings transportSettings = default)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentNullException($"{nameof(hostName)},  Parameter cannot be null or empty");
            }

            if (credential == null)
            {
                throw new ArgumentNullException($"{nameof(credential)},  Parameter cannot be null");
            }

            var sasCredentialProperties = new IotHubSasCredentialProperties(hostName, credential);
            return new RegistryManager(sasCredentialProperties, transportSettings ?? new HttpTransportSettings());
        }

#endif

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
        {
            if (disposing && _httpClientHelper != null)
            {
                _httpClientHelper.Dispose();
                _httpClientHelper = null;
            }
        }

        /// <summary>
        /// Explicitly open the RegistryManager instance.
        /// </summary>
        public virtual Task OpenAsync()
        {
            return TaskHelpers.CompletedTask;
        }

        /// <summary>
        /// Closes the RegistryManager instance and disposes its resources.
        /// </summary>
        public virtual Task CloseAsync()
        {
            return TaskHelpers.CompletedTask;
        }

        /// <summary>
        /// Register a new device with the system
        /// </summary>
        /// <param name="device">The Device object being registered.</param>
        /// <returns>The Device object with the generated keys and ETags.</returns>
        public virtual Task<Device> AddDeviceAsync(Device device)
        {
            return AddDeviceAsync(device, CancellationToken.None);
        }

        /// <summary>
        /// Register a new device with the system
        /// </summary>
        /// <param name="device">The Device object being registered.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Device object with the generated keys and ETags.</returns>
        public virtual Task<Device> AddDeviceAsync(Device device, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Adding device: {device?.Id}", nameof(AddDeviceAsync));

            try
            {
                EnsureInstanceNotClosed();

                ValidateDeviceId(device);

                if (!string.IsNullOrEmpty(device.ETag))
                {
                    throw new ArgumentException(ApiResources.ETagSetWhileRegisteringDevice);
                }

                ValidateDeviceAuthentication(device.Authentication, device.Id);

                NormalizeDevice(device);

                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.PreconditionFailed,
                    async responseMessage => new PreconditionFailedException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))
                }
            };

                return _httpClientHelper.PutAsync(GetRequestUri(device.Id), device, PutOperationType.CreateEntity, errorMappingOverrides, cancellationToken);
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
        /// Register a new module with device in the system
        /// </summary>
        /// <param name="module">The Module object being registered.</param>
        /// <returns>The Module object with the generated keys and ETags.</returns>
        public virtual Task<Module> AddModuleAsync(Module module)
        {
            return AddModuleAsync(module, CancellationToken.None);
        }

        /// <summary>
        /// Register a new module with device in the system
        /// </summary>
        /// <param name="module">The Module object being registered.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Module object with the generated keys and ETags.</returns>
        public virtual Task<Module> AddModuleAsync(Module module, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Adding module: {module?.Id}", nameof(AddModuleAsync));

            try
            {
                EnsureInstanceNotClosed();

                ValidateModuleId(module);

                if (!string.IsNullOrEmpty(module.ETag))
                {
                    throw new ArgumentException(ApiResources.ETagSetWhileRegisteringDevice);
                }

                ValidateDeviceAuthentication(module.Authentication, module.DeviceId);

                // auto generate keys if not specified
                if (module.Authentication == null)
                {
                    module.Authentication = new AuthenticationMechanism();
                }

                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
                {
                    {
                        HttpStatusCode.PreconditionFailed,
                        async responseMessage => new PreconditionFailedException(
                            await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))
                    },
                    {
                        HttpStatusCode.Conflict,
                        async responseMessage => new ModuleAlreadyExistsException(
                            await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))
                    },
                    {
                        HttpStatusCode.RequestEntityTooLarge,
                        async responseMessage => new TooManyModulesOnDeviceException(
                            await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))
                    }
                };

                return _httpClientHelper.PutAsync(GetModulesRequestUri(module.DeviceId, module.Id), module, PutOperationType.CreateEntity, errorMappingOverrides, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(AddModuleAsync)} threw an exception: {ex}", nameof(AddModuleAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Adding module: {module?.Id}", nameof(AddModuleAsync));
            }
        }

        /// <summary>
        /// Adds a Device with Twin information
        /// </summary>
        /// <param name="device">The device to add.</param>
        /// <param name="twin">The twin information for the device being added.</param>
        /// <returns>The result of the add operation.</returns>
        public virtual Task<BulkRegistryOperationResult> AddDeviceWithTwinAsync(Device device, Twin twin)
        {
            return AddDeviceWithTwinAsync(device, twin, CancellationToken.None);
        }

        /// <summary>
        /// Adds a Device with Twin information
        /// </summary>
        /// <param name="device">The device to add.</param>
        /// <param name="twin">The twin information for the device being added.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The result of the add operation.</returns>
        public virtual Task<BulkRegistryOperationResult> AddDeviceWithTwinAsync(Device device, Twin twin, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Adding device with twin: {device?.Id}", nameof(AddDeviceWithTwinAsync));

            try
            {
                ValidateDeviceId(device);
                if (!string.IsNullOrWhiteSpace(device.ETag))
                {
                    throw new ArgumentException(ApiResources.ETagSetWhileRegisteringDevice);
                }
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

                return BulkDeviceOperationsAsync<BulkRegistryOperationResult>(
                   exportImportDeviceList,
                   ClientApiVersionHelper.ApiVersionQueryString,
                   cancellationToken);
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
        /// Register a list of new devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being registered.</param>
        /// <returns>Returns a string array of error messages.</returns>
        [Obsolete("Use AddDevices2Async")]
        public virtual Task<string[]> AddDevicesAsync(IEnumerable<Device> devices)
        {
            return AddDevicesAsync(devices, CancellationToken.None);
        }

        /// <summary>
        /// Register a list of new devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being registered.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>Returns a string array of error messages.</returns>
        [Obsolete("Use AddDevices2Async")]
        public virtual Task<string[]> AddDevicesAsync(IEnumerable<Device> devices, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Adding {devices?.Count()} devices", nameof(AddDevicesAsync));

            try
            {
                return BulkDeviceOperationsAsync<string[]>(
                    GenerateExportImportDeviceListForBulkOperations(devices, ImportMode.Create),
                    ClientApiVersionHelper.ApiVersionQueryString,
                    cancellationToken);
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
        /// Register a list of new devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being registered.</param>
        /// <returns>Returns a BulkRegistryOperationResult object.</returns>
        public virtual Task<BulkRegistryOperationResult> AddDevices2Async(IEnumerable<Device> devices)
        {
            return AddDevices2Async(devices, CancellationToken.None);
        }

        /// <summary>
        /// Register a list of new devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being registered.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>Returns a BulkRegistryOperationResult object.</returns>
        public virtual Task<BulkRegistryOperationResult> AddDevices2Async(IEnumerable<Device> devices, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Adding {devices?.Count()} devices", nameof(AddDevices2Async));

            try
            {
                return BulkDeviceOperationsAsync<BulkRegistryOperationResult>(
                    GenerateExportImportDeviceListForBulkOperations(devices, ImportMode.Create),
                    ClientApiVersionHelper.ApiVersionQueryString,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(AddDevices2Async)} threw an exception: {ex}", nameof(AddDevices2Async));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Adding {devices?.Count()} devices", nameof(AddDevices2Async));
            }
        }

        /// <summary>
        /// Update the mutable fields of the device registration
        /// </summary>
        /// <param name="device">The Device object with updated fields.</param>
        /// <returns>The Device object with updated ETag.</returns>
        public virtual Task<Device> UpdateDeviceAsync(Device device)
        {
            return UpdateDeviceAsync(device, CancellationToken.None);
        }

        /// <summary>
        /// Update the mutable fields of the device registration
        /// </summary>
        /// <param name="device">The Device object with updated fields.</param>
        /// <param name="forceUpdate">Forces the device object to be replaced without regard for an ETag match.</param>
        /// <returns>The Device object with updated ETag.</returns>
        public virtual Task<Device> UpdateDeviceAsync(Device device, bool forceUpdate)
        {
            return UpdateDeviceAsync(device, forceUpdate, CancellationToken.None);
        }

        /// <summary>
        /// Update the mutable fields of the device registration
        /// </summary>
        /// <param name="device">The Device object with updated fields.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Device object with updated ETag.</returns>
        public virtual Task<Device> UpdateDeviceAsync(Device device, CancellationToken cancellationToken)
        {
            return UpdateDeviceAsync(device, false, cancellationToken);
        }

        /// <summary>
        /// Update the mutable fields of the device registration
        /// </summary>
        /// <param name="device">The Device object with updated fields.</param>
        /// <param name="forceUpdate">Forces the device object to be replaced even if it was updated since it was retrieved last time.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Device object with updated ETags.</returns>
        public virtual Task<Device> UpdateDeviceAsync(Device device, bool forceUpdate, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Updating device: {device?.Id}", nameof(UpdateDeviceAsync));
            try
            {
                EnsureInstanceNotClosed();

                ValidateDeviceId(device);

                if (string.IsNullOrWhiteSpace(device.ETag) && !forceUpdate)
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingDevice);
                }

                ValidateDeviceAuthentication(device.Authentication, device.Id);

                NormalizeDevice(device);

                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>()
                {
                    { HttpStatusCode.PreconditionFailed, async (responseMessage) => new PreconditionFailedException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false)) },
                    {
                        HttpStatusCode.NotFound, async responseMessage =>
                        {
                            string responseContent = await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false);
                            return (Exception)new DeviceNotFoundException(responseContent, (Exception)null);
                        }
                    }
                };

                PutOperationType operationType = forceUpdate ? PutOperationType.ForceUpdateEntity : PutOperationType.UpdateEntity;

                return _httpClientHelper.PutAsync(GetRequestUri(device.Id), device, operationType, errorMappingOverrides, cancellationToken);
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
        /// Update the mutable fields of the module registration
        /// </summary>
        /// <param name="module">The Module object with updated fields.</param>
        /// <returns>The Module object with updated ETags.</returns>
        public virtual Task<Module> UpdateModuleAsync(Module module)
        {
            return UpdateModuleAsync(module, CancellationToken.None);
        }

        /// <summary>
        /// Update the mutable fields of the module registration
        /// </summary>
        /// <param name="module">The Module object with updated fields.</param>
        /// <param name="forceUpdate">Forces the device object to be replaced without regard for an ETag match.</param>
        /// <returns>The Module object with updated ETags.</returns>
        public virtual Task<Module> UpdateModuleAsync(Module module, bool forceUpdate)
        {
            return UpdateModuleAsync(module, forceUpdate, CancellationToken.None);
        }

        /// <summary>
        /// Update the mutable fields of the module registration
        /// </summary>
        /// <param name="module">The Module object with updated fields.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Module object with updated ETags.</returns>
        public virtual Task<Module> UpdateModuleAsync(Module module, CancellationToken cancellationToken)
        {
            return UpdateModuleAsync(module, false, CancellationToken.None);
        }

        /// <summary>
        /// Update the mutable fields of the module registration
        /// </summary>
        /// <param name="module">The Module object with updated fields.</param>
        /// <param name="forceUpdate">Forces the module object to be replaced even if it was updated since it was retrieved last time.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Module object with updated ETags.</returns>
        public virtual Task<Module> UpdateModuleAsync(Module module, bool forceUpdate, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Updating module: {module?.Id}", nameof(UpdateModuleAsync));

            try
            {
                EnsureInstanceNotClosed();

                ValidateModuleId(module);

                if (string.IsNullOrWhiteSpace(module.ETag) && !forceUpdate)
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingDevice);
                }

                ValidateDeviceAuthentication(module.Authentication, module.DeviceId);

                // auto generate keys if not specified
                if (module.Authentication == null)
                {
                    module.Authentication = new AuthenticationMechanism();
                }

                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>()
                {
                    { HttpStatusCode.PreconditionFailed, async (responseMessage) => new PreconditionFailedException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false)) },
                    {
                        HttpStatusCode.NotFound, async responseMessage =>
                        {
                            string responseContent = await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false);
                            return new ModuleNotFoundException(responseContent, (Exception)null);
                        }
                    }
                };

                PutOperationType operationType = forceUpdate ? PutOperationType.ForceUpdateEntity : PutOperationType.UpdateEntity;

                return _httpClientHelper.PutAsync(GetModulesRequestUri(module.DeviceId, module.Id), module, operationType, errorMappingOverrides, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(UpdateModuleAsync)} threw an exception: {ex}", nameof(UpdateModuleAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Updating module: {module?.Id}", nameof(UpdateModuleAsync));
            }
        }

        /// <summary>
        /// Update a list of devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being updated.</param>
        /// <returns>Returns a string array of error messages.</returns>
        [Obsolete("Use UpdateDevices2Async")]
        public virtual Task<string[]> UpdateDevicesAsync(IEnumerable<Device> devices)
        {
            return UpdateDevicesAsync(devices, false, CancellationToken.None);
        }

        /// <summary>
        /// Update a list of devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being updated.</param>
        /// <param name="forceUpdate">Forces the device object to be replaced even if it was updated since it was retrieved last time.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>Returns a string array of error messages.</returns>
        [Obsolete("Use UpdateDevices2Async")]
        public virtual Task<string[]> UpdateDevicesAsync(IEnumerable<Device> devices, bool forceUpdate, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Updating multiple devices: count: {devices?.Count()}", nameof(UpdateDevicesAsync));

            try
            {
                return BulkDeviceOperationsAsync<string[]>(
                GenerateExportImportDeviceListForBulkOperations(devices, forceUpdate ? ImportMode.Update : ImportMode.UpdateIfMatchETag),
                ClientApiVersionHelper.ApiVersionQueryString,
                cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(UpdateDevicesAsync)} threw an exception: {ex}", nameof(UpdateDevicesAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Updating multiple devices: count: {devices?.Count()}", nameof(UpdateDevicesAsync));
            }
        }

        /// <summary>
        /// Update a list of devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being updated.</param>
        /// <returns>Returns a BulkRegistryOperationResult object.</returns>
        public virtual Task<BulkRegistryOperationResult> UpdateDevices2Async(IEnumerable<Device> devices)
        {
            return UpdateDevices2Async(devices, false, CancellationToken.None);
        }

        /// <summary>
        /// Update a list of devices with the system
        /// </summary>
        /// <param name="devices">The Device objects being updated.</param>
        /// <param name="forceUpdate">Forces the device object to be replaced even if it was updated since it was retrieved last time.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>Returns a BulkRegistryOperationResult object.</returns>
        public virtual Task<BulkRegistryOperationResult> UpdateDevices2Async(IEnumerable<Device> devices, bool forceUpdate, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Updating multiple devices: count: {devices?.Count()} - Force update: {forceUpdate}", nameof(UpdateDevices2Async));

            try
            {
                return BulkDeviceOperationsAsync<BulkRegistryOperationResult>(
                    GenerateExportImportDeviceListForBulkOperations(devices, forceUpdate ? ImportMode.Update : ImportMode.UpdateIfMatchETag),
                    ClientApiVersionHelper.ApiVersionQueryString,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(UpdateDevices2Async)} threw an exception: {ex}", nameof(UpdateDevices2Async));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Updating multiple devices: count: {devices?.Count()} - Force update: {forceUpdate}", nameof(UpdateDevices2Async));
            }
        }

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="deviceId">The id of the device being deleted.</param>
        public virtual Task RemoveDeviceAsync(string deviceId)
        {
            return RemoveDeviceAsync(deviceId, CancellationToken.None);
        }

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="deviceId">The id of the device being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual Task RemoveDeviceAsync(string deviceId, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Removing device: {deviceId}", nameof(RemoveDeviceAsync));
            try
            {
                EnsureInstanceNotClosed();

                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrWhitespace, "deviceId"));
                }

                // use wild-card ETag
                var eTag = new ETagHolder { ETag = "*" };
                return RemoveDeviceAsync(deviceId, eTag, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(RemoveDeviceAsync)} threw an exception: {ex}", nameof(RemoveDeviceAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Removing device: {deviceId}", nameof(RemoveDeviceAsync));
            }
        }

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="device">The device being deleted.</param>
        public virtual Task RemoveDeviceAsync(Device device)
        {
            return RemoveDeviceAsync(device, CancellationToken.None);
        }

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="device">The device being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual Task RemoveDeviceAsync(Device device, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Removing device: {device?.Id}", nameof(RemoveDeviceAsync));

            try
            {
                EnsureInstanceNotClosed();

                ValidateDeviceId(device);

                return string.IsNullOrWhiteSpace(device.ETag)
                    ? throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingDevice)
                    : RemoveDeviceAsync(device.Id, device, cancellationToken);
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
        /// Deletes a previously registered module from device in the system.
        /// </summary>
        /// <param name="deviceId">The id of the device being deleted.</param>
        /// <param name="moduleId">The id of the moduleId being deleted.</param>
        public virtual Task RemoveModuleAsync(string deviceId, string moduleId)
        {
            return RemoveModuleAsync(deviceId, moduleId, CancellationToken.None);
        }

        /// <summary>
        /// Deletes a previously registered module from device in the system.
        /// </summary>
        /// <param name="deviceId">The id of the device being deleted.</param>
        /// <param name="moduleId">The id of the moduleId being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual Task RemoveModuleAsync(string deviceId, string moduleId, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Removing module: device Id:{deviceId} moduleId: {moduleId}", nameof(RemoveDeviceAsync));

            try
            {
                EnsureInstanceNotClosed();

                if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrEmpty(moduleId))
                {
                    throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrWhitespace, "deviceId"));
                }

                // use wild-card ETag
                var eTag = new ETagHolder { ETag = "*" };
                return RemoveDeviceModuleAsync(deviceId, moduleId, eTag, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(RemoveModuleAsync)} threw an exception: {ex}", nameof(RemoveModuleAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Removing module: device Id:{deviceId} moduleId: {moduleId}", nameof(RemoveModuleAsync));
            }
        }

        /// <summary>
        /// Deletes a previously registered module from device in the system.
        /// </summary>
        /// <param name="module">The module being deleted.</param>
        public virtual Task RemoveModuleAsync(Module module)
        {
            return RemoveModuleAsync(module, CancellationToken.None);
        }

        /// <summary>
        /// Deletes a previously registered module from device in the system.
        /// </summary>
        /// <param name="module">The module being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual Task RemoveModuleAsync(Module module, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Removing module: device Id:{module?.DeviceId} moduleId: {module?.Id}", nameof(RemoveModuleAsync));

            try
            {
                EnsureInstanceNotClosed();

                ValidateModuleId(module);

                return string.IsNullOrWhiteSpace(module.ETag)
                    ? throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingDevice)
                    : RemoveDeviceModuleAsync(module.DeviceId, module.Id, module, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(RemoveModuleAsync)} threw an exception: {ex}", nameof(RemoveModuleAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Removing module: device Id:{module?.DeviceId} moduleId: {module?.Id}", nameof(RemoveModuleAsync));
            }
        }

        /// <summary>
        /// Deletes a list of previously registered devices from the system.
        /// </summary>
        /// <param name="devices">The devices being deleted.</param>
        [Obsolete("Use RemoveDevices2Async")]
        public virtual Task<string[]> RemoveDevicesAsync(IEnumerable<Device> devices)
        {
            return RemoveDevicesAsync(devices, false, CancellationToken.None);
        }

        /// <summary>
        /// Deletes a list of previously registered devices from the system.
        /// </summary>
        /// <param name="devices">The devices being deleted.</param>
        /// <param name="forceRemove">Forces the device object to be removed without regard for an ETag match.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        [Obsolete("Use RemoveDevices2Async")]
        public virtual Task<string[]> RemoveDevicesAsync(IEnumerable<Device> devices, bool forceRemove, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Removing devices : count: {devices?.Count()} - Force remove: {forceRemove}", nameof(RemoveDevicesAsync));
            try
            {
                return BulkDeviceOperationsAsync<string[]>(
                   GenerateExportImportDeviceListForBulkOperations(devices, forceRemove ? ImportMode.Delete : ImportMode.DeleteIfMatchETag),
                   ClientApiVersionHelper.ApiVersionQueryString,
                   cancellationToken);
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
        /// Deletes a list of previously registered devices from the system.
        /// </summary>
        /// <param name="devices">The devices being deleted.</param>
        /// <returns>Returns a BulkRegistryOperationResult object.</returns>
        public virtual Task<BulkRegistryOperationResult> RemoveDevices2Async(IEnumerable<Device> devices)
        {
            return RemoveDevices2Async(devices, false, CancellationToken.None);
        }

        /// <summary>
        /// Deletes a list of previously registered devices from the system.
        /// </summary>
        /// <param name="devices">The devices being deleted.</param>
        /// <param name="forceRemove">Forces the device object to be removed even if it was updated since it was retrieved last time.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>Returns a BulkRegistryOperationResult object.</returns>
        public virtual Task<BulkRegistryOperationResult> RemoveDevices2Async(IEnumerable<Device> devices, bool forceRemove, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Removing devices : count: {devices?.Count()} - Force remove: {forceRemove}", nameof(RemoveDevices2Async));

            try
            {
                return BulkDeviceOperationsAsync<BulkRegistryOperationResult>(
                    GenerateExportImportDeviceListForBulkOperations(devices, forceRemove ? ImportMode.Delete : ImportMode.DeleteIfMatchETag),
                    ClientApiVersionHelper.ApiVersionQueryString,
                    cancellationToken);
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
        /// Gets usage statistics for the IoT Hub.
        /// </summary>
        public virtual Task<RegistryStatistics> GetRegistryStatisticsAsync()
        {
            return GetRegistryStatisticsAsync(CancellationToken.None);
        }

        /// <summary>
        /// Gets usage statistics for the IoT Hub.
        /// </summary>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual Task<RegistryStatistics> GetRegistryStatisticsAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Getting registry statistics", nameof(GetRegistryStatisticsAsync));

            try
            {
                EnsureInstanceNotClosed();
                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
                {
                    { HttpStatusCode.NotFound, responseMessage => Task.FromResult((Exception)new IotHubNotFoundException(_iotHubName)) }
                };

                return _httpClientHelper.GetAsync<RegistryStatistics>(GetStatisticsUri(), errorMappingOverrides, null, cancellationToken);
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

        /// <summary>
        /// Retrieves the specified Device object.
        /// </summary>
        /// <param name="deviceId">The id of the device being retrieved.</param>
        /// <returns>The Device object.</returns>
        public virtual Task<Device> GetDeviceAsync(string deviceId)
        {
            return GetDeviceAsync(deviceId, CancellationToken.None);
        }

        /// <summary>
        /// Retrieves the specified Device object.
        /// </summary>
        /// <param name="deviceId">The id of the device being retrieved.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Device object.</returns>
        public virtual Task<Device> GetDeviceAsync(string deviceId, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Getting device: {deviceId}", nameof(GetDeviceAsync));
            try
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrWhitespace, "deviceId"));
                }

                EnsureInstanceNotClosed();
                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>()
                {
                    { HttpStatusCode.NotFound, async responseMessage => new DeviceNotFoundException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false)) }
                };

                return _httpClientHelper.GetAsync<Device>(GetRequestUri(deviceId), errorMappingOverrides, null, false, cancellationToken);
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
        /// Retrieves the specified Module object.
        /// </summary>
        /// <param name="deviceId">The id of the device being retrieved.</param>
        /// <param name="moduleId">The id of the module being retrieved.</param>
        /// <returns>The Module object.</returns>
        public virtual Task<Module> GetModuleAsync(string deviceId, string moduleId)
        {
            return GetModuleAsync(deviceId, moduleId, CancellationToken.None);
        }

        /// <summary>
        /// Retrieves the specified Module object.
        /// </summary>
        /// <param name="deviceId">The id of the device being retrieved.</param>
        /// <param name="moduleId">The id of the module being retrieved.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Module object.</returns>
        public virtual Task<Module> GetModuleAsync(string deviceId, string moduleId, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Getting module: device Id: {deviceId} - module Id: {moduleId}", nameof(GetModuleAsync));

            try
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrWhitespace, "deviceId"));
                }

                if (string.IsNullOrWhiteSpace(moduleId))
                {
                    throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrWhitespace, "moduleId"));
                }

                EnsureInstanceNotClosed();
                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
                {
                    {
                        HttpStatusCode.NotFound,
                        responseMessage => Task.FromResult<Exception>(new ModuleNotFoundException(deviceId, moduleId))
                    },
                };

                return _httpClientHelper.GetAsync<Module>(GetModulesRequestUri(deviceId, moduleId), errorMappingOverrides, null, false, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetModuleAsync)} threw an exception: {ex}", nameof(GetModuleAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting module: device Id: {deviceId} - module Id: {moduleId}", nameof(GetModuleAsync));
            }
        }

        /// <summary>
        /// Retrieves the module identities on device
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <returns>List of modules on device.</returns>
        public virtual Task<IEnumerable<Module>> GetModulesOnDeviceAsync(string deviceId)
        {
            return GetModulesOnDeviceAsync(deviceId, CancellationToken.None);
        }

        /// <summary>
        /// Retrieves the module identities on device
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>List of modules on device.</returns>
        public virtual Task<IEnumerable<Module>> GetModulesOnDeviceAsync(string deviceId, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Getting module on device: {deviceId}", nameof(GetModulesOnDeviceAsync));

            try
            {
                EnsureInstanceNotClosed();

                return _httpClientHelper.GetAsync<IEnumerable<Module>>(
                    GetModulesOnDeviceRequestUri(deviceId),
                    null,
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetModulesOnDeviceAsync)} threw an exception: {ex}", nameof(GetModulesOnDeviceAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting module on device: {deviceId}", nameof(GetModulesOnDeviceAsync));
            }
        }

        /// <summary>
        /// Retrieves specified number of devices from every IoT Hub partition.
        /// This is an approximation and not a definitive list. Results are not ordered.
        /// </summary>
        /// <returns>The list of devices.</returns>
        [Obsolete("Use CreateQuery(\"select * from devices\", pageSize);")]
        public virtual Task<IEnumerable<Device>> GetDevicesAsync(int maxCount)
        {
            return GetDevicesAsync(maxCount, CancellationToken.None);
        }

        /// <summary>
        /// Retrieves specified number of devices from every IoT hub partition.
        /// This is an approximation and not a definitive list. Results are not ordered.
        /// </summary>
        /// <returns>The list of devices.</returns>
        [Obsolete("Use CreateQuery(\"select * from devices\", pageSize);")]
        public virtual Task<IEnumerable<Device>> GetDevicesAsync(int maxCount, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Getting devices - max count: {maxCount}", nameof(GetDevicesAsync));

            try
            {
                EnsureInstanceNotClosed();

                return _httpClientHelper.GetAsync<IEnumerable<Device>>(
                    GetDevicesRequestUri(maxCount),
                    s_defaultGetDevicesOperationTimeout,
                    null,
                    null,
                    true,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetDevicesAsync)} threw an exception: {ex}", nameof(GetDevicesAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting devices - max count: {maxCount}", nameof(GetDevicesAsync));
            }
        }

        /// <summary>
        /// Retrieves a handle through which a result for a given query can be fetched.
        /// </summary>
        /// <param name="sqlQueryString">The SQL query.</param>
        /// <returns>A handle used to fetch results for a SQL query.</returns>
        public virtual IQuery CreateQuery(string sqlQueryString)
        {
            return CreateQuery(sqlQueryString, null);
        }

        /// <summary>
        /// Retrieves a handle through which a result for a given query can be fetched.
        /// </summary>
        /// <param name="sqlQueryString">The SQL query.</param>
        /// <param name="pageSize">The maximum number of items per page.</param>
        /// <returns>A handle used to fetch results for a SQL query.</returns>
        public virtual IQuery CreateQuery(string sqlQueryString, int? pageSize)
        {
            Logging.Enter(this, $"Creating query", nameof(CreateQuery));
            try
            {
                return new Query((token) => ExecuteQueryAsync(
                    sqlQueryString,
                    pageSize,
                    token,
                    CancellationToken.None));
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(CreateQuery)} threw an exception: {ex}", nameof(CreateQuery));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Creating query", nameof(CreateQuery));
            }
        }

        /// <summary>
        /// Copies registered device data to a set of blobs in a specific container in a storage account.
        /// </summary>
        /// <param name="storageAccountConnectionString">ConnectionString to the destination StorageAccount.</param>
        /// <param name="containerName">Destination blob container name.</param>
        public virtual Task ExportRegistryAsync(string storageAccountConnectionString, string containerName)
        {
            return ExportRegistryAsync(storageAccountConnectionString, containerName, CancellationToken.None);
        }

        /// <summary>
        /// Copies registered device data to a set of blobs in a specific container in a storage account.
        /// </summary>
        /// <param name="storageAccountConnectionString">ConnectionString to the destination StorageAccount.</param>
        /// <param name="containerName">Destination blob container name.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        public virtual Task ExportRegistryAsync(string storageAccountConnectionString, string containerName, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Exporting registry", nameof(ExportRegistryAsync));
            try
            {
                EnsureInstanceNotClosed();

                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
                {
                    { HttpStatusCode.NotFound, responseMessage => Task.FromResult((Exception)new IotHubNotFoundException(_iotHubName)) }
                };

                return _httpClientHelper.PostAsync(
                    GetAdminUri("exportRegistry"),
                    new ExportImportRequest
                    {
                        ContainerName = containerName,
                        StorageConnectionString = storageAccountConnectionString,
                    },
                    errorMappingOverrides,
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(ExportRegistryAsync)} threw an exception: {ex}", nameof(ExportRegistryAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Exporting registry", nameof(ExportRegistryAsync));
            }
        }

        /// <summary>
        /// Imports registered device data from a set of blobs in a specific container in a storage account.
        /// </summary>
        /// <param name="storageAccountConnectionString">ConnectionString to the source StorageAccount.</param>
        /// <param name="containerName">Source blob container name.</param>
        public virtual Task ImportRegistryAsync(string storageAccountConnectionString, string containerName)
        {
            return ImportRegistryAsync(storageAccountConnectionString, containerName, CancellationToken.None);
        }

        /// <summary>
        /// Imports registered device data from a set of blobs in a specific container in a storage account.
        /// </summary>
        /// <param name="storageAccountConnectionString">ConnectionString to the source StorageAccount.</param>
        /// <param name="containerName">Source blob container name.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        public virtual Task ImportRegistryAsync(string storageAccountConnectionString, string containerName, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Importing registry", nameof(ImportRegistryAsync));

            try
            {
                EnsureInstanceNotClosed();

                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
                {
                    { HttpStatusCode.NotFound, responseMessage => Task.FromResult((Exception)new IotHubNotFoundException(_iotHubName)) }
                };

                return _httpClientHelper.PostAsync(
                    GetAdminUri("importRegistry"),
                    new ExportImportRequest
                    {
                        ContainerName = containerName,
                        StorageConnectionString = storageAccountConnectionString,
                    },
                    errorMappingOverrides,
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(ImportRegistryAsync)} threw an exception: {ex}", nameof(ImportRegistryAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Importing registry", nameof(ImportRegistryAsync));
            }
        }

#pragma warning disable CA1054 // Uri parameters should not be strings

        /// <summary>
        /// Creates a new bulk job to export device registrations to the container specified by the provided URI.
        /// </summary>
        /// <param name="exportBlobContainerUri">Destination blob container URI.</param>
        /// <param name="excludeKeys">Specifies whether to exclude the Device's Keys during the export.</param>
        /// <returns>JobProperties of the newly created job.</returns>

        public virtual Task<JobProperties> ExportDevicesAsync(string exportBlobContainerUri, bool excludeKeys)
        {
            return ExportDevicesAsync(
                JobProperties.CreateForExportJob(
                    exportBlobContainerUri,
                    excludeKeys));
        }

        /// <summary>
        /// Creates a new bulk job to export device registrations to the container specified by the provided URI.
        /// </summary>
        /// <param name="exportBlobContainerUri">Destination blob container URI.</param>
        /// <param name="excludeKeys">Specifies whether to exclude the Device's Keys during the export.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public virtual Task<JobProperties> ExportDevicesAsync(string exportBlobContainerUri, bool excludeKeys, CancellationToken cancellationToken)
        {
            return ExportDevicesAsync(
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
        /// <returns>JobProperties of the newly created job.</returns>
        public virtual Task<JobProperties> ExportDevicesAsync(string exportBlobContainerUri, string outputBlobName, bool excludeKeys)
        {
            return ExportDevicesAsync(
                JobProperties.CreateForExportJob(
                    exportBlobContainerUri,
                    excludeKeys,
                    outputBlobName));
        }

        /// <summary>
        /// Creates a new bulk job to export device registrations to the container specified by the provided URI.
        /// </summary>
        /// <param name="exportBlobContainerUri">Destination blob container URI.</param>
        /// <param name="outputBlobName">The name of the blob that will be created in the provided output blob container.</param>
        /// <param name="excludeKeys">Specifies whether to exclude the Device's Keys during the export.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public virtual Task<JobProperties> ExportDevicesAsync(string exportBlobContainerUri, string outputBlobName, bool excludeKeys, CancellationToken cancellationToken)
        {
            return ExportDevicesAsync(
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
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <remarks>Conditionally includes configurations, if specified.</remarks>
        /// <returns>JobProperties of the newly created job.</returns>
        public virtual Task<JobProperties> ExportDevicesAsync(JobProperties jobParameters, CancellationToken cancellationToken = default)
        {
            if (jobParameters == null)
            {
                throw new ArgumentNullException(nameof(jobParameters));
            }

            Logging.Enter(this, $"Export Job running with {jobParameters}", nameof(ExportDevicesAsync));

            try
            {
                jobParameters.Type = JobType.ExportDevices;
                return CreateJobAsync(jobParameters, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(ExportDevicesAsync)} threw an exception: {ex}", nameof(ExportDevicesAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Export Job running with {jobParameters}", nameof(ExportDevicesAsync));
            }
        }

        /// <summary>
        /// Creates a new bulk job to import device registrations into the IoT Hub.
        /// </summary>
        /// <param name="importBlobContainerUri">Source blob container URI.</param>
        /// <param name="outputBlobContainerUri">Destination blob container URI.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public virtual Task<JobProperties> ImportDevicesAsync(string importBlobContainerUri, string outputBlobContainerUri)
        {
            return ImportDevicesAsync(
                JobProperties.CreateForImportJob(
                    importBlobContainerUri,
                    outputBlobContainerUri));
        }

        /// <summary>
        /// Creates a new bulk job to import device registrations into the IoT Hub.
        /// </summary>
        /// <param name="importBlobContainerUri">Source blob container URI.</param>
        /// <param name="outputBlobContainerUri">Destination blob container URI.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public virtual Task<JobProperties> ImportDevicesAsync(string importBlobContainerUri, string outputBlobContainerUri, CancellationToken cancellationToken)
        {
            return ImportDevicesAsync(
               JobProperties.CreateForImportJob(
                   importBlobContainerUri,
                   outputBlobContainerUri),
               cancellationToken);
        }

        /// <summary>
        /// Creates a new bulk job to import device registrations into the IoT Hub.
        /// </summary>
        /// <param name="importBlobContainerUri">Source blob container URI.</param>
        /// <param name="outputBlobContainerUri">Destination blob container URI.</param>
        /// <param name="inputBlobName">The blob name to be used when importing from the provided input blob container.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public virtual Task<JobProperties> ImportDevicesAsync(string importBlobContainerUri, string outputBlobContainerUri, string inputBlobName)
        {
            return ImportDevicesAsync(
               JobProperties.CreateForImportJob(
                   importBlobContainerUri,
                   outputBlobContainerUri,
                   inputBlobName));
        }

        /// <summary>
        /// Creates a new bulk job to import device registrations into the IoT Hub.
        /// </summary>
        /// <param name="importBlobContainerUri">Source blob container URI.</param>
        /// <param name="outputBlobContainerUri">Destination blob container URI.</param>
        /// <param name="inputBlobName">The blob name to be used when importing from the provided input blob container.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>JobProperties of the newly created job.</returns>
        public virtual Task<JobProperties> ImportDevicesAsync(string importBlobContainerUri, string outputBlobContainerUri, string inputBlobName, CancellationToken cancellationToken)
        {
            return ImportDevicesAsync(
               JobProperties.CreateForImportJob(
                   importBlobContainerUri,
                   outputBlobContainerUri,
                   inputBlobName),
               cancellationToken);
        }

#pragma warning restore CA1054 // Uri parameters should not be strings

        /// <summary>
        /// Creates a new bulk job to import device registrations into the IoT Hub.
        /// </summary>
        /// <param name="jobParameters">Parameters for the job.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <remarks>Conditionally includes configurations, if specified.</remarks>
        /// <returns>JobProperties of the newly created job.</returns>
        public virtual Task<JobProperties> ImportDevicesAsync(JobProperties jobParameters, CancellationToken cancellationToken = default)
        {
            if (jobParameters == null)
            {
                throw new ArgumentNullException(nameof(jobParameters));
            }

            Logging.Enter(this, $"Import Job running with {jobParameters}", nameof(ImportDevicesAsync));
            try
            {
                jobParameters.Type = JobType.ImportDevices;
                return CreateJobAsync(jobParameters, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(ExportDevicesAsync)} threw an exception: {ex}", nameof(ImportDevicesAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Import Job running with {jobParameters}", nameof(ImportDevicesAsync));
            }
        }

        /// <summary>
        /// Gets the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the Job object to retrieve.</param>
        /// <returns>JobProperties of the job specified by the provided jobId.</returns>
        public virtual Task<JobProperties> GetJobAsync(string jobId)
        {
            return GetJobAsync(jobId, CancellationToken.None);
        }

        /// <summary>
        /// Gets the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the Job object to retrieve.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>JobProperties of the job specified by the provided jobId.</returns>
        public virtual Task<JobProperties> GetJobAsync(string jobId, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Getting job {jobId}", nameof(GetJobsAsync));
            try
            {
                EnsureInstanceNotClosed();

                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
                {
                    { HttpStatusCode.NotFound, responseMessage => Task.FromResult((Exception)new JobNotFoundException(jobId)) }
                };

                return _httpClientHelper.GetAsync<JobProperties>(
                    GetJobUri("/{0}".FormatInvariant(jobId)),
                    errorMappingOverrides,
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetJobsAsync)} threw an exception: {ex}", nameof(GetJobsAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting job {jobId}", nameof(GetJobsAsync));
            }
        }

        /// <summary>
        /// List all jobs for the IoT Hub.
        /// </summary>
        /// <returns>IEnumerable of JobProperties of all jobs for this IoT Hub.</returns>
        public virtual Task<IEnumerable<JobProperties>> GetJobsAsync()
        {
            return GetJobsAsync(CancellationToken.None);
        }

        /// <summary>
        /// List all jobs for the IoT Hub.
        /// </summary>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>IEnumerable of JobProperties of all jobs for this IoT Hub.</returns>
        public virtual Task<IEnumerable<JobProperties>> GetJobsAsync(CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Getting job", nameof(GetJobsAsync));
            try
            {
                EnsureInstanceNotClosed();

                return _httpClientHelper.GetAsync<IEnumerable<JobProperties>>(
                    GetJobUri(string.Empty),
                    null,
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetJobsAsync)} threw an exception: {ex}", nameof(GetJobsAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting job", nameof(GetJobsAsync));
            }
        }

        /// <summary>
        /// Cancels/Deletes the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to cancel.</param>
        public virtual Task CancelJobAsync(string jobId)
        {
            return CancelJobAsync(jobId, CancellationToken.None);
        }

        /// <summary>
        /// Cancels/Deletes the job with the specified Id.
        /// </summary>
        /// <param name="jobId">Id of the job to cancel.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        public virtual Task CancelJobAsync(string jobId, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Canceling job: {jobId}", nameof(CancelJobAsync));
            try
            {
                EnsureInstanceNotClosed();

                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
                {
                    { HttpStatusCode.NotFound, responseMessage => Task.FromResult((Exception)new JobNotFoundException(jobId)) }
                };

                IETagHolder jobETag = new ETagHolder
                {
                    ETag = jobId,
                };

                return _httpClientHelper.DeleteAsync(
                    GetJobUri("/{0}".FormatInvariant(jobId)),
                    jobETag,
                    errorMappingOverrides,
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetJobsAsync)} threw an exception: {ex}", nameof(GetJobsAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting job {jobId}", nameof(GetJobsAsync));
            }
        }

        /// <summary>
        /// Gets <see cref="Twin"/> from IotHub
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <returns>Twin instance.</returns>
        public virtual Task<Twin> GetTwinAsync(string deviceId)
        {
            return GetTwinAsync(deviceId, CancellationToken.None);
        }

        /// <summary>
        /// Gets <see cref="Twin"/> from IotHub
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Twin instance.</returns>
        public virtual Task<Twin> GetTwinAsync(string deviceId, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Getting device twin on device: {deviceId}", nameof(GetTwinAsync));
            try
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrWhitespace, "deviceId"));
                }

                EnsureInstanceNotClosed();
                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>()
                {
                    { HttpStatusCode.NotFound, async responseMessage => new DeviceNotFoundException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false)) }
                };

                return _httpClientHelper.GetAsync<Twin>(GetTwinUri(deviceId), errorMappingOverrides, null, false, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetTwinAsync)} threw an exception: {ex}", nameof(GetTwinAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting device twin on device: {deviceId}", nameof(GetTwinAsync));
            }
        }

        /// <summary>
        /// Gets Module's <see cref="Twin"/> from IotHub
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <returns>Twin instance.</returns>
        public virtual Task<Twin> GetTwinAsync(string deviceId, string moduleId)
        {
            return GetTwinAsync(deviceId, moduleId, CancellationToken.None);
        }

        /// <summary>
        /// Gets Module's <see cref="Twin"/> from IotHub
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Twin instance.</returns>
        public virtual Task<Twin> GetTwinAsync(string deviceId, string moduleId, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Getting device twin on device: {deviceId} and module: {moduleId}", nameof(GetTwinAsync));

            try
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrWhitespace, "deviceId"));
                }

                if (string.IsNullOrWhiteSpace(moduleId))
                {
                    throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrWhitespace, "moduleId"));
                }

                EnsureInstanceNotClosed();
                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>()
                {
                    { HttpStatusCode.NotFound, async responseMessage => new ModuleNotFoundException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false), (Exception)null) }
                };

                return _httpClientHelper.GetAsync<Twin>(GetModuleTwinRequestUri(deviceId, moduleId), errorMappingOverrides, null, false, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetTwinAsync)} threw an exception: {ex}", nameof(GetTwinAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting device twin on device: {deviceId} and module: {moduleId}", nameof(GetTwinAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, Twin twinPatch, string etag)
        {
            return UpdateTwinAsync(deviceId, twinPatch, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, Twin twinPatch, string etag, CancellationToken cancellationToken)
        {
            return UpdateTwinInternalAsync(deviceId, twinPatch, etag, false, cancellationToken);
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="jsonTwinPatch">Twin json with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, string jsonTwinPatch, string etag)
        {
            return UpdateTwinAsync(deviceId, jsonTwinPatch, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="jsonTwinPatch">Twin json with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, string jsonTwinPatch, string etag, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Updating device twin on device: {deviceId}", nameof(UpdateTwinAsync));

            try
            {
                if (string.IsNullOrWhiteSpace(jsonTwinPatch))
                {
                    throw new ArgumentNullException(nameof(jsonTwinPatch));
                }

                // TODO: Do we need to deserialize Twin, only to serialize it again?
                Twin twin = JsonConvert.DeserializeObject<Twin>(jsonTwinPatch);
                return UpdateTwinAsync(deviceId, twin, etag, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(UpdateTwinAsync)} threw an exception: {ex}", nameof(UpdateTwinAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Updating device twin on device: {deviceId}", nameof(UpdateTwinAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, string moduleId, Twin twinPatch, string etag)
        {
            return UpdateTwinAsync(deviceId, moduleId, twinPatch, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, string moduleId, Twin twinPatch, string etag, CancellationToken cancellationToken)
        {
            return UpdateTwinInternalAsync(deviceId, moduleId, twinPatch, etag, false, cancellationToken);
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="jsonTwinPatch">Twin json with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, string moduleId, string jsonTwinPatch, string etag)
        {
            return UpdateTwinAsync(deviceId, moduleId, jsonTwinPatch, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="jsonTwinPatch">Twin json with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, string moduleId, string jsonTwinPatch, string etag, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Updating device twin on device: {deviceId} and module: {moduleId}", nameof(UpdateTwinAsync));
            try
            {
                if (string.IsNullOrWhiteSpace(jsonTwinPatch))
                {
                    throw new ArgumentNullException(nameof(jsonTwinPatch));
                }

                // TODO: Do we need to deserialize Twin, only to serialize it again?
                Twin twin = JsonConvert.DeserializeObject<Twin>(jsonTwinPatch);
                return UpdateTwinAsync(deviceId, moduleId, twin, etag, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(UpdateTwinAsync)} threw an exception: {ex}", nameof(UpdateTwinAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Updating device twin on device: {deviceId} and module: {moduleId}", nameof(UpdateTwinAsync));
            }
        }

        /// <summary>
        /// Update the mutable fields for a list of <see cref="Twin"/>s previously created within the system
        /// </summary>
        /// <param name="twins">List of <see cref="Twin"/>s with updated fields.</param>
        /// <returns>Result of the bulk update operation.</returns>
        public virtual Task<BulkRegistryOperationResult> UpdateTwins2Async(IEnumerable<Twin> twins)
        {
            return UpdateTwins2Async(twins, false, CancellationToken.None);
        }

        /// <summary>
        /// Update the mutable fields for a list of <see cref="Twin"/>s previously created within the system
        /// </summary>
        /// <param name="twins">List of <see cref="Twin"/>s with updated fields.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Result of the bulk update operation.</returns>
        public virtual Task<BulkRegistryOperationResult> UpdateTwins2Async(IEnumerable<Twin> twins, CancellationToken cancellationToken)
        {
            return UpdateTwins2Async(twins, false, cancellationToken);
        }

        /// <summary>
        /// Update the mutable fields for a list of <see cref="Twin"/>s previously created within the system
        /// </summary>
        /// <param name="twins">List of <see cref="Twin"/>s with updated fields.</param>
        /// <param name="forceUpdate">Forces the <see cref="Twin"/> object to be updated even if it has changed since it was retrieved last time.</param>
        /// <returns>Result of the bulk update operation.</returns>
        public virtual Task<BulkRegistryOperationResult> UpdateTwins2Async(IEnumerable<Twin> twins, bool forceUpdate)
        {
            return UpdateTwins2Async(twins, forceUpdate, CancellationToken.None);
        }

        /// <summary>
        /// Update the mutable fields for a list of <see cref="Twin"/>s previously created within the system
        /// </summary>
        /// <param name="twins">List of <see cref="Twin"/>s with updated fields.</param>
        /// <param name="forceUpdate">Forces the <see cref="Twin"/> object to be updated even if it has changed since it was retrieved last time.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Result of the bulk update operation.</returns>
        public virtual Task<BulkRegistryOperationResult> UpdateTwins2Async(IEnumerable<Twin> twins, bool forceUpdate, CancellationToken cancellationToken)
        {
            return BulkDeviceOperationsAsync<BulkRegistryOperationResult>(
                GenerateExportImportDeviceListForTwinBulkOperations(twins, forceUpdate ? ImportMode.UpdateTwin : ImportMode.UpdateTwinIfMatchETag),
                ClientApiVersionHelper.ApiVersionQueryString,
                cancellationToken);
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="newTwin">New Twin object to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, Twin newTwin, string etag)
        {
            return ReplaceTwinAsync(deviceId, newTwin, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="newTwin">New Twin object to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, Twin newTwin, string etag, CancellationToken cancellationToken)
        {
            return UpdateTwinInternalAsync(deviceId, newTwin, etag, true, cancellationToken);
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="newTwinJson">New Twin json to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, string newTwinJson, string etag)
        {
            return ReplaceTwinAsync(deviceId, newTwinJson, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="newTwinJson">New Twin json to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, string newTwinJson, string etag, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Replacing device twin on device: {deviceId}", nameof(ReplaceTwinAsync));
            try
            {
                if (string.IsNullOrWhiteSpace(newTwinJson))
                {
                    throw new ArgumentNullException(nameof(newTwinJson));
                }

                // TODO: Do we need to deserialize Twin, only to serialize it again?
                Twin twin = JsonConvert.DeserializeObject<Twin>(newTwinJson);
                return ReplaceTwinAsync(deviceId, twin, etag, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(ReplaceTwinAsync)} threw an exception: {ex}", nameof(ReplaceTwinAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Replacing device twin on device: {deviceId}", nameof(ReplaceTwinAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="newTwin">New Twin object to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, string moduleId, Twin newTwin, string etag)
        {
            return ReplaceTwinAsync(deviceId, moduleId, newTwin, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="newTwin">New Twin object to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, string moduleId, Twin newTwin, string etag, CancellationToken cancellationToken)
        {
            return UpdateTwinInternalAsync(deviceId, moduleId, newTwin, etag, true, cancellationToken);
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="newTwinJson">New Twin json to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, string moduleId, string newTwinJson, string etag)
        {
            return ReplaceTwinAsync(deviceId, moduleId, newTwinJson, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="newTwinJson">New Twin json to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, string moduleId, string newTwinJson, string etag, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(newTwinJson))
            {
                throw new ArgumentNullException(nameof(newTwinJson));
            }

            // TODO: Do we need to deserialize Twin, only to serialize it again?
            Twin twin = JsonConvert.DeserializeObject<Twin>(newTwinJson);
            return ReplaceTwinAsync(deviceId, moduleId, twin, etag, cancellationToken);
        }

        /// <summary>
        /// Register a new Configuration for Azure IOT Edge in IotHub
        /// </summary>
        /// <param name="configuration">The Configuration object being registered.</param>
        /// <returns>The Configuration object.</returns>
        public virtual Task<Configuration> AddConfigurationAsync(Configuration configuration)
        {
            return AddConfigurationAsync(configuration, CancellationToken.None);
        }

        /// <summary>
        /// Register a new Configuration for Azure IOT Edge in IotHub
        /// </summary>
        /// <param name="configuration">The Configuration object being registered.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object.</returns>
        public virtual Task<Configuration> AddConfigurationAsync(Configuration configuration, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Adding configuration: {configuration?.Id}", nameof(AddConfigurationAsync));

            try
            {
                EnsureInstanceNotClosed();

                if (!string.IsNullOrEmpty(configuration.ETag))
                {
                    throw new ArgumentException(ApiResources.ETagSetWhileCreatingConfiguration);
                }

                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
                {
                    {
                        HttpStatusCode.PreconditionFailed,
                        async responseMessage => new PreconditionFailedException(
                            await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))
                    }
                };

                return _httpClientHelper.PutAsync(GetConfigurationRequestUri(configuration.Id), configuration, PutOperationType.CreateEntity, errorMappingOverrides, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(AddConfigurationAsync)} threw an exception: {ex}", nameof(AddConfigurationAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Adding configuration: {configuration?.Id}", nameof(AddConfigurationAsync));
            }
        }


        /// <summary>
        /// Add a devicegroup to IotHub
        /// </summary>
        /// <param name="deviceGroupName">The DeviceGroup name</param>
        /// <returns>DeviceGroup object.</returns>
        public virtual Task<DeviceGroup> GetDeviceGroupAsync(string deviceGroupName)
        {
            return GetDeviceGroupAsync(deviceGroupName, CancellationToken.None);
        }

        /// <summary>
        /// Add a devicegroup to IotHub
        /// </summary>
        /// <param name="deviceGroupName">The DeviceGroup name</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>DeviceGroup object.</returns>
        public virtual Task<DeviceGroup> GetDeviceGroupAsync(string deviceGroupName, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Getting DeviceGroup", nameof(GetDeviceGroupAsync));
            try
            {
                Uri uri = GetDeviceGroupUri(deviceGroupName);
                return _httpClientHelper.GetAsync<DeviceGroup>(
                    uri,
                    null,
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetDeviceGroupAsync)} threw an exception: {ex}", nameof(GetDeviceGroupAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting job", nameof(GetDeviceGroupAsync));
            }
        }

        /// <summary>
        /// Add a devicegroup to IotHub
        /// </summary>
        /// <param name="deviceGroup">The DeviceGroup object</param>
        /// <returns>The Updated DeviceGroup object.</returns>
        public virtual Task<DeviceGroup> AddDeviceGroupAsync(DeviceGroup deviceGroup)
        {
            return AddDeviceGroupAsync(deviceGroup, CancellationToken.None);
        }

        /// <summary>
        /// Add a devicegroup to IotHub
        /// </summary>
        /// <param name="deviceGroup">The DeviceGroup object</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Updated DeviceGroup object.</returns>
        public virtual Task<DeviceGroup> AddDeviceGroupAsync( DeviceGroup deviceGroup, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Adding DeviceGroup", nameof(AddDeviceGroupAsync));
            try
            {
                EnsureInstanceNotClosed();
                if (!string.IsNullOrEmpty(deviceGroup.ETag))
                {
                    throw new ArgumentException(ApiResources.ETagSetWhileCreatingDeviceGroup);
                }

                return _httpClientHelper.PutAsync<DeviceGroup>(
                    GetDeviceGroupUri(deviceGroup.Name),
                    deviceGroup,
                    PutOperationType.CreateEntity,
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(AddDeviceGroupAsync)} threw an exception: {ex}", nameof(AddDeviceGroupAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Adding DeviceGroup", nameof(AddDeviceGroupAsync));
            }
        }

        /// <summary>
        /// Update a devicegroup to IotHub
        /// </summary>
        /// <param name="deviceGroup">The DeviceGroup object</param>
        /// <returns>The Updated DeviceGroup object.</returns>
        public virtual Task<DeviceGroup> UpdateDeviceGroupAsync(DeviceGroup deviceGroup)
        {
            return UpdateDeviceGroupAsync(deviceGroup, CancellationToken.None);
        }

        /// <summary>
        /// Update DeviceGroup in IotHub
        /// </summary>
        /// <param name="deviceGroup">The DeviceGroup object</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Updated DeviceGroup object.</returns>
        public virtual Task<DeviceGroup> UpdateDeviceGroupAsync(DeviceGroup deviceGroup, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Updating DeviceGroup", nameof(UpdateDeviceGroupAsync));
            try
            {
                EnsureInstanceNotClosed();
                if (string.IsNullOrEmpty(deviceGroup.ETag))
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingDeviceGroup);
                }

                return _httpClientHelper.PutAsync<DeviceGroup>(
                    GetDeviceGroupUri(deviceGroup.Name),
                    deviceGroup,
                    deviceGroup.ETag == WildcardEtag ? PutOperationType.ForceUpdateEntity : PutOperationType.UpdateEntity,
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(UpdateDeviceGroupAsync)} threw an exception: {ex}", nameof(UpdateDeviceGroupAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Updating DeviceGroup", nameof(UpdateDeviceGroupAsync));
            }
        }

        /// <summary>
        /// Delete DeviceGroup in IotHub
        /// </summary>
        /// <param name="deviceGroup"> device group to be deleted </param>
        /// <returns>The Updated DeviceGroup object.</returns>
        public virtual Task RemoveDeviceGroupAsync(DeviceGroup deviceGroup)
        {
            return RemoveDeviceGroupAsync(deviceGroup, CancellationToken.None);
        }

        /// <summary>
        /// Delete DeviceGroup in IotHub
        /// </summary>
        /// <param name="deviceGroup"> device group to be deleted </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Updated DeviceGroup object.</returns>
        public virtual Task RemoveDeviceGroupAsync(DeviceGroup deviceGroup, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Deleting DeviceGroup", nameof(RemoveDeviceGroupAsync));
            try
            {
                Uri uri = GetDeviceGroupUri(deviceGroup.Name);

                return _httpClientHelper.DeleteAsync<IETagHolder>(
                    uri,
                    deviceGroup,
                    null,
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(RemoveDeviceGroupAsync)} threw an exception: {ex}", nameof(RemoveDeviceGroupAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Deleting DeviceGroup", nameof(RemoveDeviceGroupAsync));
            }
        }


        /// <summary>
        /// Get DeviceGroups
        /// </summary>
        /// <param name="maxpagesize"> maxpagesize </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns> IEnumberable od DeviceGroups</returns>
        public virtual Task<IEnumerable<DeviceGroup>> GetDeviceGroupsAsync(int maxpagesize, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Getting DeviceGroups", nameof(GetDeviceGroupAsync));
            try
            {
                Uri uri = GetDeviceGroupsUri();
                return _httpClientHelper.GetAsync<IEnumerable<DeviceGroup>>(
                    uri,
                    null,
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetDeviceGroupAsync)} threw an exception: {ex}", nameof(GetDeviceGroupAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting DeviceGroups", nameof(GetDeviceGroupAsync));
            }
        }

        private static Uri GetDeviceGroupUri(string deviceGroupName)
        {
            return new Uri(DeviceGroupUriFormat.FormatInvariant(deviceGroupName), UriKind.Relative);
        }

        private static Uri GetDeviceGroupsUri()
        {
            return new Uri(DeviceGroupsUriFormat, UriKind.Relative);
        }

        /// <summary>
        /// Retrieves the specified Configuration object.
        /// </summary>
        /// <param name="configurationId">The id of the Configuration being retrieved.</param>
        /// <returns>The Configuration object.</returns>
        public virtual Task<Configuration> GetConfigurationAsync(string configurationId)
        {
            return GetConfigurationAsync(configurationId, CancellationToken.None);
        }

        /// <summary>
        /// Retrieves the specified Configuration object.
        /// </summary>
        /// <param name="configurationId">The id of the Configuration being retrieved.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object.</returns>
        public virtual Task<Configuration> GetConfigurationAsync(string configurationId, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Getting configuration: {configurationId}", nameof(GetConfigurationAsync));
            try
            {
                if (string.IsNullOrWhiteSpace(configurationId))
                {
                    throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrWhitespace, "configurationId"));
                }

                EnsureInstanceNotClosed();
                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>()
                {
                    { HttpStatusCode.NotFound, async responseMessage => new ConfigurationNotFoundException(
                        await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false)) }
                };

                return _httpClientHelper.GetAsync<Configuration>(GetConfigurationRequestUri(configurationId), errorMappingOverrides, null, false, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetConfigurationAsync)} threw an exception: {ex}", nameof(GetConfigurationAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Get configuration: {configurationId}", nameof(GetConfigurationAsync));
            }
        }

        /// <summary>
        /// Retrieves specified number of configurations from every IoT Hub partition.
        /// Results are not ordered.
        /// </summary>
        /// <returns>The list of configurations.</returns>
        public virtual Task<IEnumerable<Configuration>> GetConfigurationsAsync(int maxCount)
        {
            return GetConfigurationsAsync(maxCount, CancellationToken.None);
        }

        /// <summary>
        /// Retrieves specified number of configurations from every IoT hub partition.
        /// Results are not ordered.
        /// </summary>
        /// <returns>The list of configurations.</returns>
        public virtual Task<IEnumerable<Configuration>> GetConfigurationsAsync(int maxCount, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Getting configuration: max count: {maxCount}", nameof(GetConfigurationsAsync));
            try
            {
                EnsureInstanceNotClosed();

                return _httpClientHelper.GetAsync<IEnumerable<Configuration>>(
                    GetConfigurationsRequestUri(maxCount),
                    null,
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetConfigurationsAsync)} threw an exception: {ex}", nameof(GetConfigurationsAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting configuration: max count: {maxCount}", nameof(GetConfigurationsAsync));
            }
        }

        /// <summary>
        /// Update the mutable fields of the Configuration registration
        /// </summary>
        /// <param name="configuration">The Configuration object with updated fields.</param>
        /// <returns>The Configuration object with updated ETag.</returns>
        public virtual Task<Configuration> UpdateConfigurationAsync(Configuration configuration)
        {
            return UpdateConfigurationAsync(configuration, CancellationToken.None);
        }

        /// <summary>
        /// Update the mutable fields of the Configuration registration
        /// </summary>
        /// <param name="configuration">The Configuration object with updated fields.</param>
        /// <param name="forceUpdate">Forces the device object to be replaced without regard for an ETag match.</param>
        /// <returns>The Configuration object with updated ETags.</returns>
        public virtual Task<Configuration> UpdateConfigurationAsync(Configuration configuration, bool forceUpdate)
        {
            return UpdateConfigurationAsync(configuration, forceUpdate, CancellationToken.None);
        }

        /// <summary>
        /// Update the mutable fields of the Configuration registration
        /// </summary>
        /// <param name="configuration">The Configuration object with updated fields.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object with updated ETags.</returns>
        public virtual Task<Configuration> UpdateConfigurationAsync(Configuration configuration, CancellationToken cancellationToken)
        {
            return UpdateConfigurationAsync(configuration, false, cancellationToken);
        }

        /// <summary>
        /// Update the mutable fields of the Configuration registration
        /// </summary>
        /// <param name="configuration">The Configuration object with updated fields.</param>
        /// <param name="forceUpdate">Forces the Configuration object to be replaced even if it was updated since it was retrieved last time.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object with updated ETags.</returns>
        public virtual Task<Configuration> UpdateConfigurationAsync(Configuration configuration, bool forceUpdate, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Updating configuration: {configuration?.Id} - Force update: {forceUpdate}", nameof(UpdateConfigurationAsync));

            try
            {
                EnsureInstanceNotClosed();

                if (string.IsNullOrWhiteSpace(configuration.ETag) && !forceUpdate)
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingConfiguration);
                }

                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>()
                {
                    { HttpStatusCode.PreconditionFailed, async (responseMessage) => new PreconditionFailedException(
                        await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false)) },
                    {
                        HttpStatusCode.NotFound, async responseMessage =>
                        {
                            string responseContent = await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false);
                            return new ConfigurationNotFoundException(responseContent, (Exception)null);
                        }
                    }
                };

                PutOperationType operationType = forceUpdate
                    ? PutOperationType.ForceUpdateEntity
                    : PutOperationType.UpdateEntity;

                return _httpClientHelper.PutAsync(GetConfigurationRequestUri(configuration.Id), configuration, operationType, errorMappingOverrides, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(UpdateConfigurationAsync)} threw an exception: {ex}", nameof(UpdateConfigurationAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Updating configuration: {configuration?.Id} - Force update: {forceUpdate}", nameof(UpdateConfigurationAsync));
            }
        }

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="configurationId">The id of the Configuration being deleted.</param>
        public virtual Task RemoveConfigurationAsync(string configurationId)
        {
            return RemoveConfigurationAsync(configurationId, CancellationToken.None);
        }

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="configurationId">The id of the configurationId being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual Task RemoveConfigurationAsync(string configurationId, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Removing configuration: {configurationId}", nameof(RemoveConfigurationAsync));

            try
            {
                EnsureInstanceNotClosed();

                if (string.IsNullOrWhiteSpace(configurationId))
                {
                    throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrWhitespace, "configurationId"));
                }

                // use wild-card ETag
                var eTag = new ETagHolder { ETag = "*" };
                return RemoveConfigurationAsync(configurationId, eTag, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(RemoveConfigurationAsync)} threw an exception: {ex}", nameof(RemoveConfigurationAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Removing configuration: {configurationId}", nameof(RemoveConfigurationAsync));
            }
        }

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="configuration">The Configuration being deleted.</param>
        public virtual Task RemoveConfigurationAsync(Configuration configuration)
        {
            return RemoveConfigurationAsync(configuration, CancellationToken.None);
        }

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="configuration">The Configuration being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual Task RemoveConfigurationAsync(Configuration configuration, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Removing configuration: {configuration?.Id}", nameof(RemoveConfigurationAsync));
            try
            {
                EnsureInstanceNotClosed();

                return string.IsNullOrWhiteSpace(configuration.ETag)
                    ? throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingConfiguration)
                    : RemoveConfigurationAsync(configuration.Id, configuration, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(RemoveConfigurationAsync)} threw an exception: {ex}", nameof(RemoveConfigurationAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Removing configuration: {configuration?.Id}", nameof(RemoveConfigurationAsync));
            }
        }

        /// <summary>
        /// Applies configuration content to a device.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="content">The configuration of a device.</param>
        public virtual Task ApplyConfigurationContentOnDeviceAsync(string deviceId, ConfigurationContent content)
        {
            return ApplyConfigurationContentOnDeviceAsync(deviceId, content, CancellationToken.None);
        }

        /// <summary>
        /// Applies configuration content to a device.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="content">The configuration of a device.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual Task ApplyConfigurationContentOnDeviceAsync(string deviceId, ConfigurationContent content, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Applying configuration content on device: {deviceId}", nameof(ApplyConfigurationContentOnDeviceAsync));

            try
            {
                return _httpClientHelper.PostAsync(GetApplyConfigurationOnDeviceRequestUri(deviceId), content, null, null, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(ApplyConfigurationContentOnDeviceAsync)} threw an exception: {ex}", nameof(ApplyConfigurationContentOnDeviceAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Applying configuration content on device: {deviceId}", nameof(ApplyConfigurationContentOnDeviceAsync));
            }
        }

        /// <summary>
        /// Gets the list of device groups the device belongs to. The device belongs to a device group if its attributes match the device group query.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        public virtual Task<IEnumerable<Device>> ListDeviceGroupsForDeviceAsync(string deviceId)
        {
            return ListDeviceGroupsForDeviceAsync(deviceId, CancellationToken.None);
        }

        /// <summary>
        /// Gets the list of device groups the device belongs to. The device belongs to a device group if its attributes match the device group query.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual Task<IEnumerable<Device>> ListDeviceGroupsForDeviceAsync(string deviceId, CancellationToken cancellationToken)
        {

        }

        /// <summary>
        /// Gets the list of device groups the module belongs to. The module belongs to a device group if its attributes match the device group query.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The Module Id.</param>
        public virtual Task<IEnumerable<Module>> ListDeviceGroupsForModuleAsync(string deviceId,string moduleId)
        {
            return ListDeviceGroupsForModuleAsync(deviceId,moduleId, CancellationToken.None);
        }

        /// <summary>
        /// Gets the list of device groups the module belongs to. The module belongs to a device group if its attributes match the device group query.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual Task<IEnumerable<Module>> ListDeviceGroupsForModuleAsync(string deviceId,string moduleId , CancellationToken cancellationToken)
        {
          
        }

        /// <summary>
        /// Gets the list of all members (devices and modules) in the device group.
        /// </summary>
        /// <param name="deviceGroupName">The DeviceGroup Name</param>
        /// <param name="maxpagesize">maxpagesize</param>
        public virtual Task ListDevicesAndModulesInGroupAsync(string deviceGroupName,int maxpagesize)
        {

        }


        public virtual Task<IEnumerable<RoleAssignment>> GetRoleAssignmentAsync(int maxpagesize)
        {
            
        }


        public virtual Task<IEnumerable<RoleAssignment>> GetRoleAssignmentAsync(int maxpagesize)
        {

        }

        public virtual Task<RoleAssignment> CreateRoleAssignmentAsync(RoleAssignment roleAssignment)
        {

        }

        public virtual Task<RoleAssignment> UpdateRoleAssignmentAsync(RoleAssignment roleAssignment)
        {

        }

        public virtual Task<TopicSpace> GetTopicSpaceAsync(string topicSpaceName)
        {

        }

        public virtual Task<TopicSpaceCollection> ListTopicSpacesAsync(string topicSpaceName)
        {

        }

        /// <summary>
        /// Undelete the device group. The call is idempotent -- it will succeed even if the device group is already active (not deleted).
        /// </summary>
        /// <param name="deviceGroup">The DeviceGroup object</param>
        public virtual Task<DeviceGroup> UndeleteDeviceGroupAsync(DeviceGroup deviceGroup)
        {
            return UndeleteDeviceGroupAsync(deviceGroup, CancellationToken.None);
        }

        /// <summary>
        /// Undelete the device group. The call is idempotent -- it will succeed even if the device group is already active (not deleted).
        /// </summary>
        /// <param name="deviceGroup">The DeviceGroup object</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual Task<DeviceGroup> UndeleteDeviceGroupAsync(DeviceGroup deviceGroup, CancellationToken cancellationToken)
        {

        }

        /// <summary>
        /// Purge the device group. This is allowed for both live and soft deleted groups.This call is idempotent -- it will succeed even if the device group is already purged.
        /// </summary>
        /// <param name="deviceGroupName">The DeviceGroup Name</param>
        public virtual Task PurgeDeviceGroupAsync(string deviceGroupName)
        {
            return PurgeDeviceGroupAsync(deviceGroupName, CancellationToken.None);
        }

        /// <summary>
        /// Purge the device group. This is allowed for both live and soft deleted groups.This call is idempotent -- it will succeed even if the device group is already purged.
        /// </summary>
        /// <param name="deviceGroupName">The DeviceGroup name</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual Task PurgeDeviceGroupAsync(string deviceGroupName, CancellationToken cancellationToken)
        {

        }



        private Task RemoveConfigurationAsync(string configurationId, IETagHolder eTagHolder, CancellationToken cancellationToken)
        {
            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.NotFound,
                    async responseMessage =>
                        {
                            string responseContent = await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false);
                            return new ConfigurationNotFoundException(responseContent, (Exception) null);
                        }
                },
                {
                    HttpStatusCode.PreconditionFailed,
                    async responseMessage => new PreconditionFailedException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))
                }
            };

            return _httpClientHelper.DeleteAsync(GetConfigurationRequestUri(configurationId), eTagHolder, errorMappingOverrides, null, cancellationToken);
        }

        private Task<Twin> UpdateTwinInternalAsync(string deviceId, Twin twin, string etag, bool isReplace, CancellationToken cancellationToken)
        {
            EnsureInstanceNotClosed();

            if (twin != null)
            {
                twin.DeviceId = deviceId;
            }

            ValidateTwinId(twin);

            if (string.IsNullOrEmpty(etag))
            {
                throw new ArgumentNullException(nameof(etag));
            }

            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.PreconditionFailed,
                    async responseMessage => new PreconditionFailedException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))
                },
                {
                    HttpStatusCode.NotFound,
                    async responseMessage => new DeviceNotFoundException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false), (Exception)null)
                }
            };

            return isReplace
                ? _httpClientHelper.PutAsync<Twin, Twin>(
                    GetTwinUri(deviceId),
                    twin,
                    etag,
                    etag == WildcardEtag ? PutOperationType.ForceUpdateEntity : PutOperationType.UpdateEntity,
                    errorMappingOverrides,
                    cancellationToken)
                : _httpClientHelper.PatchAsync<Twin, Twin>(
                    GetTwinUri(deviceId),
                    twin,
                    etag,
                    etag == WildcardEtag ? PutOperationType.ForceUpdateEntity : PutOperationType.UpdateEntity,
                    errorMappingOverrides,
                    cancellationToken);
        }

        private Task<Twin> UpdateTwinInternalAsync(string deviceId, string moduleId, Twin twin, string etag, bool isReplace, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Replacing device twin on device: {deviceId} - module: {moduleId} - is replace: {isReplace}", nameof(UpdateTwinAsync));
            try
            {
                EnsureInstanceNotClosed();

                if (twin != null)
                {
                    twin.DeviceId = deviceId;
                    twin.ModuleId = moduleId;
                }

                ValidateTwinId(twin);

                if (string.IsNullOrEmpty(etag))
                {
                    throw new ArgumentNullException(nameof(etag));
                }

                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
                {
                    {
                        HttpStatusCode.PreconditionFailed,
                        async responseMessage => new PreconditionFailedException(
                            await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))
                    },
                    {
                        HttpStatusCode.NotFound,
                        async responseMessage => new ModuleNotFoundException(
                            await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false),
                            (Exception)null)
                    }
                };

                return isReplace
                    ? _httpClientHelper.PutAsync<Twin, Twin>(
                        GetModuleTwinRequestUri(deviceId, moduleId),
                        twin,
                        etag,
                        etag == WildcardEtag ? PutOperationType.ForceUpdateEntity : PutOperationType.UpdateEntity,
                        errorMappingOverrides,
                        cancellationToken)
                    : _httpClientHelper.PatchAsync<Twin, Twin>(
                        GetModuleTwinRequestUri(deviceId, moduleId),
                        twin,
                        etag,
                        etag == WildcardEtag ? PutOperationType.ForceUpdateEntity : PutOperationType.UpdateEntity,
                        errorMappingOverrides,
                        cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(UpdateTwinAsync)} threw an exception: {ex}", nameof(UpdateTwinAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Replacing device twin on device: {deviceId} - module: {moduleId} - is replace: {isReplace}", nameof(UpdateTwinAsync));
            }
        }

        private async Task<QueryResult> ExecuteQueryAsync(string sqlQueryString, int? pageSize, string continuationToken, CancellationToken cancellationToken)
        {
            EnsureInstanceNotClosed();

            if (string.IsNullOrWhiteSpace(sqlQueryString))
            {
                throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrEmpty, nameof(sqlQueryString)));
            }

            var customHeaders = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                customHeaders.Add(ContinuationTokenHeader, continuationToken);
            }

            if (pageSize != null)
            {
                customHeaders.Add(PageSizeHeader, pageSize.ToString());
            }

            HttpResponseMessage response = await _httpClientHelper
                .PostAsync(
                    QueryDevicesRequestUri(),
                    new QuerySpecification { Sql = sqlQueryString },
                    null,
                    customHeaders,
                    new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" },
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            return await QueryResult.FromHttpResponseAsync(response).ConfigureAwait(false);
        }

        private Task<JobProperties> CreateJobAsync(JobProperties jobProperties, CancellationToken ct)
        {
            EnsureInstanceNotClosed();

            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                { HttpStatusCode.Forbidden, async (responseMessage) => new JobQuotaExceededException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))}
            };

            string clientApiVersion = ClientApiVersionHelper.ApiVersionQueryString;

            return _httpClientHelper.PostAsync<JobProperties, JobProperties>(
                GetJobUri("/create", clientApiVersion),
                jobProperties,
                errorMappingOverrides,
                null,
                ct);
        }

        private static Uri GetRequestUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(RequestUriFormat.FormatInvariant(deviceId, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
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

        private static Uri GetModuleTwinRequestUri(string deviceId, string moduleId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            moduleId = WebUtility.UrlEncode(moduleId);
            return new Uri(ModuleTwinUriFormat.FormatInvariant(deviceId, moduleId, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetConfigurationRequestUri(string configurationId)
        {
            configurationId = WebUtility.UrlEncode(configurationId);
            return new Uri(ConfigurationRequestUriFormat.FormatInvariant(configurationId, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetConfigurationsRequestUri(int maxCount)
        {
            return new Uri(ConfigurationsRequestUriFormat.FormatInvariant(maxCount, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetApplyConfigurationOnDeviceRequestUri(string deviceId)
        {
            return new Uri(ApplyConfigurationOnDeviceUriFormat.FormatInvariant(deviceId), UriKind.Relative);
        }

        private static Uri GetBulkRequestUri(string apiVersionQueryString)
        {
            return new Uri(RequestUriFormat.FormatInvariant(string.Empty, apiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetJobUri(string jobId, string apiVersion = ClientApiVersionHelper.ApiVersionQueryString)
        {
            return new Uri(JobsUriFormat.FormatInvariant(jobId, apiVersion), UriKind.Relative);
        }

        private static Uri GetDevicesRequestUri(int maxCount)
        {
            return new Uri(DevicesRequestUriFormat.FormatInvariant(maxCount, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri QueryDevicesRequestUri()
        {
            return new Uri(DevicesQueryUriFormat, UriKind.Relative);
        }

        private static Uri GetAdminUri(string operation)
        {
            return new Uri(AdminUriFormat.FormatInvariant(operation, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetStatisticsUri()
        {
            return new Uri(StatisticsUriFormat, UriKind.Relative);
        }

        private static Uri GetTwinUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(TwinUriFormat.FormatInvariant(deviceId, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }

        private static void ValidateDeviceId(Device device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (string.IsNullOrWhiteSpace(device.Id))
            {
                throw new ArgumentException("device.Id");
            }

            if (!s_deviceIdRegex.IsMatch(device.Id))
            {
                throw new ArgumentException(ApiResources.DeviceIdInvalid.FormatInvariant(device.Id));
            }
        }

        private static void ValidateTwinId(Twin twin)
        {
            if (twin == null)
            {
                throw new ArgumentNullException(nameof(twin));
            }

            if (string.IsNullOrWhiteSpace(twin.DeviceId))
            {
                throw new ArgumentException("twin.DeviceId");
            }

            if (!s_deviceIdRegex.IsMatch(twin.DeviceId))
            {
                throw new ArgumentException(ApiResources.DeviceIdInvalid.FormatInvariant(twin.DeviceId));
            }
        }

        private static void ValidateModuleId(Module module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (string.IsNullOrWhiteSpace(module.DeviceId))
            {
                throw new ArgumentException("module.Id");
            }

            if (string.IsNullOrWhiteSpace(module.Id))
            {
                throw new ArgumentException("module.ModuleId");
            }

            if (!s_deviceIdRegex.IsMatch(module.DeviceId))
            {
                throw new ArgumentException(ApiResources.DeviceIdInvalid.FormatInvariant(module.DeviceId));
            }

            if (!s_deviceIdRegex.IsMatch(module.Id))
            {
                throw new ArgumentException(ApiResources.DeviceIdInvalid.FormatInvariant(module.Id));
            }
        }

        private static void ValidateDeviceAuthentication(AuthenticationMechanism authentication, string deviceId)
        {
            if (authentication != null)
            {
                // Both symmetric keys and X.509 cert thumbprints cannot be specified for the same device
                bool symmetricKeyIsSet = !authentication.SymmetricKey?.IsEmpty() ?? false;
                bool x509ThumbprintIsSet = !authentication.X509Thumbprint?.IsEmpty() ?? false;

                if (symmetricKeyIsSet && x509ThumbprintIsSet)
                {
                    throw new ArgumentException(ApiResources.DeviceAuthenticationInvalid.FormatInvariant(deviceId ?? string.Empty));
                }

                // Validate X.509 thumbprints or SymmetricKeys since we should not have both at the same time
                if (x509ThumbprintIsSet)
                {
                    authentication.X509Thumbprint.IsValid(true);
                }
                else if (symmetricKeyIsSet)
                {
                    authentication.SymmetricKey.IsValid(true);
                }
            }
        }

        private Task RemoveDeviceModuleAsync(string deviceId, string moduleId, IETagHolder eTagHolder, CancellationToken cancellationToken)
        {
            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.NotFound,
                    async responseMessage =>
                        {
                            string responseContent = await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false);
                            return new DeviceNotFoundException(responseContent, (Exception) null);
                        }
                },
                {
                    HttpStatusCode.PreconditionFailed,
                    async responseMessage => new PreconditionFailedException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))
                },
            };

            return _httpClientHelper.DeleteAsync(GetModulesRequestUri(deviceId, moduleId), eTagHolder, errorMappingOverrides, null, cancellationToken);
        }

        private void EnsureInstanceNotClosed()
        {
            if (_httpClientHelper == null)
            {
                throw new ObjectDisposedException("RegistryManager", ApiResources.RegistryManagerInstanceAlreadyClosed);
            }
        }

        private static void NormalizeDevice(Device device)
        {
            // auto generate keys if not specified
            if (device.Authentication == null)
            {
                device.Authentication = new AuthenticationMechanism();
            }

            NormalizeAuthenticationInfo(device.Authentication);
        }

        private static void NormalizeAuthenticationInfo(AuthenticationMechanism authenticationInfo)
        {
            //to make it backward compatible we set the type according to the values
            //we don't set CA type - that has to be explicit
            if (authenticationInfo.SymmetricKey != null && !authenticationInfo.SymmetricKey.IsEmpty())
            {
                authenticationInfo.Type = AuthenticationType.Sas;
            }

            if (authenticationInfo.X509Thumbprint != null && !authenticationInfo.X509Thumbprint.IsEmpty())
            {
                authenticationInfo.Type = AuthenticationType.SelfSigned;
            }
        }

        private static void NormalizeExportImportDevice(ExportImportDevice device)
        {
            // auto generate keys if not specified
            if (device.Authentication == null)
            {
                device.Authentication = new AuthenticationMechanism();
            }

            NormalizeAuthenticationInfo(device.Authentication);
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
                ValidateDeviceId(device);

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

        private static IEnumerable<ExportImportDevice> GenerateExportImportDeviceListForTwinBulkOperations(IEnumerable<Twin> twins, ImportMode importMode)
        {
            if (twins == null)
            {
                throw new ArgumentNullException(nameof(twins));
            }

            if (!twins.Any())
            {
                throw new ArgumentException($"Parameter {nameof(twins)} cannot be empty");
            }

            var exportImportDeviceList = new List<ExportImportDevice>(twins.Count());
            foreach (Twin twin in twins)
            {
                ValidateTwinId(twin);

                switch (importMode)
                {
                    case ImportMode.UpdateTwin:
                        // No preconditions
                        break;

                    case ImportMode.UpdateTwinIfMatchETag:
                        if (string.IsNullOrWhiteSpace(twin.ETag))
                        {
                            throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingTwin);
                        }
                        break;

                    default:
                        throw new ArgumentException(IotHubApiResources.GetString(ApiResources.InvalidImportMode, importMode));
                }

                var exportImportDevice = new ExportImportDevice
                {
                    Id = twin.DeviceId,
                    ModuleId = twin.ModuleId,
                    ImportMode = importMode,
                    TwinETag = importMode == ImportMode.UpdateTwinIfMatchETag ? twin.ETag : null,
                    Tags = twin.Tags,
                    Properties = new ExportImportDevice.PropertyContainer(),
                };
                exportImportDevice.Properties.DesiredProperties = twin.Properties?.Desired;

                exportImportDeviceList.Add(exportImportDevice);
            }

            return exportImportDeviceList;
        }

        private Task<T> BulkDeviceOperationsAsync<T>(IEnumerable<ExportImportDevice> devices, string version, CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Performing bulk device operation on : {devices?.Count()} devices. version: {version}", nameof(BulkDeviceOperationsAsync));
            try
            {
                BulkDeviceOperationSetup(devices);

                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
                {
                    { HttpStatusCode.PreconditionFailed, async responseMessage => new PreconditionFailedException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false)) },
                    { HttpStatusCode.RequestEntityTooLarge, async responseMessage => new TooManyDevicesException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false)) },
                    { HttpStatusCode.BadRequest, async responseMessage => new ArgumentException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false)) }
                };

                return _httpClientHelper.PostAsync<IEnumerable<ExportImportDevice>, T>(GetBulkRequestUri(version), devices, errorMappingOverrides, null, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(BulkDeviceOperationsAsync)} threw an exception: {ex}", nameof(BulkDeviceOperationsAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Performing bulk device operation on : {devices?.Count()} devices. version: {version}", nameof(BulkDeviceOperationsAsync));
            }
        }

        private void BulkDeviceOperationSetup(IEnumerable<ExportImportDevice> devices)
        {
            EnsureInstanceNotClosed();

            if (devices == null)
            {
                throw new ArgumentNullException(nameof(devices));
            }

            foreach (ExportImportDevice device in devices)
            {
                ValidateDeviceAuthentication(device.Authentication, device.Id);

                NormalizeExportImportDevice(device);
            }
        }

        private Task RemoveDeviceAsync(string deviceId, IETagHolder eTagHolder, CancellationToken cancellationToken)
        {
            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.NotFound,
                    async responseMessage =>
                        {
                            string responseContent = await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false);
                            return new DeviceNotFoundException(responseContent, (Exception) null);
                        }
                },
                {
                    HttpStatusCode.PreconditionFailed,
                    async responseMessage => new PreconditionFailedException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))
                },
            };

            return _httpClientHelper.DeleteAsync(GetRequestUri(deviceId), eTagHolder, errorMappingOverrides, null, cancellationToken);
        }
    }
}
