// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> to invoke direct methods on devices and modules in IoT hub.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-direct-methods"/>
    public class DirectMethodsClient
    {
        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;
        private const string DeviceMethodUriFormat = "/twins/{0}/methods";
        private const string ModuleMethodUriFormat = "/twins/{0}/modules/{1}/methods";

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected DirectMethodsClient()
        {
        }

        internal DirectMethodsClient(string hostName, IotHubConnectionProperties credentialProvider, HttpClient httpClient, HttpRequestMessageFactory httpRequestMessageFactory)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
        }

        /// <summary>
        /// Invokes a method on a device.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="directMethodRequest">Parameters to execute a direct method on the device.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The direct method response.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="deviceId"/> or <paramref name="directMethodRequest"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="deviceId"/> is empty or white space.</exception>
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
        public virtual async Task<DirectMethodResponse> InvokeAsync(string deviceId, DirectMethodRequest directMethodRequest, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Invoking device method for device id: {deviceId}", nameof(InvokeAsync));

            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNull(directMethodRequest, nameof(directMethodRequest));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, GetDeviceMethodUri(deviceId), _credentialProvider, directMethodRequest);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<DirectMethodResponse>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Invoking device method for device id {deviceId} threw an exception: {ex}", nameof(InvokeAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Invoking device method for device id: {deviceId}", nameof(InvokeAsync));
            }
        }

        /// <summary>
        /// Invokes a method on a module.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="moduleId">The module identifier for the target module.</param>
        /// <param name="directMethodRequest">Parameters to execute a direct method on the module.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The direct method response.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="deviceId"/> or <paramref name="moduleId"/> or <paramref name="directMethodRequest"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="deviceId"/> or <paramref name="moduleId"/> is empty or white space.</exception>
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
        public virtual async Task<DirectMethodResponse> InvokeAsync(string deviceId, string moduleId, DirectMethodRequest directMethodRequest, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Invoking device method for device id: {deviceId} and module id: {moduleId}", nameof(InvokeAsync));

            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            Argument.AssertNotNullOrWhiteSpace(moduleId, nameof(moduleId));
            Argument.AssertNotNull(directMethodRequest, nameof(directMethodRequest));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, GetModuleMethodUri(deviceId, moduleId), _credentialProvider, directMethodRequest);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                return await HttpMessageHelper.DeserializeResponseAsync<DirectMethodResponse>(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Invoking device method for device id {deviceId} and module id {moduleId} threw an exception: {ex}", nameof(InvokeAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Invoking device method for device id: {deviceId} and module id: {moduleId}", nameof(InvokeAsync));
            }
        }

        private static Uri GetModuleMethodUri(string deviceId, string moduleId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            moduleId = WebUtility.UrlEncode(moduleId);

            return new Uri(
                string.Format(
                    CultureInfo.InvariantCulture,
                    ModuleMethodUriFormat,
                    deviceId,
                    moduleId),
                UriKind.Relative);
        }

        private static Uri GetDeviceMethodUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);

            return new Uri(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DeviceMethodUriFormat,
                    deviceId),
                UriKind.Relative);
        }
    }
}
