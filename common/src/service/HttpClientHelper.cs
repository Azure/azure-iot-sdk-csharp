// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Common.Extensions;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    internal sealed class HttpClientHelper : IHttpClientHelper
    {
        private const string ApplicationJson = "application/json";
        private readonly Uri _baseAddress;
        private readonly IAuthorizationHeaderProvider _authenticationHeaderProvider;
        private readonly IReadOnlyDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> _defaultErrorMapping;
        private readonly TimeSpan _defaultOperationTimeout;

        // IDisposables

        private readonly HttpClient _httpClientWithDefaultTimeout;
        private readonly HttpClient _httpClientWithNoTimeout;

        public HttpClientHelper(
            Uri baseAddress,
            IAuthorizationHeaderProvider authenticationHeaderProvider,
            IReadOnlyDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> defaultErrorMapping,
            TimeSpan timeout,
            IWebProxy customHttpProxy,
            int connectionLeaseTimeoutMilliseconds)
        {
            _baseAddress = baseAddress;
            _authenticationHeaderProvider = authenticationHeaderProvider;
            _defaultErrorMapping = defaultErrorMapping;
            _defaultOperationTimeout = timeout;

            // We need two types of HttpClients, one with our default operation timeout, and one without. The one without will rely on
            // a cancellation token.

            _httpClientWithDefaultTimeout = CreateDefaultClient(customHttpProxy, _baseAddress, connectionLeaseTimeoutMilliseconds);
            _httpClientWithDefaultTimeout.Timeout = _defaultOperationTimeout;
            _httpClientWithDefaultTimeout.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(CommonConstants.MediaTypeForDeviceManagementApis));
            _httpClientWithDefaultTimeout.DefaultRequestHeaders.ExpectContinue = false;

            _httpClientWithNoTimeout = CreateDefaultClient(customHttpProxy, _baseAddress, connectionLeaseTimeoutMilliseconds);
            _httpClientWithNoTimeout.Timeout = Timeout.InfiniteTimeSpan;
            _httpClientWithNoTimeout.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(CommonConstants.MediaTypeForDeviceManagementApis));
            _httpClientWithNoTimeout.DefaultRequestHeaders.ExpectContinue = false;
        }

        public Task<T> GetAsync<T>(
            Uri requestUri,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            IDictionary<string, string> customHeaders,
            CancellationToken cancellationToken)
        {
            return GetAsync<T>(requestUri, _defaultOperationTimeout, errorMappingOverrides, customHeaders, true, cancellationToken);
        }

        public Task<T> GetAsync<T>(
            Uri requestUri,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            IDictionary<string, string> customHeaders,
            bool throwIfNotFound,
            CancellationToken cancellationToken)
        {
            return GetAsync<T>(requestUri, _defaultOperationTimeout, errorMappingOverrides, customHeaders, throwIfNotFound, cancellationToken);
        }

        public async Task<T> GetAsync<T>(
            Uri requestUri,
            TimeSpan operationTimeout,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            IDictionary<string, string> customHeaders,
            bool throwIfNotFound,
            CancellationToken cancellationToken)
        {
            T result = default;

            if (operationTimeout != _defaultOperationTimeout && operationTimeout > TimeSpan.Zero)
            {
                if (throwIfNotFound)
                {
                    await ExecuteWithCustomOperationTimeoutAsync(
                            HttpMethod.Get,
                            new Uri(_baseAddress, requestUri),
                            operationTimeout,
                            (requestMsg, token) => AddCustomHeaders(requestMsg, customHeaders),
                            IsMappedToException,
                            async (message, token) => result = await ReadResponseMessageAsync<T>(message, token).ConfigureAwait(false),
                            errorMappingOverrides,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await ExecuteWithCustomOperationTimeoutAsync(
                            HttpMethod.Get,
                            new Uri(_baseAddress, requestUri),
                            operationTimeout,
                            (requestMsg, token) => AddCustomHeaders(requestMsg, customHeaders),
                            message => !(message.IsSuccessStatusCode || message.StatusCode == HttpStatusCode.NotFound),
                            async (message, token) => result = message.StatusCode == HttpStatusCode.NotFound
                                ? default
                                : await ReadResponseMessageAsync<T>(message, token).ConfigureAwait(false),
                            errorMappingOverrides,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            else
            {
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
                        _httpClientWithDefaultTimeout,
                        HttpMethod.Get,
                        new Uri(_baseAddress, requestUri),
                        (requestMsg, token) => AddCustomHeaders(requestMsg, customHeaders),
                        message => !(message.IsSuccessStatusCode || message.StatusCode == HttpStatusCode.NotFound),
                        async (message, token) => result = message.StatusCode == HttpStatusCode.NotFound
                            ? default
                            : await ReadResponseMessageAsync<T>(message, token).ConfigureAwait(false),
                        errorMappingOverrides,
                        cancellationToken)
                    .ConfigureAwait(false);
                }
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
                        HttpMessageHelper.SetHttpRequestMessageContent(requestMsg, entity);

                        return Task.FromResult(0);
                    },
                    async (httpClient, token) => result = await ReadResponseMessageAsync<T>(httpClient, token).ConfigureAwait(false),
                    errorMappingOverrides,
                    cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        public async Task<T2> PutAsync<T, T2>(
            Uri requestUri,
            T entity,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            CancellationToken cancellationToken)
        {
            T2 result = default;

            await ExecuteAsync(
                    HttpMethod.Put,
                    new Uri(_baseAddress, requestUri),
                    (requestMsg, token) =>
                    {
                        HttpMessageHelper.SetHttpRequestMessageContent<T>(requestMsg, entity);
                        return Task.FromResult(0);
                    },
                    async (httpClient, token) => result = await ReadResponseMessageAsync<T2>(httpClient, token).ConfigureAwait(false),
                    errorMappingOverrides,
                    cancellationToken).ConfigureAwait(false);

            return result;
        }

        public async Task PutAsync<T>(
            Uri requestUri,
            T entity,
            string etag,
            PutOperationType operationType,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            CancellationToken cancellationToken)
        {
            await ExecuteAsync(
                HttpMethod.Put,
                new Uri(_baseAddress, requestUri),
                (requestMsg, token) =>
                {
                    InsertEtag(requestMsg, etag, operationType);
                    HttpMessageHelper.SetHttpRequestMessageContent<T>(requestMsg, entity);

                    return Task.FromResult(0);
                },
                null,
                errorMappingOverrides,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<T2> PutAsync<T, T2>(
            Uri requestUri,
            T entity,
            string etag,
            PutOperationType operationType,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            CancellationToken cancellationToken)
        {
            T2 result = default;

            await ExecuteAsync(
                HttpMethod.Put,
                new Uri(_baseAddress, requestUri),
                (requestMsg, token) =>
                {
                    // TODO: skintali: Use string etag when service side changes are ready
                    InsertEtag(requestMsg, etag, operationType);
                    HttpMessageHelper.SetHttpRequestMessageContent<T>(requestMsg, entity);

                    return Task.FromResult(0);
                },
                async (httpClient, token) => result = await ReadResponseMessageAsync<T2>(httpClient, token).ConfigureAwait(false),
                errorMappingOverrides,
                cancellationToken).ConfigureAwait(false);

            return result;
        }

        public async Task PatchAsync<T>(Uri requestUri, T entity, string etag,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides, CancellationToken cancellationToken)
        {
            await ExecuteAsync(
                new HttpMethod("PATCH"),
                new Uri(_baseAddress, requestUri),
                (requestMsg, token) =>
                {
                    InsertEtag(requestMsg, etag, PutOperationType.UpdateEntity);
                    HttpMessageHelper.SetHttpRequestMessageContent<T>(requestMsg, entity);

                    return Task.FromResult(0);
                },
                null,
                errorMappingOverrides,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<T2> PatchAsync<T, T2>(Uri requestUri, T entity, string etag,
            PutOperationType putOperationType,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            CancellationToken cancellationToken)
        {
            T2 result = default;

            await ExecuteAsync(
                new HttpMethod("PATCH"),
                new Uri(_baseAddress, requestUri),
                (requestMsg, token) =>
                {
                    InsertEtag(requestMsg, etag, putOperationType);
                    HttpMessageHelper.SetHttpRequestMessageContent<T>(requestMsg, entity);

                    return Task.FromResult(0);
                },
                async (httpClient, token) => result = await ReadResponseMessageAsync<T2>(httpClient, token).ConfigureAwait(false),
                errorMappingOverrides,
                cancellationToken).ConfigureAwait(false);

            return result;
        }

        private static async Task<T> ReadResponseMessageAsync<T>(HttpResponseMessage message, CancellationToken token)
        {
            if (typeof(T) == typeof(HttpResponseMessage))
            {
                return (T)(object)message;
            }

            string str = await message.Content.ReadHttpContentAsStringAsync(token).ConfigureAwait(false);
            T entity = JsonConvert.DeserializeObject<T>(str);

            // Etag in the header is considered authoritative
            var eTagHolder = entity as IETagHolder;
            if (eTagHolder != null)
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

            return Task.FromResult(0);
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
                InsertEtag(requestMessage, entity.ETag);
            }
        }

        private static void InsertEtag(HttpRequestMessage requestMessage, string etag, PutOperationType operationType)
        {
            if (operationType == PutOperationType.CreateEntity)
            {
                return;
            }

            string etagString = "\"*\"";
            if (operationType == PutOperationType.ForceUpdateEntity)
            {
                requestMessage.Headers.IfMatch.Add(new EntityTagHeaderValue(etagString));
            }
            else
            {
                InsertEtag(requestMessage, etag);
            }
        }

        private static void InsertEtag(HttpRequestMessage requestMessage, string etag)
        {
            if (string.IsNullOrWhiteSpace(etag))
            {
                throw new ArgumentException("The entity does not have its ETag set.");
            }

            if (!etag.StartsWith("\"", StringComparison.OrdinalIgnoreCase))
            {
                etag = "\"" + etag;
            }

            if (!etag.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
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
                foreach (KeyValuePair<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> @override in errorMappingOverrides)
                {
                    mergedMapping[@override.Key] = @override.Value;
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
            return PostAsyncHelper(
                requestUri,
                entity,
                TimeSpan.Zero,
                errorMappingOverrides,
                customHeaders,
                null,
                null,
                ReadResponseMessageAsync<HttpResponseMessage>,
                cancellationToken);
        }

        public Task PostAsync<T>(
            Uri requestUri,
            T entity,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            IDictionary<string, string> customHeaders,
            TimeSpan operationTimeout,
            CancellationToken cancellationToken)
        {
            return PostAsyncHelper(
                requestUri,
                entity,
                operationTimeout,
                errorMappingOverrides,
                customHeaders,
                null,
                null,
                ReadResponseMessageAsync<HttpResponseMessage>,
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
                    TimeSpan.Zero,
                    errorMappingOverrides,
                    customHeaders,
                    null,
                    null,
                    async (message, token) => result = await ReadResponseMessageAsync<T2>(message, token).ConfigureAwait(false),
                    cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        public async Task<T2> PostAsync<T, T2>(
            Uri requestUri,
            T entity,
            TimeSpan operationTimeout,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            IDictionary<string, string> customHeaders,
            CancellationToken cancellationToken)
        {
            T2 result = default;
            await PostAsyncHelper(
                    requestUri,
                    entity,
                    operationTimeout,
                    errorMappingOverrides,
                    customHeaders,
                    null,
                    null,
                    async (message, token) => result = await ReadResponseMessageAsync<T2>(message, token).ConfigureAwait(false),
                    cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        public async Task<T2> PostAsync<T, T2>(
            Uri requestUri,
            T entity,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            IDictionary<string, string> customHeaders,
            MediaTypeHeaderValue customContentType,
            ICollection<string> customContentEncoding,
            CancellationToken cancellationToken)
        {
            T2 result = default;
            await PostAsyncHelper(
                    requestUri,
                    entity,
                    TimeSpan.Zero,
                    errorMappingOverrides,
                    customHeaders,
                    customContentType,
                    customContentEncoding,
                    async (message, token) => result = await ReadResponseMessageAsync<T2>(message, token).ConfigureAwait(false),
                    cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        public async Task<HttpResponseMessage> PostAsync<T>(
            Uri requestUri,
            T entity, IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            IDictionary<string, string> customHeaders,
            MediaTypeHeaderValue customContentType,
            ICollection<string> customContentEncoding,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage result = default;
            await PostAsyncHelper(
                    requestUri,
                    entity,
                    TimeSpan.Zero,
                    errorMappingOverrides,
                    customHeaders,
                    customContentType,
                    customContentEncoding,
                    (message, token) => { result = message; return TaskHelpers.CompletedTask; },
                    cancellationToken)
                .ConfigureAwait(false);
            return result;
        }

        private Task PostAsyncHelper<T1>(
            Uri requestUri,
            T1 entity,
            TimeSpan operationTimeout,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            IDictionary<string, string> customHeaders,
            MediaTypeHeaderValue customContentType,
            ICollection<string> customContentEncoding,
            Func<HttpResponseMessage, CancellationToken, Task> processResponseMessageAsync,
            CancellationToken cancellationToken)
        {
            Func<HttpRequestMessage, CancellationToken, Task> modifyRequestMessageFunc = (requestMsg, token) =>
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
                        string str = JsonConvert.SerializeObject(entity);
                        requestMsg.Content = new StringContent(str, System.Text.Encoding.UTF8, ApplicationJson);
                    }
                }

                if (customContentType != null)
                {
                    requestMsg.Content.Headers.ContentType = customContentType;
                }

                if (customContentEncoding != null && customContentEncoding.Count > 0)
                {
                    foreach (string contentEncoding in customContentEncoding)
                    {
                        requestMsg.Content.Headers.ContentEncoding.Add(contentEncoding);
                    }
                }

                return Task.FromResult(0);
            };

            if (operationTimeout != _defaultOperationTimeout && operationTimeout > TimeSpan.Zero)
            {
                return ExecuteWithCustomOperationTimeoutAsync(
                    HttpMethod.Post,
                    new Uri(_baseAddress, requestUri),
                    operationTimeout,
                    modifyRequestMessageFunc,
                    IsMappedToException,
                    processResponseMessageAsync,
                    errorMappingOverrides,
                    cancellationToken);
            }
            else
            {
                return ExecuteAsync(
                    HttpMethod.Post,
                    new Uri(_baseAddress, requestUri),
                    modifyRequestMessageFunc,
                    processResponseMessageAsync,
                    errorMappingOverrides,
                    cancellationToken);
            }
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
                    InsertEtag(requestMsg, entity.ETag);
                    AddCustomHeaders(requestMsg, customHeaders);
                    return TaskHelpers.CompletedTask;
                },
                null,
                errorMappingOverrides,
                cancellationToken);
        }

        public async Task<T> DeleteAsync<T>(
            Uri requestUri,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            IDictionary<string, string> customHeaders,
            CancellationToken cancellationToken)
        {
            T result = default;

            await ExecuteAsync(
                    HttpMethod.Delete,
                    new Uri(_baseAddress, requestUri),
                    (requestMsg, token) =>
                    {
                        AddCustomHeaders(requestMsg, customHeaders);
                        return TaskHelpers.CompletedTask;
                    },
                    async (message, token) => result = await ReadResponseMessageAsync<T>(message, token).ConfigureAwait(false),
                    errorMappingOverrides,
                    cancellationToken)
                .ConfigureAwait(false);

            return result;
        }

        private async Task ExecuteAsync(
            HttpMethod httpMethod,
            Uri requestUri,
            Func<HttpRequestMessage, CancellationToken, Task> modifyRequestMessageAsync,
            Func<HttpResponseMessage, CancellationToken, Task> processResponseMessageAsync,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            CancellationToken cancellationToken)
        {
            await ExecuteAsync(
                    _httpClientWithDefaultTimeout,
                    httpMethod,
                    requestUri,
                    modifyRequestMessageAsync,
                    IsMappedToException,
                    processResponseMessageAsync,
                    errorMappingOverrides,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task ExecuteWithCustomOperationTimeoutAsync(
            HttpMethod httpMethod,
            Uri requestUri,
            TimeSpan operationTimeout,
            Func<HttpRequestMessage, CancellationToken, Task> modifyRequestMessageAsync,
            Func<HttpResponseMessage, bool> isMappedToException,
            Func<HttpResponseMessage, CancellationToken, Task> processResponseMessageAsync,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var cts = new CancellationTokenSource(operationTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            await ExecuteAsync(
                    _httpClientWithNoTimeout,
                    httpMethod,
                    requestUri,
                    modifyRequestMessageAsync,
                    isMappedToException,
                    processResponseMessageAsync,
                    errorMappingOverrides,
                    linkedCts.Token)
                .ConfigureAwait(false);
        }

        public static bool IsMappedToException(HttpResponseMessage message)
        {
            bool isMappedToException = !message.IsSuccessStatusCode;

            // Get any IotHubErrorCode information from the header for special case exemption of exception throwing
            string iotHubErrorCodeAsString = message.Headers.GetFirstValueOrNull(CommonConstants.IotHubErrorCode);
            if (Enum.TryParse(iotHubErrorCodeAsString, out ErrorCode iotHubErrorCode))
            {
                switch (iotHubErrorCode)
                {
                    case ErrorCode.BulkRegistryOperationFailure:
                        isMappedToException = false;
                        break;
                }
            }

            return isMappedToException;
        }

        public void Dispose()
        {
            _httpClientWithDefaultTimeout.Dispose();
            _httpClientWithNoTimeout.Dispose();
        }

        private async Task ExecuteAsync(
            HttpClient httpClient,
            HttpMethod httpMethod,
            Uri requestUri,
            Func<HttpRequestMessage, CancellationToken, Task> modifyRequestMessageAsync,
            Func<HttpResponseMessage, bool> isMappedToException,
            Func<HttpResponseMessage, CancellationToken, Task> processResponseMessageAsync,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            CancellationToken cancellationToken)
        {
            Logging.Enter(this, httpMethod.Method, requestUri, nameof(ExecuteAsync));

            try
            {
                IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> mergedErrorMapping = MergeErrorMapping(errorMappingOverrides);

                using var msg = new HttpRequestMessage(httpMethod, requestUri);
                msg.Headers.Add(HttpRequestHeader.Authorization.ToString(), _authenticationHeaderProvider.GetAuthorizationHeader());
                msg.Headers.Add(HttpRequestHeader.UserAgent.ToString(), Utils.GetClientVersion());

                if (modifyRequestMessageAsync != null)
                {
                    await modifyRequestMessageAsync(msg, cancellationToken).ConfigureAwait(false);
                }

                // TODO: pradeepc - find out the list of exceptions that HttpClient can throw.
                HttpResponseMessage responseMsg;
                try
                {
                    responseMsg = await httpClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                    if (responseMsg == null)
                    {
                        throw new InvalidOperationException("The response message was null when executing operation {0}.".FormatInvariant(httpMethod));
                    }

                    if (!isMappedToException(responseMsg))
                    {
                        if (processResponseMessageAsync != null)
                        {
                            await processResponseMessageAsync(responseMsg, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                catch (AggregateException ex)
                {
                    Logging.Error(this, ex, nameof(ExecuteAsync));

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
                    Logging.Error(this, ex, nameof(ExecuteAsync));

                    throw new IotHubCommunicationException(ex.Message, ex);
                }
                catch (IOException ex)
                {
                    Logging.Error(this, ex, nameof(ExecuteAsync));

                    throw new IotHubCommunicationException(ex.Message, ex);
                }
                catch (HttpRequestException ex)
                {
                    Logging.Error(this, ex, nameof(ExecuteAsync));

                    throw new IotHubCommunicationException(ex.Message, ex);
                }
                catch (TaskCanceledException ex)
                {
                    Logging.Error(this, ex, nameof(ExecuteAsync));

                    // Unfortunately TaskCanceledException is thrown when HttpClient times out.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new IotHubException(ex.Message, ex);
                    }

                    throw new IotHubCommunicationException(string.Format(CultureInfo.InvariantCulture, "The {0} operation timed out.", httpMethod), ex);
                }
                catch (Exception ex) when (!Fx.IsFatal(ex))
                {
                    Logging.Error(this, ex, nameof(ExecuteAsync));

                    throw new IotHubException(ex.Message, ex);
                }

                if (isMappedToException(responseMsg))
                {
                    Exception mappedEx = await MapToExceptionAsync(responseMsg, mergedErrorMapping).ConfigureAwait(false);
                    throw mappedEx;
                }
            }
            finally
            {
                Logging.Exit(this, httpMethod.Method, requestUri, nameof(ExecuteAsync));
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

        internal static HttpClient CreateDefaultClient(IWebProxy webProxy, Uri baseUri, int connectionLeaseTimeoutMilliseconds)
        {
            // This http client will dispose of this httpMessageHandler when the http client is disposed, so no need to save a local copy
#pragma warning disable CA2000 // Dispose objects before losing scope (httpClient and messageHandler are disposed outside of this scope)
            return new HttpClient(CreateDefaultHttpMessageHandler(webProxy, baseUri, connectionLeaseTimeoutMilliseconds))
#pragma warning restore CA2000 // Dispose objects before losing scope
            {
                // Timeouts are handled by the pipeline
                Timeout = Timeout.InfiniteTimeSpan,
                BaseAddress = baseUri
            };
        }

        internal static HttpMessageHandler CreateDefaultHttpMessageHandler(IWebProxy webProxy, Uri baseUri, int connectionLeaseTimeoutMilliseconds)
        {
#if NETCOREAPP && !NETCOREAPP2_0 && !NETCOREAPP1_0 && !NETCOREAPP1_1
            // SocketsHttpHandler is only available in netcoreapp2.1 and onwards
            var httpMessageHandler = new SocketsHttpHandler();
            httpMessageHandler.SslOptions.EnabledSslProtocols = TlsVersions.Instance.Preferred;
#else
            var httpMessageHandler = new HttpClientHandler();
            httpMessageHandler.SslProtocols = TlsVersions.Instance.Preferred;
            httpMessageHandler.CheckCertificateRevocationList = TlsVersions.Instance.CertificateRevocationCheck;
#endif

            if (webProxy != DefaultWebProxySettings.Instance)
            {
                httpMessageHandler.UseProxy = webProxy != null;
                httpMessageHandler.Proxy = webProxy;
            }

            ServicePointHelpers.SetLimits(httpMessageHandler, baseUri, connectionLeaseTimeoutMilliseconds);

            return httpMessageHandler;
        }
    }
}
