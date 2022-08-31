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
using Microsoft.Azure.Devices.Common.Exceptions;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> that handles getting, updating, and replacing device and module twins.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-csharp-csharp-twin-getstarted"/>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-csharp-csharp-module-twin-getstarted"/>
    public class TwinsClient
    {
        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;

        private const string RequestUriFormat = "/devices/{0}";
        private const string TwinUriFormat = "/twins/{0}";
        private const string ModuleTwinUriFormat = "/twins/{0}/modules/{1}";
        private const string ETagNotSetWhileUpdatingTwin = "ETagNotSetWhileUpdatingTwin";
        private const string InvalidImportMode = "InvalidImportMode";
        private static readonly TimeSpan s_regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);

        // HttpMethod does not define PATCH in its enum in .netstandard 2.0, so this is the only way to create an HTTP patch request.
        private readonly HttpMethod _patch = new HttpMethod("PATCH");

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected TwinsClient()
        {
        }

        internal TwinsClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            HttpClient httpClient,
            HttpRequestMessageFactory httpRequestMessageFactory)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
        }

        /// <summary>
        /// Gets a device's twin from IoT hub.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>The device twin.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="deviceId"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> GetAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting device twin on device: {deviceId}", nameof(GetAsync));
            try
            {
                Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetTwinUri(deviceId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Twin>(response).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Getting device twin on device: {deviceId}", nameof(GetAsync));
            }
        }

        /// <summary>
        /// Gets a module's twin from IoT hub.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>The module twin.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="deviceId"/> or <paramref name="moduleId"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> GetAsync(string deviceId, string moduleId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting device twin on device: {deviceId} and module: {moduleId}", nameof(GetAsync));

            try
            {
                Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
                Argument.AssertNotNullOrWhiteSpace(moduleId, nameof(moduleId));
                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetModuleTwinRequestUri(deviceId, moduleId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Twin>(response).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Getting device twin on device: {deviceId} and module: {moduleId}", nameof(GetAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of a module's twin.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated device twin.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="twinPatch"/> or <paramref name="etag"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="deviceId"/> or <paramref name="etag"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> UpdateAsync(string deviceId, Twin twinPatch, string etag, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating device twin on device: {deviceId}", nameof(UpdateAsync));
            try
            {
                Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
                Argument.AssertNotNullOrWhiteSpace(etag, nameof(etag));
                Argument.AssertNotNull(twinPatch, nameof(twinPatch));
                cancellationToken.ThrowIfCancellationRequested();
                return await UpdateInternalAsync(deviceId, twinPatch, etag, false, cancellationToken).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Updating device twin on device: {deviceId}", nameof(UpdateAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of a device's twin.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="jsonTwinPatch">Twin json with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated device twin.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="jsonTwinPatch"/> or <paramref name="etag"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="deviceId"/> or <paramref name="jsonTwinPatch"/> or <paramref name="etag"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> UpdateAsync(string deviceId, string jsonTwinPatch, string etag, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating device twin on device: {deviceId}", nameof(UpdateAsync));

            try
            {
                Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
                Argument.AssertNotNullOrWhiteSpace(jsonTwinPatch, nameof(jsonTwinPatch));
                Argument.AssertNotNullOrWhiteSpace(etag, nameof(etag));
                cancellationToken.ThrowIfCancellationRequested();

                // TODO: Do we need to deserialize Twin, only to serialize it again?
                Twin twin = JsonConvert.DeserializeObject<Twin>(jsonTwinPatch);
                return await UpdateAsync(deviceId, twin, etag, cancellationToken).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Updating device twin on device: {deviceId}", nameof(UpdateAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of a module's twin.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated device twin.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> or <paramref name="twinPatch"/> or <paramref name="etag"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="deviceId"/> or <paramref name="moduleId"/> or <paramref name="etag"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> UpdateAsync(string deviceId, string moduleId, Twin twinPatch, string etag, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating device twin on device: {deviceId}", nameof(UpdateAsync));
            try
            {
                Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
                Argument.AssertNotNullOrWhiteSpace(moduleId, nameof(moduleId));
                Argument.AssertNotNullOrWhiteSpace(etag, nameof(etag));
                Argument.AssertNotNull(twinPatch, nameof(twinPatch));
                cancellationToken.ThrowIfCancellationRequested();

                return await UpdateInternalAsync(deviceId, moduleId, twinPatch, etag, false, cancellationToken).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Updating device twin on device: {deviceId}", nameof(UpdateAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of a module's twin.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="jsonTwinPatch">Twin json with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated module twin.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> or <paramref name="jsonTwinPatch"/> or <paramref name="etag"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="deviceId"/> or <paramref name="moduleId"/> or <paramref name="jsonTwinPatch"/> or <paramref name="etag"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> UpdateAsync(string deviceId, string moduleId, string jsonTwinPatch, string etag, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating device twin on device: {deviceId} and module: {moduleId}", nameof(UpdateAsync));
            try
            {
                Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
                Argument.AssertNotNullOrWhiteSpace(moduleId, nameof(moduleId));
                Argument.AssertNotNullOrWhiteSpace(etag, nameof(etag));
                Argument.AssertNotNull(jsonTwinPatch, nameof(jsonTwinPatch));
                cancellationToken.ThrowIfCancellationRequested();

                // TODO: Do we need to deserialize Twin, only to serialize it again?
                Twin twin = JsonConvert.DeserializeObject<Twin>(jsonTwinPatch);
                return await UpdateAsync(deviceId, moduleId, twin, etag, cancellationToken).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Updating device twin on device: {deviceId} and module: {moduleId}", nameof(UpdateAsync));
            }
        }

        /// <summary>
        /// Update the mutable fields for a list of module twins previously created within the system.
        /// </summary>
        /// <param name="twins">List of <see cref="Twin"/>s with updated fields.</param>
        /// <param name="forceUpdate">Forces the <see cref="Twin"/> object to be updated even if it has changed since it was retrieved last time.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>updated module twin.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="twins"/> is null.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<BulkRegistryOperationResult> UpdateAsync(IEnumerable<Twin> twins, bool forceUpdate = false, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating device twins.", nameof(UpdateAsync));
            try
            {
                Argument.AssertNotNull(twins, nameof(twins));
                cancellationToken.ThrowIfCancellationRequested();

                return await BulkDeviceOperationsAsync<BulkRegistryOperationResult>(
                GenerateExportImportDeviceListForTwinBulkOperations(twins, forceUpdate ? ImportMode.UpdateTwin : ImportMode.UpdateTwinIfMatchETag),
                ClientApiVersionHelper.ApiVersionQueryString,
                cancellationToken).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Updating device twins.", nameof(UpdateAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of a device's twin.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="newTwin">New Twin object to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>updated twins.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="newTwin"/> or <paramref name="etag"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="etag"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> ReplaceAsync(string deviceId, Twin newTwin, string etag, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Replacing device twin on device: {deviceId}", nameof(ReplaceAsync));
            try
            {
                Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
                Argument.AssertNotNullOrWhiteSpace(etag, nameof(etag));
                Argument.AssertNotNull(newTwin, nameof(newTwin));
                cancellationToken.ThrowIfCancellationRequested();

                return await UpdateInternalAsync(deviceId, newTwin, etag, true, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ReplaceAsync)} threw an exception: {ex}", nameof(ReplaceAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Replacing device twin on device: {deviceId}", nameof(ReplaceAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of a device's twin.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="newTwinJson">New Twin json to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated device twin.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="newTwinJson"/> or <paramref name="etag"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="newTwinJson"/> or <paramref name="etag"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> ReplaceAsync(string deviceId, string newTwinJson, string etag, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Replacing device twin on device: {deviceId}", nameof(ReplaceAsync));
            try
            {
                Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
                Argument.AssertNotNullOrWhiteSpace(newTwinJson, nameof(newTwinJson));
                Argument.AssertNotNullOrWhiteSpace(etag, nameof(etag));
                cancellationToken.ThrowIfCancellationRequested();

                // TODO: Do we need to deserialize Twin, only to serialize it again?
                Twin twin = JsonConvert.DeserializeObject<Twin>(newTwinJson);
                return await ReplaceAsync(deviceId, twin, etag, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ReplaceAsync)} threw an exception: {ex}", nameof(ReplaceAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Replacing device twin on device: {deviceId}", nameof(ReplaceAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of a module's twin.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="newTwin">New Twin object to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated device twin.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> or <paramref name="newTwin"/> or <paramref name="etag"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> or <paramref name="etag"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> ReplaceAsync(string deviceId, string moduleId, Twin newTwin, string etag, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Replacing device twin on device: {deviceId} and module: {moduleId}", nameof(ReplaceAsync));
            try
            {
                Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
                Argument.AssertNotNullOrWhiteSpace(moduleId, nameof(moduleId));
                Argument.AssertNotNullOrWhiteSpace(etag, nameof(etag));
                Argument.AssertNotNull(newTwin, nameof(newTwin));
                cancellationToken.ThrowIfCancellationRequested();

                return await UpdateInternalAsync(deviceId, moduleId, newTwin, etag, true, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ReplaceAsync)} threw an exception: {ex}", nameof(ReplaceAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Replacing device twin on device: {deviceId} and module: {moduleId}", nameof(ReplaceAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of a module's twin.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="newTwinJson">New Twin json to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated module twin.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> or <paramref name="newTwinJson"/> or <paramref name="etag"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> or <paramref name="newTwinJson"/> or <paramref name="etag"/> is empty or white space.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> ReplaceAsync(string deviceId, string moduleId, string newTwinJson, string etag, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Replacing device twin on device: {deviceId} and module: {moduleId}", nameof(ReplaceAsync));
            try
            {
                Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
                Argument.AssertNotNullOrWhiteSpace(moduleId, nameof(moduleId));
                Argument.AssertNotNullOrWhiteSpace(etag, nameof(etag));
                Argument.AssertNotNullOrWhiteSpace(newTwinJson, nameof(newTwinJson));
                cancellationToken.ThrowIfCancellationRequested();
                // TODO: Do we need to deserialize Twin, only to serialize it again?
                Twin twin = JsonConvert.DeserializeObject<Twin>(newTwinJson);
                return await ReplaceAsync(deviceId, moduleId, twin, etag, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ReplaceAsync)} threw an exception: {ex}", nameof(ReplaceAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Replacing device twin on device: {deviceId} and module: {moduleId}", nameof(ReplaceAsync));
            }
        }

        private async Task<Twin> UpdateInternalAsync(string deviceId, Twin twin, string etag, bool isReplace, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Replacing device twin on device: {deviceId} - is replace: {isReplace}", nameof(UpdateAsync));
            try
            {
                twin.DeviceId = deviceId;

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(isReplace ? HttpMethod.Put : _patch, GetTwinUri(deviceId), _credentialProvider, twin);
                HttpMessageHelper.ConditionallyInsertETag(request, etag);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Twin>(response).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Replacing device twin on device: {deviceId} - is replace: {isReplace}", nameof(UpdateAsync));
            }
        }

        private async Task<Twin> UpdateInternalAsync(string deviceId, string moduleId, Twin twin, string etag, bool isReplace, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Replacing device twin on device: {deviceId} - module: {moduleId} - is replace: {isReplace}", nameof(UpdateAsync));
            try
            {
                twin.DeviceId = deviceId;
                twin.ModuleId = moduleId;

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(isReplace ? HttpMethod.Put : _patch, GetModuleTwinRequestUri(deviceId, moduleId), _credentialProvider, twin);
                HttpMessageHelper.ConditionallyInsertETag(request, etag);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Twin>(response).ConfigureAwait(false);
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
                    Logging.Exit(this, $"Replacing device twin on device: {deviceId} - module: {moduleId} - is replace: {isReplace}", nameof(UpdateAsync));
            }
        }

        private async Task<T> BulkDeviceOperationsAsync<T>(IEnumerable<ExportImportDevice> devices, string version, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Performing bulk device operation on : {devices?.Count()} devices. version: {version}", nameof(BulkDeviceOperationsAsync));
            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, GetBulkRequestUri(), _credentialProvider, devices);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<T>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(BulkDeviceOperationsAsync)} threw an exception: {ex}", nameof(BulkDeviceOperationsAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Performing bulk device operation on : {devices?.Count()} devices. version: {version}", nameof(BulkDeviceOperationsAsync));
            }
        }

        private static IEnumerable<ExportImportDevice> GenerateExportImportDeviceListForTwinBulkOperations(IEnumerable<Twin> twins, ImportMode importMode)
        {
            if (!twins.Any())
            {
                throw new ArgumentException($"Parameter {nameof(twins)} cannot be empty");
            }

            var exportImportDeviceList = new List<ExportImportDevice>(twins.Count());
            foreach (Twin twin in twins)
            {
                if (twin == null)
                {
                    throw new ArgumentNullException(nameof(twin));
                }
                switch (importMode)
                {
                    case ImportMode.UpdateTwin:
                        // No preconditions
                        break;

                    case ImportMode.UpdateTwinIfMatchETag:
                        if (string.IsNullOrWhiteSpace(twin.ETag))
                        {
                            throw new ArgumentException(ETagNotSetWhileUpdatingTwin);
                        }
                        break;

                    default:
                        throw new ArgumentException($"{InvalidImportMode} {importMode}.");
                }

                var exportImportDevice = new ExportImportDevice
                {
                    Id = twin.DeviceId,
                    ModuleId = twin.ModuleId,
                    ImportMode = importMode,
                    TwinETag = new ETag(twin.ETag),
                    Tags = twin.Tags,
                    Properties = new ExportImportDevice.PropertyContainer(),
                };
                exportImportDevice.Properties.DesiredProperties = twin.Properties?.Desired;

                exportImportDeviceList.Add(exportImportDevice);
            }

            return exportImportDeviceList;
        }

        private static Uri GetModuleTwinRequestUri(string deviceId, string moduleId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            moduleId = WebUtility.UrlEncode(moduleId);
            return new Uri(ModuleTwinUriFormat.FormatInvariant(deviceId, moduleId), UriKind.Relative);
        }

        private static Uri GetTwinUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(TwinUriFormat.FormatInvariant(deviceId), UriKind.Relative);
        }

        private static Uri GetBulkRequestUri()
        {
            return new Uri(RequestUriFormat.FormatInvariant(string.Empty), UriKind.Relative);
        }
    }
}
