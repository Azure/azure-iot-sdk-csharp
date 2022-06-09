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
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal sealed class HttpClientHelper : IHttpClientHelper
    {
        private readonly Uri _baseAddress;
        private readonly IAuthorizationProvider _authenticationHeaderProvider;
        private readonly IReadOnlyDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> _defaultErrorMapping;
        private readonly bool _usingX509ClientCert;
        private HttpClient _httpClientObj;
        private HttpClientHandler _httpClientHandler;
        private bool _isDisposed;
        private readonly ProductInfo _productInfo;
        private readonly bool _isClientPrimaryTransportHandler;

        public HttpClientHelper(
            Uri baseAddress,
            IAuthorizationProvider authenticationHeaderProvider,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> defaultErrorMapping,
            TimeSpan timeout,
            Action<HttpClient> preRequestActionForAllRequests,
            X509Certificate2 clientCert,
            HttpClientHandler httpClientHandler,
            ProductInfo productInfo,
            IWebProxy proxy,
            bool isClientPrimaryTransportHandler = false)
        {
            _baseAddress = baseAddress;
            _authenticationHeaderProvider = authenticationHeaderProvider;
            _defaultErrorMapping = new ReadOnlyDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>(defaultErrorMapping);
            _httpClientHandler = httpClientHandler ?? new HttpClientHandler();
            _httpClientHandler.SslProtocols = TlsVersions.Instance.Preferred;
            _httpClientHandler.CheckCertificateRevocationList = TlsVersions.Instance.CertificateRevocationCheck;

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
            _productInfo = productInfo;
            _isClientPrimaryTransportHandler = isClientPrimaryTransportHandler;
        }

        public Task<T> GetAsync<T>(
            Uri requestUri,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            IDictionary<string, string> customHeaders,
            CancellationToken cancellationToken)
        {
            return GetAsync<T>(requestUri, errorMappingOverrides, customHeaders, true, cancellationToken);
        }

        public async Task<T> GetAsync<T>(
            Uri requestUri,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            IDictionary<string, string> customHeaders,
            bool throwIfNotFound,
            CancellationToken cancellationToken)
        {
            T result = default;

            if (throwIfNotFound)
            {
                await ExecuteAsync(
                        HttpMethod.Get,
                        new Uri(_baseAddress, requestUri),
                        (requestMsg, token) => AddCustomHeaders(requestMsg, customHeaders),
                        async (message, token) => result = await ReadResponseMessageAsync<T>(message, token).ConfigureAwait(false),
                        errorMappingOverrides,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await ExecuteAsync(
                       HttpMethod.Get,
                       new Uri(_baseAddress, requestUri),
                       (requestMsg, token) => AddCustomHeaders(requestMsg, customHeaders),
                       message => message.IsSuccessStatusCode || message.StatusCode == HttpStatusCode.NotFound,
                       async (message, token) => result = message.StatusCode == HttpStatusCode.NotFound
                           ? default
                           : await ReadResponseMessageAsync<T>(message, token).ConfigureAwait(false),
                       errorMappingOverrides,
                       cancellationToken)
                    .ConfigureAwait(false);
            }

            return result;
        }

        public async Task<T> PutAsync<T>(
            Uri requestUri,
            T entity,
            PutOperationType operationType,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            CancellationToken cancellationToken) where T : IETagHolder
        {
            T result = default;

            await ExecuteAsync(
                    HttpMethod.Put,
                    new Uri(_baseAddress, requestUri),
                    (requestMsg, token) =>
                    {
                        InsertEtag(requestMsg, entity, operationType);
                        requestMsg.Content = CreateContent(entity);
                        return TaskHelpers.CompletedTask;
                    },
                    async (httpClient, token) => result = await ReadResponseMessageAsync<T>(httpClient, token).ConfigureAwait(false),
                    errorMappingOverrides,
                    cancellationToken)
                .ConfigureAwait(false);

            return result;
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

            return TaskHelpers.CompletedTask;
        }

        private static void InsertEtag(HttpRequestMessage requestMessage, IETagHolder entity, PutOperationType operationType)
        {
            if (operationType == PutOperationType.CreateEntity)
            {
                return;
            }

            if (operationType == PutOperationType.ForceUpdateEntity)
            {
                const string etag = "\"*\"";
                requestMessage.Headers.IfMatch.Add(new EntityTagHeaderValue(etag));
            }
            else
            {
                InsertEtag(requestMessage, entity);
            }
        }

        private static void InsertEtag(HttpRequestMessage requestMessage, IETagHolder entity)
        {
            if (string.IsNullOrWhiteSpace(entity.ETag))
            {
                throw new ArgumentException("The entity does not have its ETag set.");
            }

            string etag = entity.ETag;

            if (!etag.StartsWith("\"", StringComparison.InvariantCultureIgnoreCase))
            {
                etag = "\"" + etag;
            }

            if (!etag.EndsWith("\"", StringComparison.InvariantCultureIgnoreCase))
            {
                etag += "\"";
            }

            requestMessage.Headers.IfMatch.Add(new EntityTagHeaderValue(etag));
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

        public Task PostAsync<T>(
            Uri requestUri,
            T entity,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            IDictionary<string, string> customHeaders,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                HttpMethod.Post,
                new Uri(_baseAddress, requestUri),
                (requestMsg, token) =>
                {
                    AddCustomHeaders(requestMsg, customHeaders);
                    if (entity != null)
                    {
                        if (typeof(T) == typeof(byte[]))
                        {
                            requestMsg.Content = new ByteArrayContent((byte[])(object)entity);
                        }
                        else if (typeof(T) == typeof(string))
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

                    return TaskHelpers.CompletedTask;
                },
                ReadResponseMessageAsync<HttpResponseMessage>,
                errorMappingOverrides,
                cancellationToken);
        }

        public async Task<T2> PostAsync<T1, T2>(
             Uri requestUri,
             T1 entity,
             IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
             IDictionary<string, string> customHeaders,
             CancellationToken cancellationToken)
        {
            T2 result = default;
            await PostAsyncHelper(
                    requestUri,
                    entity,
                    errorMappingOverrides,
                    customHeaders,
                    async (message, token) => result = await ReadResponseMessageAsync<T2>(message, token).ConfigureAwait(false),
                    cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        private Task PostAsyncHelper<T1>(
            Uri requestUri,
            T1 entity,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            IDictionary<string, string> customHeaders,
            Func<HttpResponseMessage, CancellationToken, Task> processResponseMessageAsync,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                HttpMethod.Post,
                new Uri(_baseAddress, requestUri),
                (requestMsg, token) =>
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

                    return TaskHelpers.CompletedTask;
                },
                processResponseMessageAsync,
                errorMappingOverrides,
                cancellationToken);
        }

        public Task DeleteAsync<T>(
            Uri requestUri,
            T entity,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            IDictionary<string, string> customHeaders,
            CancellationToken cancellationToken) where T : IETagHolder
        {
            return ExecuteAsync(
                HttpMethod.Delete,
                new Uri(_baseAddress, requestUri),
                (requestMsg, token) =>
                {
                    InsertEtag(requestMsg, entity);
                    AddCustomHeaders(requestMsg, customHeaders);
                    return TaskHelpers.CompletedTask;
                },
                null,
                errorMappingOverrides,
                cancellationToken);
        }

        private Task ExecuteAsync(
            HttpMethod httpMethod,
            Uri requestUri,
            Func<HttpRequestMessage, CancellationToken, Task> modifyRequestMessageAsync,
            Func<HttpResponseMessage, CancellationToken, Task> processResponseMessageAsync,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                httpMethod,
                requestUri,
                modifyRequestMessageAsync,
                message => message.IsSuccessStatusCode,
                processResponseMessageAsync,
                errorMappingOverrides,
                cancellationToken);
        }

        private async Task ExecuteAsync(
            HttpMethod httpMethod,
            Uri requestUri,
            Func<HttpRequestMessage, CancellationToken, Task> modifyRequestMessageAsync,
            Func<HttpResponseMessage, bool> isSuccessful,
            Func<HttpResponseMessage, CancellationToken, Task> processResponseMessageAsync,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> mergedErrorMapping =
                MergeErrorMapping(errorMappingOverrides);

            using var msg = new HttpRequestMessage(httpMethod, requestUri);
            if (!_usingX509ClientCert)
            {
                string authHeader = await _authenticationHeaderProvider.GetPasswordAsync().ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(authHeader))
                {
                    msg.Headers.Add(HttpRequestHeader.Authorization.ToString(), authHeader);
                }
            }

            msg.Headers.UserAgent.ParseAdd(_productInfo.ToString(UserAgentFormats.Http));

            if (modifyRequestMessageAsync != null)
            {
                await modifyRequestMessageAsync(msg, cancellationToken).ConfigureAwait(false);
            }

            // TODO: pradeepc - find out the list of exceptions that HttpClient can throw.
            HttpResponseMessage responseMsg;
            try
            {
                responseMsg = await _httpClientObj.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                if (responseMsg == null)
                {
                    throw new InvalidOperationException(
                        $"The response message was null when executing operation {httpMethod}.");
                }

                if (isSuccessful(responseMsg))
                {
                    if (processResponseMessageAsync != null)
                    {
                        await processResponseMessageAsync(responseMsg, cancellationToken).ConfigureAwait(false);
                    }
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
                    throw new IotHubCommunicationException(ex.Message, ex);
                }

                throw new IotHubException(ex.Message, ex);
            }
            catch (TimeoutException ex)
            {
                throw new IotHubCommunicationException(ex.Message, ex);
            }
            catch (IOException ex)
            {
                throw new IotHubCommunicationException(ex.Message, ex);
            }
            catch (HttpRequestException ex)
            {
                throw new IotHubCommunicationException(ex.Message, ex);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                throw new IotHubException(ex.Message, ex);
            }

            if (!isSuccessful(responseMsg))
            {
                Exception mappedEx = await MapToExceptionAsync(responseMsg, mergedErrorMapping).ConfigureAwait(false);
                throw mappedEx;
            }
        }

        private static async Task<Exception> MapToExceptionAsync(
            HttpResponseMessage response,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMapping)
        {
            if (!errorMapping.TryGetValue(response.StatusCode, out Func<HttpResponseMessage, Task<Exception>> func))
            {
                return new IotHubException(
                    await ExceptionHandlingHelper.GetExceptionMessageAsync(response).ConfigureAwait(false),
                    isTransient: true);
            }

            Func<HttpResponseMessage, Task<Exception>> mapToExceptionFunc = errorMapping[response.StatusCode];
            Task<Exception> exception = mapToExceptionFunc(response);
            return await exception.ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_httpClientObj != null)
                {
                    _httpClientObj.Dispose();
                    _httpClientObj = null;
                }

                // HttpClientHandler that is used to create HttpClient will automatically be disposed when HttpClient is disposed
                // But in case the client handler didn't end up being used by the HttpClient, we explicitly dispose it here.
                if (_httpClientHandler != null)
                {
                    _httpClientHandler?.Dispose();
                    _httpClientHandler = null;
                }

                // The associated TokenRefresher should be disposed by the http client helper only when the http client
                // is the primary transport handler.
                // For eg. we create HttpTransportHandler instances for file upload operations even though the client might be
                // initialized via MQTT/ AMQP. In those scenarios, since the shared TokenRefresher resource would be primarily used by the
                // corresponding transport layers (MQTT/ AMQP), the diposal should be delegated to them and it should not be disposed here.
                // The only scenario where the TokenRefresher should be disposed here is when the client has been initialized using HTTP.
                if (_isClientPrimaryTransportHandler
                    && _authenticationHeaderProvider is IotHubConnectionString iotHubConnectionString
                    && iotHubConnectionString.TokenRefresher != null
                    && iotHubConnectionString.TokenRefresher.DisposalWithClient)
                {
                    iotHubConnectionString.TokenRefresher.Dispose();
                }

                _isDisposed = true;
            }
        }

        private static StringContent CreateContent<T>(T entity)
        {
            return new StringContent(JsonConvert.SerializeObject(entity), Encoding.UTF8, "application/json");
        }

        private static async Task<T> ReadAsAsync<T>(HttpContent content, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            using Stream stream = await content.ReadHttpContentAsStream(token).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            return new JsonSerializer().Deserialize<T>(jsonReader);
        }

        }
}
