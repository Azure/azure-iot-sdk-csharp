using System;
using System.Collections.Generic;
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
    /// Subclient of <see cref="IotHubServiceClient"/> that handles creating, getting, setting and deleting configurations.
    /// </summary>
    public class ConfigurationsClient
    {
        private string _hostName;
        private IotHubConnectionProperties _credentialProvider;
        private HttpClient _httpClient;
        private HttpRequestMessageFactory _httpRequestMessageFactory;

        private const string ConfigurationRequestUriFormat = "/configurations/{0}";
        private const string ConfigurationsRequestUriFormat = "&top={0}";

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected ConfigurationsClient()
        {
        }

        internal ConfigurationsClient(string hostName, IotHubConnectionProperties credentialProvider, HttpClient httpClient, HttpRequestMessageFactory httpRequestMessageFactory)
        {
            _credentialProvider = credentialProvider;
            _hostName = hostName;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
        }

        /// <summary>
        /// Creates a new configuration for Azure IoT Edge in IoT hub
        /// </summary>
        /// <param name="configuration">The configuration object being created.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided configuration is null.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public async virtual Task<Configuration> CreateAsync(Configuration configuration, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Adding configuration: {configuration?.Id}", nameof(CreateAsync));

            try
            {
                Argument.RequireNotNull(configuration, nameof(configuration));
                if (!string.IsNullOrEmpty(configuration.ETag))
                {
                    throw new ArgumentException(ApiResources.ETagSetWhileCreatingConfiguration);
                }
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetConfigurationRequestUri(configuration.Id), _credentialProvider, configuration);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<Configuration>(response, cancellationToken);
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
        /// Retrieves the specified configuration object.
        /// </summary>
        /// <param name="configurationId">The id of the configuration being retrieved.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided configuration Id is null.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public async virtual Task<Configuration> GetAsync(string configurationId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting configuration: {configurationId}", nameof(GetAsync));
            try
            {
                Argument.RequireNotNull(configurationId, nameof(configurationId));
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetConfigurationRequestUri(configurationId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<Configuration>(response, cancellationToken);
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
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public async virtual Task<IEnumerable<Configuration>> GetAsync(int maxCount, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting configuration: max count: {maxCount}", nameof(GetAsync));
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetConfigurationRequestUri(""), _credentialProvider, null, ConfigurationsRequestUriFormat.FormatInvariant(maxCount));
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<IEnumerable<Configuration>>(response, cancellationToken);
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
        /// Replace the mutable fields of the configuration registration
        /// </summary>
        /// <param name="configuration">The configuration object with replaced fields.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object with replaced ETags.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided configuration is null.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public virtual Task<Configuration> SetAsync(Configuration configuration, CancellationToken cancellationToken = default)
        {
            return SetAsync(configuration, false, cancellationToken);
        }

        /// <summary>
        /// replace the mutable fields of the configuration registration
        /// </summary>
        /// <param name="configuration">The configuration object with replaced fields.</param>
        /// <param name="forceUpdate">Forces the configuration object to be replaced even if it was replaced since it was retrieved last time.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The Configuration object with replaced ETags.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided configuration is null.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public async virtual Task<Configuration> SetAsync(Configuration configuration, bool forceUpdate, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating configuration: {configuration?.Id} - Force update: {forceUpdate}", nameof(SetAsync));

            try
            {
                Argument.RequireNotNull(configuration, nameof(configuration));
                if (string.IsNullOrWhiteSpace(configuration.ETag) && !forceUpdate)
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingConfiguration);
                }
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetConfigurationRequestUri(configuration.Id), _credentialProvider, configuration);
                HttpMessageHelper2.InsertEtag(request, configuration.ETag);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<Configuration>(response, cancellationToken);
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
                    Logging.Exit(this, $"Updating configuration: {configuration?.Id} - Force update: {forceUpdate}", nameof(SetAsync));
            }
        }

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="configurationId">The id of the configurationId being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided configuration Id is null.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public async virtual Task DeleteAsync(string configurationId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Deleting configuration: {configurationId}", nameof(DeleteAsync));

            try
            {
                Argument.RequireNotNull(configurationId, nameof(configurationId));
                cancellationToken.ThrowIfCancellationRequested();

                // use wild-card ETag
                var eTag = new ETagHolder { ETag = "*" };
                await DeleteAsync(configurationId, eTag, cancellationToken);
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
                    Logging.Exit(this, $"Deleting configuration: {configurationId}", nameof(DeleteAsync));
            }
        }

        /// <summary>
        /// Deletes a previously registered device from the system.
        /// </summary>
        /// <param name="configuration">The configuration being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided configuration is null.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public async virtual Task DeleteAsync(Configuration configuration, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Deleting configuration: {configuration?.Id}", nameof(DeleteAsync));
            try
            {
                Argument.RequireNotNull(configuration, nameof(configuration));
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(configuration.ETag)) 
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingConfiguration);
                }
                await DeleteAsync(configuration.Id, configuration, cancellationToken);
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
                    Logging.Exit(this, $"Deleting configuration: {configuration?.Id}", nameof(DeleteAsync));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configurationId"></param>
        /// <param name="eTagHolder"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DeleteAsync(string configurationId, IETagHolder eTagHolder, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Deleting configuration: {configurationId}", nameof(DeleteAsync));
            try
            {
                Argument.RequireNotNull(configurationId, nameof(configurationId));
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(eTagHolder.ETag))
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingConfiguration);
                }

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Delete, GetConfigurationRequestUri(configurationId), _credentialProvider);
                HttpMessageHelper2.InsertEtag(request, eTagHolder.ETag);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.NoContent, response);
            }
            catch(Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(DeleteAsync)} threw an exception: {ex}", nameof(DeleteAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Deleting configuration: {configurationId}", nameof(DeleteAsync));
            }

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
        public async virtual Task ApplyConfigurationContentOnDeviceAsync(string deviceId, ConfigurationContent content, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Applying configuration content on device: {deviceId}", nameof(ApplyConfigurationContentOnDeviceAsync));

            try
            {
                Argument.RequireNotNull(deviceId, nameof(deviceId));
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetConfigurationRequestUri(deviceId), _credentialProvider, content);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
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

        private static Uri GetConfigurationRequestUri(string configurationId)
        {
            configurationId = WebUtility.UrlEncode(configurationId);
            return new Uri(ConfigurationRequestUriFormat.FormatInvariant(configurationId), UriKind.Relative);
        }
    }
}
