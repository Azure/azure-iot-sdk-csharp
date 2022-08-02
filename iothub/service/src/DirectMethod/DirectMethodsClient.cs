using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Http2;

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
        /// <param name="cloudToDeviceMethod">Parameters to execute a direct method on the device.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The <see cref="CloudToDeviceMethodResult"/>.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="deviceId"/> is null.</exception>
        /// <exception cref="ArgumentNullException">When <paramref name="cloudToDeviceMethod"/> is null.</exception>
        /// <exception cref="ArgumentException">When <paramref name="deviceId"/> is empty or whitespace.</exception>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-direct-methods"/>
        public virtual async Task<CloudToDeviceMethodResult> InvokeAsync(string deviceId, CloudToDeviceMethod cloudToDeviceMethod, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Invoking device method for device id: {deviceId}", nameof(InvokeAsync));

            try
            {
                Argument.RequireNotNullOrEmpty(deviceId, nameof(deviceId));
                Argument.RequireNotNull(cloudToDeviceMethod, nameof(cloudToDeviceMethod));

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, GetDeviceMethodUri(deviceId), _credentialProvider, cloudToDeviceMethod);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<CloudToDeviceMethodResult>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(InvokeAsync)} threw an exception: {ex}", nameof(InvokeAsync));
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
        /// <param name="cloudToDeviceMethod">Parameters to execute a direct method on the module.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The <see cref="CloudToDeviceMethodResult"/>.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="deviceId"/> or <paramref name="moduleId"/> are null.</exception>
        /// <exception cref="ArgumentNullException">When <paramref name="cloudToDeviceMethod"/> is null.</exception>
        /// <exception cref="ArgumentException">When <paramref name="deviceId"/> or <paramref name="moduleId"/> are empty or whitespace.</exception>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-direct-methods"/>
        public virtual async Task<CloudToDeviceMethodResult> InvokeAsync(string deviceId, string moduleId, CloudToDeviceMethod cloudToDeviceMethod, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Invoking device method for device id: {deviceId} and module id: {moduleId}", nameof(InvokeAsync));

            try
            {
                Argument.RequireNotNullOrEmpty(deviceId, nameof(deviceId));
                Argument.RequireNotNullOrEmpty(moduleId, nameof(moduleId));
                Argument.RequireNotNull(cloudToDeviceMethod, nameof(cloudToDeviceMethod));

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, GetModuleMethodUri(deviceId, moduleId), _credentialProvider, cloudToDeviceMethod);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response);
                return await HttpMessageHelper2.DeserializeResponse<CloudToDeviceMethodResult>(response, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(InvokeAsync)} threw an exception: {ex}", nameof(InvokeAsync));
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
            return new Uri(ModuleMethodUriFormat.FormatInvariant(deviceId, moduleId), UriKind.Relative);
        }

        private static Uri GetDeviceMethodUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(DeviceMethodUriFormat.FormatInvariant(deviceId), UriKind.Relative);
        }
    }
}
