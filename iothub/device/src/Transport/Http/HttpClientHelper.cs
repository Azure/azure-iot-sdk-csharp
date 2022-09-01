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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal sealed class HttpClientHelper
    {
        private readonly Uri _baseAddress;
        private readonly IConnectionCredentials _connectionCredentials;
        private readonly IReadOnlyDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> _defaultErrorMapping;
        private readonly bool _usingX509ClientCert;
        private HttpClient _httpClientObj;
        private HttpClientHandler _httpClientHandler;
        private readonly AdditionalClientInformation _additionalClientInformation;

        public HttpClientHelper(
            Uri baseAddress,
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation additionalClientInformation,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> defaultErrorMapping,
            TimeSpan timeout,
            Action<HttpClient> preRequestActionForAllRequests,
            HttpClientHandler httpClientHandler,
            IotHubClientHttpSettings iotHubClientHttpSettings)
        {
            _baseAddress = baseAddress;
            _connectionCredentials = connectionCredentials;
            _defaultErrorMapping = new ReadOnlyDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>(defaultErrorMapping);
            _httpClientHandler = httpClientHandler ?? new HttpClientHandler();
            _httpClientHandler.SslProtocols = iotHubClientHttpSettings.SslProtocols;
            _httpClientHandler.CheckCertificateRevocationList = iotHubClientHttpSettings.CertificateRevocationCheck;

            X509Certificate2 clientCert = _connectionCredentials.Certificate;
            IWebProxy proxy = iotHubClientHttpSettings.Proxy;

            if (clientCert != null)
            {
                _httpClientHandler.ClientCertificates.Add(clientCert);
                _usingX509ClientCert = true;
            }

            if (proxy != DefaultWebProxySettings.Instance)
            {
                _httpClientHandler.UseProxy = proxy != null;
                _httpClientHandler.Proxy = proxy;
            }

            _httpClientObj = new HttpClient(_httpClientHandler)
            {
                BaseAddress = _baseAddress,
                Timeout = timeout
            };

            _httpClientObj.BaseAddress = _baseAddress;
            _httpClientObj.Timeout = timeout;
            _httpClientObj.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(CommonConstants.MediaTypeForDeviceManagementApis));
            _httpClientObj.DefaultRequestHeaders.ExpectContinue = false;

            preRequestActionForAllRequests?.Invoke(_httpClientObj);
            _additionalClientInformation = additionalClientInformation;
        }

        private static async Task<T> ReadResponseMessageAsync<T>(HttpResponseMessage message, CancellationToken token)
        {
            if (typeof(T) == typeof(HttpResponseMessage))
            {
                return (T)(object)message;
            }

            T entity = await ReadAsAsync<T>(message.Content, token).ConfigureAwait(false);

            // ETag in the header is considered authoritative
            if (entity is IETagHolder eTagHolder)
            {
                if (message.Headers.ETag != null && !string.IsNullOrWhiteSpace(message.Headers.ETag.Tag))
                {
                    // RDBug 3429280:Make the version field of Device object internal
                    eTagHolder.ETag = message.Headers.ETag.Tag;
                }
            }

            return entity;
        }

        private static Task AddCustomHeaders(HttpRequestMessage requestMessage, IDictionary<string, string> customHeaders)
        {
            if (customHeaders != null)
            {
                foreach (KeyValuePair<string, string> header in customHeaders)
                {
                    requestMessage.Headers.Add(header.Key, header.Value);
                }
            }

            return Task.CompletedTask;
        }

        private IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> MergeErrorMapping(
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides)
        {
            var mergedMapping = _defaultErrorMapping.ToDictionary(mapping => mapping.Key, mapping => mapping.Value);

            if (errorMappingOverrides != null)
            {
                foreach (KeyValuePair<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorOverride in errorMappingOverrides)
                {
                    mergedMapping[errorOverride.Key] = errorOverride.Value;
                }
            }

            return mergedMapping;
        }

        public async Task<T2> PostAsync<T1, T2>(
             Uri requestUri,
             T1 entity,
             IDictionary<string, string> customHeaders,
             CancellationToken cancellationToken)
        {
            T2 result = default;

            cancellationToken.ThrowIfCancellationRequested();

            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> mergedErrorMapping =
                MergeErrorMapping(ExceptionHandlingHelper.GetDefaultErrorMapping());

            using var msg = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseAddress, requestUri));
            if (!_usingX509ClientCert)
            {
                string authHeader = await _connectionCredentials.GetPasswordAsync().ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(authHeader))
                {
                    msg.Headers.Add(HttpRequestHeader.Authorization.ToString(), authHeader);
                }
            }

            msg.Headers.UserAgent.ParseAdd(_additionalClientInformation.ProductInfo?.ToString(UserAgentFormats.Http));

            Func<HttpRequestMessage, CancellationToken, Task> modifyRequestMessageAsync = (requestMsg, token) =>
            {
                AddCustomHeaders(requestMsg, customHeaders);
                if (entity != null)
                {
                    if (typeof(T1) == typeof(byte[]))
                    {
                        requestMsg.Content = new ByteArrayContent((byte[])(object)entity);
                    }
                    else if (typeof(T1) == typeof(string))
                    {
                        // only used to send batched messages on Http runtime
                        requestMsg.Content = new StringContent((string)(object)entity);
                        requestMsg.Content.Headers.ContentType = new MediaTypeHeaderValue(CommonConstants.BatchedMessageContentType);
                    }
                    else
                    {
                        requestMsg.Content = CreateContent(entity);
                    }
                }

                return Task.CompletedTask;
            };

            if (modifyRequestMessageAsync != null)
            {
                await modifyRequestMessageAsync(msg, cancellationToken).ConfigureAwait(false);
            }

            HttpResponseMessage responseMsg;
            try
            {
                responseMsg = await _httpClientObj.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                if (responseMsg == null)
                {
                    throw new InvalidOperationException(
                        $"The response message was null when executing operation {HttpMethod.Post}.");
                }
                if (responseMsg.IsSuccessStatusCode)
                {
                    result = await ReadResponseMessageAsync<T2>(responseMsg, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (AggregateException ex)
            {
                ReadOnlyCollection<Exception> innerExceptions = ex.Flatten().InnerExceptions;
                if (innerExceptions.Any(Fx.IsFatal))
                {
                    throw;
                }

                // Apparently HttpClient throws AggregateException when a timeout occurs.
                // TODO: pradeepc - need to confirm this with ASP.NET team
                if (innerExceptions.Any(e => e is TimeoutException))
                {
                    throw new IotHubClientException(ex.Message, ex, true, IotHubStatusCode.NetworkErrors);
                }

                throw new IotHubClientException(ex.Message, ex);
            }
            catch (TimeoutException ex)
            {
                throw new IotHubClientException(ex.Message, ex, true, IotHubStatusCode.NetworkErrors);
            }
            catch (IOException ex)
            {
                throw new IotHubClientException(ex.Message, ex, true, IotHubStatusCode.NetworkErrors);
            }
            catch (HttpRequestException ex)
            {
                throw new IotHubClientException(ex.Message, ex, true, IotHubStatusCode.NetworkErrors);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (!Fx.IsFatal(ex))
            {
                throw new IotHubClientException(ex.Message, ex);
            }

            if (!responseMsg.IsSuccessStatusCode)
            {
                Exception mappedEx = await MapToExceptionAsync(responseMsg, mergedErrorMapping).ConfigureAwait(false);
                throw mappedEx;
            }

            return result;
        }

        private static async Task<Exception> MapToExceptionAsync(
            HttpResponseMessage response,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMapping)
        {
            if (!errorMapping.TryGetValue(response.StatusCode, out Func<HttpResponseMessage, Task<Exception>> func))
            {
                return new IotHubClientException(
                    await ExceptionHandlingHelper.GetExceptionMessageAsync(response).ConfigureAwait(false),
                    isTransient: true);
            }

            Func<HttpResponseMessage, Task<Exception>> mapToExceptionFunc = errorMapping[response.StatusCode];
            Task<Exception> exception = mapToExceptionFunc(response);
            return await exception.ConfigureAwait(false);
        }

        private static StringContent CreateContent<T>(T entity)
        {
            return new StringContent(JsonConvert.SerializeObject(entity), Encoding.UTF8, "application/json");
        }

        private static async Task<T> ReadAsAsync<T>(HttpContent content, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            using Stream stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            return new JsonSerializer().Deserialize<T>(jsonReader);
        }
    }
}
