using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> that handles creating, updating, getting and deleting configurations
    /// </summary>
    public class ConfigurationsClient
    {
        private const string ConfigurationRequestUriFormat = "/configurations/{0}?{1}";
        private const string ConfigurationsRequestUriFormat = "/configurations/?top={0}&{1}";
        private const string ApplyConfigurationOnDeviceUriFormat = "/devices/{0}/applyConfigurationContent?" + ClientApiVersionHelper.ApiVersionQueryString;
        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromSeconds(100);

        private IHttpClientHelper _httpClientHelper;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected ConfigurationsClient()
        {
        }

        internal ConfigurationsClient(IotHubConnectionProperties connectionProperties, HttpTransportSettings transportSettings)
        {
            _httpClientHelper = new HttpClientHelper(
                connectionProperties.HttpsEndpoint,
                connectionProperties,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                s_defaultOperationTimeout,
                transportSettings.Proxy,
                transportSettings.ConnectionLeaseTimeoutMilliseconds);
        }

        /// <summary>
        /// Creates a new Configuration for Azure IoT Edge in IoT hub
        /// </summary>
        /// <param name="configuration">The Configuration object being created.</param>
        /// <returns>The Configuration object.</returns>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public virtual Task<Configuration> CreateAsync(Configuration configuration)
        {
            return CreateAsync(configuration, CancellationToken.None);
        }

        /// <summary>
        /// Creates a new Configuration for Azure IoT Edge in IoT hub
        /// </summary>
        /// <param name="configuration">The Configuration object being created.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object.</returns>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public virtual Task<Configuration> CreateAsync(Configuration configuration, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Adding configuration: {configuration?.Id}", nameof(CreateAsync));

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
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(CreateAsync)} threw an exception: {ex}", nameof(CreateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Creating configuration: {configuration?.Id}", nameof(CreateAsync));
            }
        }

        /// <summary>
        /// Retrieves the specified Configuration object.
        /// </summary>
        /// <param name="configurationId">The id of the Configuration being retrieved.</param>
        /// <returns>The Configuration object.</returns>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public virtual Task<Configuration> GetAsync(string configurationId)
        {
            return GetAsync(configurationId, CancellationToken.None);
        }

        /// <summary>
        /// Retrieves the specified Configuration object.
        /// </summary>
        /// <param name="configurationId">The id of the Configuration being retrieved.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object.</returns>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public virtual Task<Configuration> GetAsync(string configurationId, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting configuration: {configurationId}", nameof(GetAsync));
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
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(GetAsync)} threw an exception: {ex}", nameof(GetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Get configuration: {configurationId}", nameof(GetAsync));
            }
        }

        /// <summary>
        /// Retrieves specified number of configurations from every IoT hub partition.
        /// Results are not ordered.
        /// </summary>
        /// <returns>The list of configurations.</returns>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public virtual Task<IEnumerable<Configuration>> GetAsync(int maxCount)
        {
            return GetAsync(maxCount, CancellationToken.None);
        }

        /// <summary>
        /// Retrieves specified number of configurations from every IoT hub partition.
        /// Results are not ordered.
        /// </summary>
        /// <returns>The list of configurations.</returns>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public virtual Task<IEnumerable<Configuration>> GetAsync(int maxCount, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting configuration: max count: {maxCount}", nameof(GetAsync));
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
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(GetAsync)} threw an exception: {ex}", nameof(GetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting configuration: max count: {maxCount}", nameof(GetAsync));
            }
        }

        /// <summary>
        /// Update the mutable fields of the Configuration registration
        /// </summary>
        /// <param name="configuration">The Configuration object with updated fields.</param>
        /// <returns>The Configuration object with updated ETag.</returns>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public virtual Task<Configuration> UpdateAsync(Configuration configuration)
        {
            return UpdateAsync(configuration, CancellationToken.None);
        }

        /// <summary>
        /// Update the mutable fields of the Configuration registration
        /// </summary>
        /// <param name="configuration">The Configuration object with updated fields.</param>
        /// <param name="forceUpdate">Forces the device object to be replaced without regard for an ETag match.</param>
        /// <returns>The Configuration object with updated ETags.</returns>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public virtual Task<Configuration> UpdateAsync(Configuration configuration, bool forceUpdate)
        {
            return UpdateAsync(configuration, forceUpdate, CancellationToken.None);
        }

        /// <summary>
        /// Update the mutable fields of the Configuration registration
        /// </summary>
        /// <param name="configuration">The Configuration object with updated fields.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object with updated ETags.</returns>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public virtual Task<Configuration> UpdateAsync(Configuration configuration, CancellationToken cancellationToken)
        {
            return UpdateAsync(configuration, false, cancellationToken);
        }

        /// <summary>
        /// Update the mutable fields of the Configuration registration
        /// </summary>
        /// <param name="configuration">The Configuration object with updated fields.</param>
        /// <param name="forceUpdate">Forces the Configuration object to be replaced even if it was updated since it was retrieved last time.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object with updated ETags.</returns>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public virtual Task<Configuration> UpdateAsync(Configuration configuration, bool forceUpdate, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating configuration: {configuration?.Id} - Force update: {forceUpdate}", nameof(UpdateAsync));

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
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(UpdateAsync)} threw an exception: {ex}", nameof(UpdateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Updating configuration: {configuration?.Id} - Force update: {forceUpdate}", nameof(UpdateAsync));
            }
        }

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="configurationId">The id of the Configuration being deleted.</param>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public virtual Task RemoveAsync(string configurationId)
        {
            return RemoveAsync(configurationId, CancellationToken.None);
        }

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="configurationId">The id of the configurationId being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public virtual Task RemoveAsync(string configurationId, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Removing configuration: {configurationId}", nameof(RemoveAsync));

            try
            {
                EnsureInstanceNotClosed();

                if (string.IsNullOrWhiteSpace(configurationId))
                {
                    throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrWhitespace, "configurationId"));
                }

                // use wild-card ETag
                var eTag = new ETagHolder { ETag = "*" };
                return RemoveAsync(configurationId, eTag, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(RemoveAsync)} threw an exception: {ex}", nameof(RemoveAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Removing configuration: {configurationId}", nameof(RemoveAsync));
            }
        }

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="configuration">The Configuration being deleted.</param>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public virtual Task RemoveAsync(Configuration configuration)
        {
            return RemoveAsync(configuration, CancellationToken.None);
        }

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="configuration">The Configuration being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public virtual Task RemoveAsync(Configuration configuration, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Removing configuration: {configuration?.Id}", nameof(RemoveAsync));
            try
            {
                EnsureInstanceNotClosed();

                return string.IsNullOrWhiteSpace(configuration.ETag)
                    ? throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingConfiguration)
                    : RemoveAsync(configuration.Id, configuration, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(RemoveAsync)} threw an exception: {ex}", nameof(RemoveAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Removing configuration: {configuration?.Id}", nameof(RemoveAsync));
            }
        }

        private Task RemoveAsync(string configurationId, IETagHolder eTagHolder, CancellationToken cancellationToken)
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

        /// <summary>
        /// Applies configuration content to an Edge device to create a deployment.
        /// </summary>
        /// <remarks><see cref="ConfigurationContent.ModulesContent"/> is required.
        /// <see cref="ConfigurationContent.DeviceContent"/> is optional.
        /// <see cref="ConfigurationContent.ModuleContent"/> is not applicable.</remarks>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="content">The configuration.</param>
        public virtual Task ApplyConfigurationContentOnDeviceAsync(string deviceId, ConfigurationContent content)
        {
            return ApplyConfigurationContentOnDeviceAsync(deviceId, content, CancellationToken.None);
        }

        /// <summary>
        /// Applies configuration content to an Edge device.
        /// </summary>
        /// <remarks><see cref="ConfigurationContent.ModulesContent"/> is required.
        /// <see cref="ConfigurationContent.DeviceContent"/> is optional.
        /// <see cref="ConfigurationContent.ModuleContent"/> is not applicable.</remarks>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="content">The configuration.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual Task ApplyConfigurationContentOnDeviceAsync(string deviceId, ConfigurationContent content, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Applying configuration content on device: {deviceId}", nameof(ApplyConfigurationContentOnDeviceAsync));

            try
            {
                return _httpClientHelper.PostAsync(GetApplyConfigurationOnDeviceRequestUri(deviceId), content, null, null, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ApplyConfigurationContentOnDeviceAsync)} threw an exception: {ex}", nameof(ApplyConfigurationContentOnDeviceAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Applying configuration content on device: {deviceId}", nameof(ApplyConfigurationContentOnDeviceAsync));
            }
        }

        private void EnsureInstanceNotClosed()
        {
            if (_httpClientHelper == null)
            {
                // TODO - update ApiResources......
                throw new ObjectDisposedException("ConfigurationsClient", ApiResources.RegistryManagerInstanceAlreadyClosed);
            }
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
    }
}
