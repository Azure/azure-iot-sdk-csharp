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
    /// Subclient of <see cref="IotHubServiceClient"/> that handles all module registry operations including
    /// getting/adding/setting/deleting module identities.
    /// </summary>
    public class ModulesClient
    {
        private string _hostName;
        private IotHubConnectionProperties _credentialProvider;
        private HttpClient _httpClient;
        private HttpRequestMessageFactory _httpRequestMessageFactory;

        private const string ModulesRequestUriFormat = "/devices/{0}/modules/{1}";

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected ModulesClient()
        {
        }

        internal ModulesClient(string hostName, IotHubConnectionProperties credentialProvider, HttpClient httpClient, HttpRequestMessageFactory httpRequestMessageFactory)
        {
            _credentialProvider = credentialProvider;
            _hostName = hostName;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
        }

        /// <summary>
        /// Add a module identity to your IoT hub's registry.
        /// </summary>
        /// <param name="module">The module identity to register.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The registered module with the generated keys and ETags.</returns>
        public virtual async Task<Module> AddAsync(Module module, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Adding module: {module?.Id} to device: {module?.DeviceId}", nameof(AddAsync));

            try
            {
                Argument.RequireNotNull(module, nameof(module));

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetModulesRequestUri(module.DeviceId, module.Id), _credentialProvider, module);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<Module>(response, cancellationToken);
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
                    Logging.Exit(this, $"Adding module: {module?.Id} to device: {module?.DeviceId}", nameof(AddAsync));
            }
        }

        /// <summary>
        /// Get a module identity by its Id and by the Id of the device it is registered on.
        /// </summary>
        /// <param name="moduleId">The unique identifier of the module identity to retrieve.</param>
        /// <param name="deviceId">The unique identifier of the device identity that the module is registered on.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The retrieved module identity.</returns>
        public virtual async Task<Module> GetAsync(string deviceId, string moduleId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting module: {moduleId} from device: {deviceId}", nameof(GetAsync));

            try
            {
                Argument.RequireNotNullOrEmpty(deviceId, nameof(deviceId));
                Argument.RequireNotNullOrEmpty(moduleId, nameof(moduleId));

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetModulesRequestUri(deviceId, moduleId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<Module>(response, cancellationToken);
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
                    Logging.Exit(this, $"Getting module: {moduleId} from device: {deviceId}", nameof(GetAsync));
            }
        }

        /// <summary>
        /// Replace a module identity's state with the provided module identity's state.
        /// </summary>
        /// <param name="module">The module identity's new state.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The newly updated module identity including its new ETag.</returns>
        public virtual async Task<Module> SetAsync(Module module, CancellationToken cancellationToken = default)
        {
            return await SetAsync(module, false, cancellationToken);
        }

        /// <summary>
        /// Replace a module identity's state with the provided module identity's state.
        /// </summary>
        /// <param name="module">The module identity's new state.</param>
        /// <param name="forceUpdate">
        /// If true, this update operation will execute even if the provided device identity has
        /// an out of date ETag. If false, the operation will throw a <see cref="PreconditionFailedException"/>
        /// if the provided module identity has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>The newly updated device identity including its new ETag.</returns>
        public virtual async Task<Module> SetAsync(Module module, bool forceUpdate, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating module: {module?.Id} on device: {module?.DeviceId}", nameof(SetAsync));

            try
            {
                Argument.RequireNotNull(module, nameof(module));

                if (string.IsNullOrWhiteSpace(module.ETag) && !forceUpdate)
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingDevice);
                }

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Put, GetModulesRequestUri(module.DeviceId, module.Id), _credentialProvider, module);
                HttpMessageHelper2.InsertEtag(request, module.ETag);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<Module>(response, cancellationToken);
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
                    Logging.Exit(this, $"Updating module: {module?.Id} on device: {module?.DeviceId}", nameof(SetAsync));
            }
        }

        /// <summary>
        /// Delete the module identity with the provided Id from your device with the provided Id from IoT hub's registry.
        /// </summary>
        /// <param name="deviceId">The Id of the device identity that contains the module to be deleted.</param>
        /// <param name="moduleId">The Id of the module identity to be deleted.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual async Task DeleteAsync(string deviceId, string moduleId, CancellationToken cancellationToken = default)
        {
            Argument.RequireNotNullOrEmpty(deviceId, nameof(deviceId));
            Argument.RequireNotNullOrEmpty(moduleId, nameof(moduleId));

            var module = new Module(deviceId, moduleId);
            module.ETag = HttpMessageHelper2.ETagForce;
            await DeleteAsync(module, cancellationToken);
        }

        /// <summary>
        /// Delete the device identity with the provided Id from your IoT hub's registry.
        /// </summary>
        /// <param name="module">
        /// The module identity to delete from your IoT hub's registry. If the provided module's ETag
        /// is out of date, this operation will throw a <see cref="PreconditionFailedException"/>
        /// An up-to-date ETag can be retrieved using <see cref="GetAsync(string, string, CancellationToken)"/>.
        /// To force the operation to execute regardless of ETag, set the module identity's ETag to "*" or
        /// use <see cref="DeleteAsync(string, string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        public virtual async Task DeleteAsync(Module module, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Deleting module: {module?.Id} from device: {module?.DeviceId}", nameof(DeleteAsync));

            try
            {
                Argument.RequireNotNull(module, nameof(module));

                if (module.ETag == null)
                {
                    throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingDevice);
                }

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Delete, GetModulesRequestUri(module.DeviceId, module.Id), _credentialProvider);
                HttpMessageHelper2.InsertEtag(request, module.ETag);
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
                    Logging.Exit(this, $"Deleting module: {module?.Id} from device: {module?.DeviceId}", nameof(DeleteAsync));
            }
        }

        private static Uri GetModulesRequestUri(string deviceId, string moduleId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            moduleId = WebUtility.UrlEncode(moduleId);
            return new Uri(ModulesRequestUriFormat.FormatInvariant(deviceId, moduleId), UriKind.Relative);
        }
    }
}
