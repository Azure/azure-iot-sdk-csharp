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
    /// Subclient of <see cref="IotHubServiceClient"/> to directly invoke direct methods on devices and modules in IoT hub.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-direct-methods"/>
    public class DirectMethodsClient
    {
        private const string DeviceMethodUriFormat = "/twins/{0}/methods";
        private const string ModuleMethodUriFormat = "/twins/{0}/modules/{1}/methods";


        private string _hostName;
        private IotHubConnectionProperties _credentialProvider;
        private HttpClient _httpClient;
        private HttpRequestMessageFactory _httpRequestMessageFactory;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected DirectMethodsClient()
        {
        }

        internal DirectMethodsClient(string hostName, IotHubConnectionProperties credentialProvider, HttpClient httpClient, HttpRequestMessageFactory httpRequestMessageFactory)
        {
            _credentialProvider = credentialProvider;
            _hostName = hostName;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
        }

        /// <summary>
        /// Interactively invokes a method on a device.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="cloudToDeviceMethod">Parameters to execute a direct method on the device.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The <see cref="CloudToDeviceMethodResult"/>.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="deviceId"/> is null.</exception>
        /// <exception cref="ArgumentNullException">When <paramref name="cloudToDeviceMethod"/> is null.</exception>
        /// <exception cref="ArgumentException">When <paramref name="deviceId"/> is empty or whitespace.</exception>
        /// <seealso href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-direct-methods"/>
        public virtual Task<CloudToDeviceMethodResult> InvokeAsync(string deviceId, CloudToDeviceMethod cloudToDeviceMethod, CancellationToken cancellationToken = default)
        {
            Argument.RequireNotNullOrEmpty(deviceId, nameof(deviceId));
            Argument.RequireNotNull(cloudToDeviceMethod, nameof(cloudToDeviceMethod));

            return InvokeAsync(GetDeviceMethodUri(deviceId), cloudToDeviceMethod, cancellationToken);
        }

        /// <summary>
        /// Interactively invokes a method on a module.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="moduleId">The module identifier for the target module.</param>
        /// <param name="cloudToDeviceMethod">Parameters to execute a direct method on the module.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The <see cref="CloudToDeviceMethodResult"/>.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="deviceId"/> or <paramref name="moduleId"/> are null.</exception>
        /// <exception cref="ArgumentNullException">When <paramref name="cloudToDeviceMethod"/> is null.</exception>
        /// <exception cref="ArgumentException">When <paramref name="deviceId"/> or <paramref name="moduleId"/> are empty or whitespace.</exception>
        /// <seealso href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-direct-methods"/>
        public virtual Task<CloudToDeviceMethodResult> InvokeAsync(string deviceId, string moduleId, CloudToDeviceMethod cloudToDeviceMethod, CancellationToken cancellationToken = default)
        {
            Argument.RequireNotNullOrEmpty(deviceId, nameof(deviceId));
            Argument.RequireNotNullOrEmpty(moduleId, nameof(moduleId));
            Argument.RequireNotNull(cloudToDeviceMethod, nameof(cloudToDeviceMethod));

            return InvokeAsync(GetModuleMethodUri(deviceId, moduleId), cloudToDeviceMethod, cancellationToken);
        }

        private async Task<CloudToDeviceMethodResult> InvokeAsync(Uri uri,CloudToDeviceMethod cloudToDeviceMethod,CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Invoking device method for: {uri}", nameof(InvokeAsync));

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Post, uri, _credentialProvider, cloudToDeviceMethod);
                TimeSpan timeout = GetInvokeDeviceMethodOperationTimeout(cloudToDeviceMethod);
                request.Headers.Add(HttpRequestHeader.Expires.ToString(), timeout.ToString());
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
                    Logging.Exit(this, $"Invoking device method for: {uri}", nameof(InvokeAsync));
            }
        }

        private static TimeSpan GetInvokeDeviceMethodOperationTimeout(CloudToDeviceMethod cloudToDeviceMethod)
        {
            // For InvokeDeviceMethod, we need to take into account the timeouts specified
            // for the Device to connect and send a response. We also need to take into account
            // the transmission time for the request send/receive
            var timeout = TimeSpan.FromSeconds(15); // For wire time
            var connectionTimeOut = TimeSpan.FromSeconds(cloudToDeviceMethod.ConnectionTimeoutInSeconds ?? 0);
            var responseTimeOut = TimeSpan.FromSeconds(cloudToDeviceMethod.ResponseTimeoutInSeconds ?? 0);

            ValidateTimeOut(connectionTimeOut);
            ValidateTimeOut(responseTimeOut);

            timeout += connectionTimeOut;
            timeout += responseTimeOut;

            return timeout;
        }

        private static void ValidateTimeOut(TimeSpan connectionTimeOut)
        {
            if (connectionTimeOut.Seconds < 0)
            {
                throw new ArgumentException("Negative timeout");
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
