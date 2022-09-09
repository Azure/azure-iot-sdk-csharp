// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> that handles all module registry operations including
    /// getting/creating/setting/deleting module identities.
    /// </summary>
    public class ModulesClient
    {
        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;

        private const string ModulesRequestUriFormat = "/devices/{0}/modules/{1}";
        private const string ETagNotSetWhileUpdatingDevice = "ETagNotSetWhileUpdatingDevice";
        private const string ETagNotSetWhileDeletingDevice = "ETagNotSetWhileDeletingDevice";

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected ModulesClient()
        {
        }

        internal ModulesClient(string hostName, IotHubConnectionProperties credentialProvider, HttpClient httpClient, HttpRequestMessageFactory httpRequestMessageFactory)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
        }

        /// <summary>
        /// Create a module identity in your IoT hub's registry.
        /// </summary>
        /// <param name="module">The module identity to register.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The registered module with the generated keys and ETags.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided module is null.</exception>
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
        public virtual async Task<Module> CreateAsync(Module module, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Creating module: {module?.Id} on device: {module?.DeviceId}", nameof(CreateAsync));

            try
            {
                Argument.AssertNotNull(module, nameof(module));

                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetModulesRequestUri(module.DeviceId, module.Id), _credentialProvider, module);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Module>(response).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Creating module: {module?.Id} on device: {module?.DeviceId}", nameof(CreateAsync));
            }
        }

        /// <summary>
        /// Get a module identity by its Id and by the Id of the device it is registered on.
        /// </summary>
        /// <param name="moduleId">The unique identifier of the module identity to retrieve.</param>
        /// <param name="deviceId">The unique identifier of the device identity that the module is registered on.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The retrieved module identity.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided device Id or module Id is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided device Id or module Id is empty or whitespace.</exception>
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
        public virtual async Task<Module> GetAsync(string deviceId, string moduleId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting module: {moduleId} on device: {deviceId}", nameof(GetAsync));

            try
            {
                Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
                Argument.AssertNotNullOrWhiteSpace(moduleId, nameof(moduleId));

                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetModulesRequestUri(deviceId, moduleId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Module>(response).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Getting module: {moduleId} on device: {deviceId}", nameof(GetAsync));
            }
        }

        /// <summary>
        /// Replace a module identity's state with the provided module identity's state.
        /// </summary>
        /// <param name="module">The module identity's new state.</param>
        /// <param name="onlyIfUnchanged">
        /// If false, this operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="PreconditionFailedException"/>
        /// if the provided module has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The newly updated module identity including its new ETag.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided module is null.</exception>
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
        public virtual async Task<Module> SetAsync(Module module, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating module: {module?.Id} on device: {module?.DeviceId} - only if changed: {onlyIfUnchanged}", nameof(SetAsync));

            try
            {
                Argument.AssertNotNull(module, nameof(module));
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetModulesRequestUri(module.DeviceId, module.Id), _credentialProvider, module);
                HttpMessageHelper.ConditionallyInsertETag(request, module.ETag, onlyIfUnchanged);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Module>(response).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Updating module: {module?.Id} on device: {module?.DeviceId} - only if changed: {onlyIfUnchanged}", nameof(SetAsync));
            }
        }

        /// <summary>
        /// Delete the module identity with the provided Id from the device with the provided Id from your IoT hub's registry.
        /// </summary>
        /// <param name="deviceId">The Id of the device identity that contains the module to be deleted.</param>
        /// <param name="moduleId">The Id of the module identity to be deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided device Id or module Id is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided device Id or module Id is empty or whitespace.</exception>
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
        public virtual async Task DeleteAsync(string deviceId, string moduleId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNullOrWhiteSpace(moduleId, nameof(moduleId));

            var module = new Module(deviceId, moduleId);
            module.ETag = new ETag(HttpMessageHelper.ETagForce);
            await DeleteAsync(module, default, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete the module identity from your IoT hub's registry.
        /// </summary>
        /// <param name="module">
        /// The module identity to delete from your IoT hub's registry. If the provided module's ETag
        /// is out of date, this operation will throw a <see cref="PreconditionFailedException"/>
        /// An up-to-date ETag can be retrieved using <see cref="GetAsync(string, string, CancellationToken)"/>.
        /// To force the operation to execute regardless of ETag, set the module identity's ETag to "*" or
        /// use <see cref="DeleteAsync(string, string, CancellationToken)"/>.
        /// </param>
        /// <param name="onlyIfUnchanged">
        /// If false, this delete operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="PreconditionFailedException"/>
        /// if the provided module has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided module is null.</exception>
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
        public virtual async Task DeleteAsync(Module module, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Deleting module: {module?.Id} on device: {module?.DeviceId} - only if changed: {onlyIfUnchanged}", nameof(DeleteAsync));

            try
            {
                Argument.AssertNotNull(module, nameof(module));
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Delete, GetModulesRequestUri(module.DeviceId, module.Id), _credentialProvider);
                HttpMessageHelper.ConditionallyInsertETag(request, module.ETag, onlyIfUnchanged);
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
                    Logging.Exit(this, $"Deleting module: {module?.Id} on device: {module?.DeviceId} - only if changed: {onlyIfUnchanged}", nameof(DeleteAsync));
            }
        }

        private static Uri GetModulesRequestUri(string deviceId, string moduleId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            moduleId = WebUtility.UrlEncode(moduleId);
            return new Uri(string.Format(CultureInfo.InvariantCulture, ModulesRequestUriFormat, deviceId, moduleId), UriKind.Relative);
        }
    }
}
