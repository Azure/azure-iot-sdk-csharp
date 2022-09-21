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
        private const string RequestUriFormat = "/devices/{0}";
        private const string TwinUriFormat = "/twins/{0}";
        private const string ModuleTwinUriFormat = "/twins/{0}/modules/{1}";
        private const string InvalidImportMode = "InvalidImportMode";

        private static readonly TimeSpan s_regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);

        // HttpMethod does not define PATCH in its enum in .netstandard 2.0, so this is the only way to create an HTTP patch request.
        private static readonly HttpMethod s_patch = new("PATCH");

        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;

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
        /// <exception cref="ArgumentNullException">When the provided <paramref name="deviceId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the <paramref name="deviceId"/> is empty or white space.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> GetAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting device twin: {deviceId}", nameof(GetAsync));

            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetTwinUri(deviceId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Twin>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Getting device twin: {deviceId} threw an exception: {ex}", nameof(GetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting device twin: {deviceId}", nameof(GetAsync));
            }
        }

        /// <summary>
        /// Gets a module's twin from IoT hub.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>The module twin.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the <paramref name="deviceId"/> or <paramref name="moduleId"/> is empty or white space.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> GetAsync(string deviceId, string moduleId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting device module twin: {deviceId}/{moduleId}", nameof(GetAsync));

            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNullOrWhiteSpace(moduleId, nameof(moduleId));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                    HttpMethod.Get,
                    GetModuleTwinRequestUri(deviceId, moduleId),
                    _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Twin>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Getting device module twin {deviceId}/{moduleId} threw an exception: {ex}", nameof(GetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting device module twin: {deviceId}/{moduleId}", nameof(GetAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of a module's twin.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="onlyIfUnchanged">
        /// If false, this operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.PreconditionFailed"/>
        /// if the provided device identity has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated device twin.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="deviceId"/> or <paramref name="twinPatch"/> is null.</exception>
        /// <exception cref="ArgumentException">If the <paramref name="deviceId"/> is empty or white space.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> UpdateAsync(string deviceId, Twin twinPatch, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNull(twinPatch, nameof(twinPatch));

            return await UpdateInternalAsync(deviceId, twinPatch, twinPatch.ETag, false, onlyIfUnchanged, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the mutable fields of a module's twin.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="onlyIfUnchanged">
        /// If false, this operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.PreconditionFailed"/>
        /// if the provided device/module identity has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated device twin.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="deviceId"/>, <paramref name="moduleId"/>,
        /// or <paramref name="twinPatch"/> is null.</exception>
        /// <exception cref="ArgumentException">If the <paramref name="deviceId"/> or <paramref name="moduleId"/> is empty or white space.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> UpdateAsync(
            string deviceId,
            string moduleId,
            Twin twinPatch,
            bool onlyIfUnchanged = false,
            CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNullOrWhiteSpace(moduleId, nameof(moduleId));
            Argument.AssertNotNull(twinPatch, nameof(twinPatch));

            return await UpdateInternalAsync(
                    deviceId,
                    moduleId,
                    twinPatch,
                    twinPatch.ETag,
                    false,
                    onlyIfUnchanged,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Update the mutable fields for a list of module twins previously created within the system.
        /// </summary>
        /// <param name="twins">List of <see cref="Twin"/>s with updated fields.</param>
        /// <param name="onlyIfUnchanged">
        /// If false, this operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.PreconditionFailed"/>
        /// if the provided device identity has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>updated module twin.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="twins"/> is null.</exception>
        /// <exception cref="ArgumentException">When the provided <paramref name="twins"/> enumeration is empty.</exception>
        /// <exception cref="InvalidOperationException">When a twin in the enumeration is null.</exception>
        /// <exception cref="InvalidOperationException">When a twin is missing an expected ETag.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<BulkRegistryOperationResult> UpdateAsync(IEnumerable<Twin> twins, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Updating twins", nameof(UpdateAsync));

            Argument.AssertNotNullOrEmpty(twins, nameof(twins));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                IEnumerable<ExportImportDevice> devices = GenerateExportImportDeviceListForTwinBulkOperations(
                    twins,
                    onlyIfUnchanged ? ImportMode.UpdateTwinIfMatchETag : ImportMode.UpdateTwin);

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, GetBulkRequestUri(), _credentialProvider, devices);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<BulkRegistryOperationResult>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Updating twins threw an exception: {ex}", nameof(UpdateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Updating twins", nameof(UpdateAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of a device's twin.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="newTwin">New twin object to replace with.</param>
        /// <param name="onlyIfUnchanged">
        /// If false, this operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.PreconditionFailed"/>
        /// if the provided device identity has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>updated twins.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="deviceId"/> or <paramref name="newTwin"/> is null.</exception>
        /// <exception cref="ArgumentException">When the provided <paramref name="deviceId"/> is empty or white space.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> ReplaceAsync(string deviceId, Twin newTwin, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNull(newTwin, nameof(newTwin));

            return await UpdateInternalAsync(deviceId, newTwin, newTwin.ETag, true, onlyIfUnchanged, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the mutable fields of a module's twin.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="newTwin">New Twin object to replace with.</param>
        /// <param name="onlyIfUnchanged">
        /// If false, this operation will be performed even if the provided device identity has
        /// an out of date ETag. If true, the operation will throw a <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.PreconditionFailed"/>
        /// if the provided device/module identity has an out of date ETag. An up-to-date ETag can be
        /// retrieved using <see cref="GetAsync(string, string, CancellationToken)"/>.
        /// </param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated device twin.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="deviceId"/>, <paramref name="moduleId"/>,
        /// or <paramref name="newTwin"/> is null.</exception>
        /// <exception cref="ArgumentException">When the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> is empty or white space.</exception>
        /// <exception cref="IotHubServiceException">
        /// If IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<Twin> ReplaceAsync(string deviceId, string moduleId, Twin newTwin, bool onlyIfUnchanged = false, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNullOrWhiteSpace(moduleId, nameof(moduleId));
            Argument.AssertNotNull(newTwin, nameof(newTwin));

            return await UpdateInternalAsync(deviceId, moduleId, newTwin, newTwin.ETag, true, onlyIfUnchanged, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Twin> UpdateInternalAsync(
            string deviceId,
            Twin twin,
            ETag etag,
            bool isReplace,
            bool onlyIfUnchanged = false,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating device twin: {deviceId} - is replace: {isReplace}", nameof(UpdateAsync));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                twin.DeviceId = deviceId;

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                    isReplace ? HttpMethod.Put : s_patch,
                    GetTwinUri(deviceId),
                    _credentialProvider,
                    twin);
                HttpMessageHelper.ConditionallyInsertETag(request, etag, onlyIfUnchanged);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Twin>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Updating device twin {deviceId} - is replace {isReplace} threw an exception: {ex}", nameof(UpdateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Updating device twin: {deviceId} - is replace: {isReplace}", nameof(UpdateAsync));
            }
        }

        private async Task<Twin> UpdateInternalAsync(
            string deviceId,
            string moduleId,
            Twin twin,
            ETag etag,
            bool isReplace,
            bool onlyIfUnchanged = false,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating device module twin: {deviceId}/{moduleId} - is replace: {isReplace}", nameof(UpdateAsync));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                twin.DeviceId = deviceId;
                twin.ModuleId = moduleId;

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                    isReplace ? HttpMethod.Put : s_patch,
                    GetModuleTwinRequestUri(deviceId, moduleId),
                    _credentialProvider,
                    twin);
                HttpMessageHelper.ConditionallyInsertETag(request, etag, onlyIfUnchanged);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<Twin>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Updating device {deviceId}/{moduleId} - is replace {isReplace} threw an exception: {ex}", nameof(UpdateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Updating device twin: {deviceId}/{moduleId} - is replace: {isReplace}", nameof(UpdateAsync));
            }
        }

        private static IEnumerable<ExportImportDevice> GenerateExportImportDeviceListForTwinBulkOperations(IEnumerable<Twin> twins, ImportMode importMode)
        {
            Debug.Assert(twins != null);
            Debug.Assert(twins.Any());

            var exportImportDeviceList = new List<ExportImportDevice>(twins.Count());

            foreach (Twin twin in twins)
            {
                if (twin == null)
                {
                    throw new InvalidOperationException("Null twin in bulk update.");
                }

                switch (importMode)
                {
                    case ImportMode.UpdateTwin:
                        // No preconditions
                        break;

                    case ImportMode.UpdateTwinIfMatchETag:
                        if (string.IsNullOrWhiteSpace(twin.ETag.ToString()))
                        {
                            throw new InvalidOperationException($"Twin: {twin.DeviceId} missing an ETag when conditionally updating.");
                        }
                        break;

                    default:
                        Debug.Fail($"{InvalidImportMode} {importMode} for bulk twin update.");
                        break;
                }

                var exportImportDevice = new ExportImportDevice
                {
                    Id = twin.DeviceId,
                    ModuleId = twin.ModuleId,
                    ImportMode = importMode,
                    TwinETag = twin.ETag,
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
            return new Uri(string.Format(CultureInfo.InvariantCulture, ModuleTwinUriFormat, deviceId, moduleId), UriKind.Relative);
        }

        private static Uri GetTwinUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(string.Format(CultureInfo.InvariantCulture, TwinUriFormat, deviceId), UriKind.Relative);
        }

        private static Uri GetBulkRequestUri()
        {
            return new Uri(string.Format(CultureInfo.InvariantCulture, RequestUriFormat, string.Empty), UriKind.Relative);
        }
    }
}
