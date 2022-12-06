// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> that handles creating, getting, setting and deleting configurations.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
    public class ConfigurationsClient
    {
        private const string ConfigurationRequestUriFormat = "/configurations/{0}";
        private const string ConfigurationsRequestUriFormat = "&top={0}";
        private const string ArgumentMustBeNonNegative = "ArgumentMustBeNonNegative";

        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;
        private readonly RetryHandler _internalRetryHandler;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected ConfigurationsClient()
        { }

        internal ConfigurationsClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            HttpClient httpClient,
            HttpRequestMessageFactory httpRequestMessageFactory,
            RetryHandler retryHandler)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
            _internalRetryHandler = retryHandler;
        }

        /// <summary>
        /// Creates a new configuration for Azure IoT Edge in IoT hub.
        /// </summary>
        /// <param name="configuration">The configuration object being created.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The configuration object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="configuration"/> is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Configuration> CreateAsync(Configuration configuration, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Creating configuration: {configuration?.Id}", nameof(CreateAsync));

            Argument.AssertNotNull(configuration, nameof(configuration));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetConfigurationRequestUri(configuration.Id), _credentialProvider, configuration);
                HttpResponseMessage response = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Configuration>(response).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, ex);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Creating configuration {configuration?.Id} threw an exception: {ex}", nameof(CreateAsync));
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
        /// <returns>The configuration object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="configurationId"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="configurationId"/> is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Configuration> GetAsync(string configurationId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting configuration: {configurationId}", nameof(GetAsync));

            Argument.AssertNotNullOrWhiteSpace(configurationId, nameof(configurationId));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetConfigurationRequestUri(configurationId), _credentialProvider);
                HttpResponseMessage response = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Configuration>(response).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, ex);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Getting configuration {configurationId} threw an exception: {ex}", nameof(GetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting configuration: {configurationId}", nameof(GetAsync));
            }
        }

        /// <summary>
        /// Retrieves specified number of configuration from every IoT hub partition.
        /// Results are not ordered.
        /// </summary>
        /// <returns>The list of configuration.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="maxCount"/> value less than zero.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<IEnumerable<Configuration>> GetAsync(int maxCount, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Getting configurations.", nameof(GetAsync));

            if (maxCount < 0)
            {
                throw new ArgumentException(ArgumentMustBeNonNegative, nameof(maxCount));
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                    HttpMethod.Get,
                    GetConfigurationRequestUri(""),
                    _credentialProvider,
                    null,
                    string.Format(CultureInfo.InvariantCulture, ConfigurationsRequestUriFormat, maxCount));
                HttpResponseMessage response = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<IEnumerable<Configuration>>(response).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, ex);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Getting configurations threw an exception: {ex}", nameof(GetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Getting configurations.", nameof(GetAsync));
            }
        }

        /// <summary>
        /// replace the mutable fields of the configuration registration.
        /// </summary>
        /// <param name="configuration">The configuration object with replaced fields.</param>
        /// <param name="onlyIfUnchanged">
        /// If false, this operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.PreconditionFailed"/>
        /// if the provided configuration has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The configuration object with replaced ETags.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="configuration"/> is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<Configuration> SetAsync(Configuration configuration, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating configuration: {configuration?.Id}", nameof(SetAsync));

            Argument.AssertNotNull(configuration, nameof(configuration));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetConfigurationRequestUri(configuration.Id), _credentialProvider, configuration);
                HttpMessageHelper.ConditionallyInsertETag(request, configuration.ETag, onlyIfUnchanged);
                HttpResponseMessage response = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Configuration>(response).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, ex);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Updating configuration {configuration?.Id} threw an exception: {ex}", nameof(SetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Updating configuration: {configuration?.Id}", nameof(SetAsync));
            }
        }

        /// <summary>
        /// Deletes a configuration from IoT hub.
        /// </summary>
        /// <param name="configurationId">The id of the configuration being deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="configurationId"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="configurationId"/> is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task DeleteAsync(string configurationId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(configurationId, nameof(configurationId));

            await DeleteAsync(new Configuration(configurationId), default, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes a configuration from IoT hub.
        /// </summary>
        /// <param name="configuration">The configuration being deleted.</param>
        /// <param name="onlyIfUnchanged">
        /// If false, this delete operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.PreconditionFailed"/>
        /// if the provided configuration has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="configuration"/> is null.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task DeleteAsync(Configuration configuration, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Deleting configuration: {configuration?.Id}", nameof(DeleteAsync));

            Argument.AssertNotNull(configuration, nameof(configuration));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Delete, GetConfigurationRequestUri(configuration.Id), _credentialProvider);
                HttpMessageHelper.ConditionallyInsertETag(request, configuration.ETag, onlyIfUnchanged);
                HttpResponseMessage response = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.NoContent, response).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, ex);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Deleting configuration {configuration?.Id} threw an exception: {ex}", nameof(DeleteAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Deleting configuration: {configuration?.Id}", nameof(DeleteAsync));
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
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="content"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="deviceId"/> is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        /// <remarks>
        /// While there is no service API for getting all the configuration content for a particular Edge device, this data can be retrieved
        /// by calling <see cref="TwinsClient.GetAsync(string, string, CancellationToken)"/> with your Edge's device Id and "$edgeAgent" as the module Id.
        /// </remarks>
        public virtual async Task ApplyConfigurationContentOnDeviceAsync(string deviceId, ConfigurationContent content, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Applying configuration content on device: {deviceId}", nameof(ApplyConfigurationContentOnDeviceAsync));

            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNull(content, nameof(content));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetConfigurationRequestUri(deviceId), _credentialProvider, content);
                HttpResponseMessage response = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, ex);
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

            return new Uri(
                string.Format(
                    CultureInfo.InvariantCulture,
                    ConfigurationRequestUriFormat,
                    configurationId),
                UriKind.Relative);
        }
    }
}
