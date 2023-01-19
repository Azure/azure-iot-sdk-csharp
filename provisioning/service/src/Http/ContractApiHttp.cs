// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal class ContractApiHttp : IContractApiHttp
    {
        private const string MediaTypeForDeviceManagementApis = "application/json";

        private readonly Uri _baseAddress;
        private readonly IAuthorizationHeaderProvider _authenticationHeaderProvider;

        private HttpClientHandler _httpClientHandler;
        private HttpClient _httpClientObj;

        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromSeconds(100);

        // These default values are consistent with Azure.Core default values:
        // https://github.com/Azure/azure-sdk-for-net/blob/7e3cf643977591e9041f4c628fd4d28237398e0b/sdk/core/Azure.Core/src/Pipeline/ServicePointHelpers.cs#L28
        private const int DefaultMaxConnectionsPerServer = 50;

        // How long, in milliseconds, a given cached TCP connection created by this client's HTTP layer will live before being closed.
        // If this value is set to any negative value, the connection lease will be infinite. If this value is set to 0, then the TCP connection will close after
        // each HTTP request and a new TCP connection will be opened upon the next request.
        //
        // By closing cached TCP connections and opening a new one upon the next request, the underlying HTTP client has a chance to do a DNS lookup
        // to validate that it will send the requests to the correct IP address. While it is atypical for a given IoT hub to change its IP address, it does
        // happen when a given IoT hub fails over into a different region.
        //
        // This default value is consistent with the default value used in Azure.Core
        // https://github.com/Azure/azure-sdk-for-net/blob/7e3cf643977591e9041f4c628fd4d28237398e0b/sdk/core/Azure.Core/src/Pipeline/ServicePointHelpers.cs#L29
        private static readonly TimeSpan s_defaultConnectionLeaseTimeout = TimeSpan.FromMinutes(5);

        public ContractApiHttp(
            Uri baseAddress,
            IAuthorizationHeaderProvider authenticationHeaderProvider,
            ProvisioningServiceClientOptions options)
        {
            _baseAddress = baseAddress;
            _authenticationHeaderProvider = authenticationHeaderProvider;

            if (options.HttpClient != null)
            {
                _httpClientObj = options.HttpClient;
                return;
            }

            _httpClientHandler = new HttpClientHandler
            {
                // Cannot specify a specific protocol here, as desired due to an error:
                //   ProvisioningDeviceClient_ValidRegistrationId_AmqpWithProxy_SymmetricKey_RegisterOk_GroupEnrollment failing for me with System.PlatformNotSupportedException: Operation is not supported on this platform.
                // When revisiting TLS12 work for DPS, we should figure out why. Perhaps the service needs to support it.

                //SslProtocols = TlsVersions.Preferred,
                CheckCertificateRevocationList = options.ProvisioningServiceHttpSettings.CertificateRevocationCheck
            };

            if (options.ProvisioningServiceHttpSettings.Proxy != null)
            {
                _httpClientHandler.UseProxy = true;
                _httpClientHandler.Proxy = options.ProvisioningServiceHttpSettings.Proxy;
            }

            _httpClientHandler.MaxConnectionsPerServer = DefaultMaxConnectionsPerServer;
            ServicePoint servicePoint = ServicePointManager.FindServicePoint(_baseAddress);
            servicePoint.ConnectionLeaseTimeout = s_defaultConnectionLeaseTimeout.Milliseconds;

            _httpClientObj = new HttpClient(_httpClientHandler, false)
            {
                BaseAddress = _baseAddress,
                Timeout = s_defaultOperationTimeout,
            };

            _httpClientObj.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeForDeviceManagementApis));
            _httpClientObj.DefaultRequestHeaders.ExpectContinue = false;
        }

        /// <summary>
        /// Unified HTTP request API
        /// </summary>
        /// <param name="httpMethod">the <see cref="HttpMethod"/> with the HTTP verb.</param>
        /// <param name="requestUri">the rest API <see cref="Uri"/> with for the requested service.</param>
        /// <param name="customHeaders">the optional Dictionary with additional header fields. It can be null.</param>
        /// <param name="body">the string with the message body. It can be null or empty.</param>
        /// <param name="eTag">the optional string with the match condition, normally an eTag. It can be null.</param>
        /// <param name="cancellationToken">the task cancellation Token.</param>
        /// <returns>The <see cref="ContractApiResponse"/> with the HTTP response.</returns>
        /// <exception cref="OperationCanceledException">If the cancellation was requested.</exception>
        /// <exception cref="ProvisioningServiceException">If there is an error in the HTTP communication
        /// between client and service or the service answers the request with error status.</exception>
        public async Task<ContractApiResponse> RequestAsync(
            HttpMethod httpMethod,
            Uri requestUri,
            IDictionary<string, string> customHeaders,
            string body,
            ETag eTag,
            CancellationToken cancellationToken)
        {
            ContractApiResponse response;

            using var msg = new HttpRequestMessage(
                httpMethod,
                new Uri($"{requestUri}?{SdkUtils.ApiVersionQueryString}", UriKind.Relative));
            if (!string.IsNullOrEmpty(body))
            {
                msg.Content = new StringContent(body, Encoding.UTF8, MediaTypeForDeviceManagementApis);
            }

            msg.Headers.Add(HttpRequestHeader.Authorization.ToString(), _authenticationHeaderProvider.GetAuthorizationHeader());
            msg.Headers.Add(HttpRequestHeader.UserAgent.ToString(), Utils.GetClientVersion());
            if (customHeaders != null)
            {
                foreach (KeyValuePair<string, string> header in customHeaders)
                {
                    msg.Headers.Add(header.Key, header.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(eTag.ToString()))
            {
                // Azure.Core.ETag expects the format "H" for serializing ETags that go into the header.
                // https://github.com/Azure/azure-sdk-for-net/blob/9c6238e0f0dd403d6583b56ec7902c77c64a2e37/sdk/core/Azure.Core/src/ETag.cs#L87-L114
                string escapedETag = eTag.ToString("H");
                msg.Headers.IfMatch.Add(new EntityTagHeaderValue(escapedETag));
            }

            try
            {
                using HttpResponseMessage httpResponse = await _httpClientObj.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                if (httpResponse == null)
                {
                    throw new ProvisioningServiceException(
                        $"The response message was null when executing operation {httpMethod}.", isTransient: true);
                }

                response = new ContractApiResponse(
                    await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false),
                    httpResponse.StatusCode,
                    httpResponse.Headers.ToDictionary(x => x.Key, x => x.Value.FirstOrDefault()),
                    httpResponse.ReasonPhrase);
            }
            catch (AggregateException ex)
            {
                ReadOnlyCollection<Exception> innerExceptions = ex.Flatten().InnerExceptions;
                if (innerExceptions.Any(e => e is TimeoutException))
                {
                    throw new ProvisioningServiceException(ex.Message, ex, true);
                }

                throw;
            }
            catch (TimeoutException ex)
            {
                throw new ProvisioningServiceException(ex.Message, HttpStatusCode.RequestTimeout, ex);
            }
            catch (IOException ex)
            {
                throw new ProvisioningServiceException(ex.Message, ex, true);
            }
            catch (HttpRequestException ex)
            {
                if (ContainsAuthenticationException(ex))
                {
                    throw new ProvisioningServiceException(ex.Message, HttpStatusCode.Unauthorized, ex);
                }
                throw new ProvisioningServiceException(ex.Message, ex, true);
            }
            catch (TaskCanceledException ex)
            {
                // Unfortunately TaskCanceledException is thrown when HttpClient times out.
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(ex.Message, ex);
                }

                throw new ProvisioningServiceException($"The {httpMethod} operation timed out.", HttpStatusCode.RequestTimeout, ex);
            }

            ValidateHttpResponse(response);

            return response;
        }

        private static bool ContainsAuthenticationException(Exception ex)
        {
            return ex != null
                && (ex is AuthenticationException
                    || ContainsAuthenticationException(ex.InnerException));
        }

        private static void ValidateHttpResponse(ContractApiResponse response)
        {
            if (response.Body == null)
            {
                throw new ProvisioningServiceException(response.ErrorMessage, response.StatusCode, response.Fields);
            }

            // Both 200 and 204 indicate a successful operation, so there is no reason to parse the body for an error code
            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NoContent)
            {
                try
                {
                    ResponseBody responseBody = JsonConvert.DeserializeObject<ResponseBody>(response.Body);

                    if (response.StatusCode >= HttpStatusCode.Ambiguous)
                    {
                        throw new ProvisioningServiceException(
                            $"{response.ErrorMessage}:{responseBody.Message}",
                            response.StatusCode,
                            responseBody.ErrorCode,
                            responseBody.TrackingId,
                            response.Fields);
                    }
                }
                catch (JsonException jex)
                {
                    throw new ProvisioningServiceException(
                        $"Fail to deserialize the received response body: {response.Body}",
                        jex,
                        false);
                }
            }
        }

        /// <summary>
        /// Release all HTTP resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_httpClientObj != null)
                {
                    _httpClientObj.Dispose();
                    _httpClientObj = null;
                }

                if (_httpClientHandler != null)
                {
                    _httpClientHandler.Dispose();
                    _httpClientHandler = null;
                }
            }
        }
    }
}
