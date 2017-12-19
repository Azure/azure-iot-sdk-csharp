// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Service.Auth;
#if !WINDOWS_UWP && !NETSTANDARD1_3 && !NETSTANDARD2_0
    using System.Net.Http.Formatting;
#endif

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal class ContractApiHttp : IContractApiHttp
    {
        private readonly Uri _baseAddress;
        private readonly IAuthorizationHeaderProvider _authenticationHeaderProvider;
        private HttpClient _httpClientObj;
        private HttpClient _httpClientObjWithPerRequestTimeout;

        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromSeconds(100);

        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <param name="baseAddress">the <code>Uri</code> HTTP endpoint in the service.</param>
        /// <param name="authenticationHeaderProvider">the <see cref="IAuthorizationHeaderProvider"/> that will provide the 
        ///     authorization token for the HTTP communication.</param>
        /// <param name="preRequestActionForAllRequests">the function with the HTTP pre-request actions.</param>
        public ContractApiHttp(
            Uri baseAddress,
            IAuthorizationHeaderProvider authenticationHeaderProvider,
            Action<HttpClient> preRequestActionForAllRequests)
        {
            _baseAddress = baseAddress;
            _authenticationHeaderProvider = authenticationHeaderProvider;

            _httpClientObj = new HttpClient();
            _httpClientObj.BaseAddress = _baseAddress;
            _httpClientObj.Timeout = s_defaultOperationTimeout;
            _httpClientObj.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(CommonConstants.MediaTypeForDeviceManagementApis));
            _httpClientObj.DefaultRequestHeaders.ExpectContinue = false;

            _httpClientObjWithPerRequestTimeout = new HttpClient();
            _httpClientObjWithPerRequestTimeout.BaseAddress = _baseAddress;
            _httpClientObjWithPerRequestTimeout.Timeout = Timeout.InfiniteTimeSpan;
            _httpClientObjWithPerRequestTimeout.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(CommonConstants.MediaTypeForDeviceManagementApis));
            _httpClientObjWithPerRequestTimeout.DefaultRequestHeaders.ExpectContinue = false;

            if (preRequestActionForAllRequests != null)
            {
                preRequestActionForAllRequests(_httpClientObj);
                preRequestActionForAllRequests(_httpClientObjWithPerRequestTimeout);
            }
        }

        /// <summary>
        /// Unified HTTP request API
        /// </summary>
        /// <param name="httpMethod">the <see cref="HttpMethod"/> with the HTTP verb.</param>
        /// <param name="requestUri">the rest API <see cref="Uri"/> with for the requested service.</param>
        /// <param name="customHeaders">the optional <code>Dictionary</code> with additional header fields. It can be <code>null</code>.</param>
        /// <param name="body">the <code>string</code> with the message body. It can be <code>null</code> or empty.</param>
        /// <param name="ifMatch">the optional <code>string</code> with the match condition, normally an eTag. It can be <code>null</code>.</param>
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

            using (HttpRequestMessage msg = new HttpRequestMessage(httpMethod, requestUri))
            {
                if (!string.IsNullOrEmpty(body))
                {
                    msg.Content = new StringContent(body, Encoding.UTF8, CommonConstants.MediaTypeForDeviceManagementApis);
                }

                msg.Headers.Add(HttpRequestHeader.Authorization.ToString(), _authenticationHeaderProvider.GetAuthorizationHeader());
                msg.Headers.Add(HttpRequestHeader.UserAgent.ToString(), Utils.GetClientVersion());
                if (customHeaders != null)
                {
                    foreach (var header in customHeaders)
                    {
                        msg.Headers.Add(header.Key, header.Value);
                    }
                }
                InsertIfMatch(msg, ifMatch);

                try
                {
                    using (HttpResponseMessage httpResponse = await _httpClientObj.SendAsync(msg, cancellationToken).ConfigureAwait(false))
                    {
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
                }
                catch(AggregateException ex)
                {
                    var innerExceptions = ex.Flatten().InnerExceptions;
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
            if (response.StatusCode >= HttpStatusCode.InternalServerError)
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

            StringBuilder quotedIfMatch = new StringBuilder();

            if (!ifMatch.StartsWith("\"", StringComparison.OrdinalIgnoreCase))
            {
                quotedIfMatch.Append("\"");
            }

            quotedIfMatch.Append(ifMatch);

            if (!ifMatch.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
            {
                quotedIfMatch.Append("\"");
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
            if(disposing)
            {
                if(_httpClientObj != null)
                {
                    _httpClientObj.Dispose();
                    _httpClientObj = null;
                }

                if(_httpClientObjWithPerRequestTimeout != null)
                {
                    _httpClientObjWithPerRequestTimeout.Dispose();
                    _httpClientObjWithPerRequestTimeout = null;
                }
            }
        }
    }
}