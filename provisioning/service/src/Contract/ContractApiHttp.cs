﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private static readonly TimeSpan DefaultConnectionLeaseTimeout = TimeSpan.FromMinutes(5);

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
                _httpClientHandler = null;
                return;
            }

            _httpClientHandler = new HttpClientHandler
            {
                // Cannot specify a specific protocol here, as desired due to an error:
                //   ProvisioningDeviceClient_ValidRegistrationId_AmqpWithProxy_SymmetricKey_RegisterOk_GroupEnrollment failing for me with System.PlatformNotSupportedException: Operation is not supported on this platform.
                // When revisiting TLS12 work for DPS, we should figure out why. Perhaps the service needs to support it.

                //SslProtocols = TlsVersions.Preferred,
                CheckCertificateRevocationList = options.CertificateRevocationCheck
            };

            if (options.Proxy != null)
            {
                _httpClientHandler.UseProxy = true;
                _httpClientHandler.Proxy = options.Proxy;
            }

            _httpClientHandler.MaxConnectionsPerServer = DefaultMaxConnectionsPerServer;
            ServicePoint servicePoint = ServicePointManager.FindServicePoint(_baseAddress);
            servicePoint.ConnectionLeaseTimeout = DefaultConnectionLeaseTimeout.Milliseconds;

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
        /// <param name="customHeaders">the optional <c>Dictionary</c> with additional header fields. It can be <c>null</c>.</param>
        /// <param name="body">the <c>string</c> with the message body. It can be <c>null</c> or empty.</param>
        /// <param name="ifMatch">the optional <c>string</c> with the match condition, normally an eTag. It can be <c>null</c>.</param>
        /// <param name="cancellationToken">the task cancellation Token.</param>
        /// <returns>The <see cref="ContractApiResponse"/> with the HTTP response.</returns>
        /// <exception cref="ProvisioningServiceClientException">if the cancellation was requested.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if there is a error in the HTTP communication
        ///     between client and service.</exception>
        /// <exception cref="ProvisioningServiceClientHttpException">if the service answer the request with error status.</exception>
        public async Task<ContractApiResponse> RequestAsync(
            HttpMethod httpMethod,
            Uri requestUri,
            IDictionary<string, string> customHeaders,
            string body,
            string ifMatch,
            CancellationToken cancellationToken)
        {
            ContractApiResponse response;

            using (var msg = new HttpRequestMessage(httpMethod, requestUri))
            {
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
                InsertIfMatch(msg, ifMatch);

                try
                {
                    using HttpResponseMessage httpResponse = await _httpClientObj.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                    if (httpResponse == null)
                    {
                        throw new ProvisioningServiceClientTransportException(
                            $"The response message was null when executing operation {httpMethod}.");
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
                        throw new ProvisioningServiceClientTransportException(ex.Message, ex);
                    }

                    throw;
                }
                catch (TimeoutException ex)
                {
                    throw new ProvisioningServiceClientTransportException(ex.Message, ex);
                }
                catch (IOException ex)
                {
                    throw new ProvisioningServiceClientTransportException(ex.Message, ex);
                }
                catch (HttpRequestException ex)
                {
                    throw new ProvisioningServiceClientTransportException(ex.Message, ex);
                }
                catch (TaskCanceledException ex)
                {
                    // Unfortunately TaskCanceledException is thrown when HttpClient times out.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new ProvisioningServiceClientException(ex.Message, ex);
                    }

                    throw new ProvisioningServiceClientTransportException($"The {httpMethod} operation timed out.", ex);
                }
            }

            ValidateHttpResponse(response);

            return response;
        }

        private static void ValidateHttpResponse(ContractApiResponse response)
        {
            if (response.StatusCode >= HttpStatusCode.InternalServerError ||
                (int)response.StatusCode == 429)
            {
                throw new ProvisioningServiceClientHttpException(response, isTransient: true);
            }
            else if (response.StatusCode >= HttpStatusCode.Ambiguous)
            {
                throw new ProvisioningServiceClientHttpException(response, isTransient: false);
            }
        }

        private static void InsertIfMatch(HttpRequestMessage requestMessage, string ifMatch)
        {
            if (string.IsNullOrWhiteSpace(ifMatch))
            {
                return;
            }

            var quotedIfMatch = new StringBuilder();

            if (!ifMatch.StartsWith("\"", StringComparison.OrdinalIgnoreCase))
            {
                quotedIfMatch.Append('"');
            }

            quotedIfMatch.Append(ifMatch);

            if (!ifMatch.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
            {
                quotedIfMatch.Append('"');
            }

            requestMessage.Headers.IfMatch.Add(new EntityTagHeaderValue(quotedIfMatch.ToString()));
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
