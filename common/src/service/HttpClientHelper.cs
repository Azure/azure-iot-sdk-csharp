﻿// Copyright (c) Microsoft. All rights reserved.
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
using Microsoft.Azure.Devices.Shared;

#if NET451
using System.Net.Http.Formatting;
#endif

namespace Microsoft.Azure.Devices
{
    internal sealed class HttpClientHelper : IHttpClientHelper
    {
#if NET451
        static readonly JsonMediaTypeFormatter JsonFormatter = new JsonMediaTypeFormatter();
#endif
        private readonly Uri _baseAddress;
        private readonly IAuthorizationHeaderProvider _authenticationHeaderProvider;
        private readonly IReadOnlyDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> _defaultErrorMapping;
        private readonly TimeSpan _defaultOperationTimeout;
        private readonly IWebProxy _customHttpProxy;
        private readonly Action<HttpClient> _preRequestActionForAllRequests;

        public HttpClientHelper(
            Uri baseAddress,
            IAuthorizationHeaderProvider authenticationHeaderProvider,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> defaultErrorMapping,
            TimeSpan timeout,
            Action<HttpClient> preRequestActionForAllRequests,
            IWebProxy customHttpProxy)
        {
            _baseAddress = baseAddress;
            _authenticationHeaderProvider = authenticationHeaderProvider;
            _defaultErrorMapping = new ReadOnlyDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>(defaultErrorMapping);
            _defaultOperationTimeout = timeout;
            _preRequestActionForAllRequests = preRequestActionForAllRequests;
            _customHttpProxy = customHttpProxy;
            TlsVersions.Instance.SetLegacyAcceptableVersions();
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
            T result = default(T);

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
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await ExecuteWithCustomOperationTimeoutAsync(
                       HttpMethod.Get,
                       new Uri(_baseAddress, requestUri),
                        operationTimeout,
                       (requestMsg, token) => AddCustomHeaders(requestMsg, customHeaders),
                       message => !(message.IsSuccessStatusCode || message.StatusCode == HttpStatusCode.NotFound),
                       async (message, token) => result = message.StatusCode == HttpStatusCode.NotFound ? (default(T)) : await ReadResponseMessageAsync<T>(message, token).ConfigureAwait(false),
                       errorMappingOverrides,
                       cancellationToken).ConfigureAwait(false);
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
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    using (var httpClient = BuildHttpClient(_defaultOperationTimeout))
                    {
                        await ExecuteAsync(
                           httpClient,
                           HttpMethod.Get,
                           new Uri(_baseAddress, requestUri),
                           (requestMsg, token) => AddCustomHeaders(requestMsg, customHeaders),
                           message => !(message.IsSuccessStatusCode || message.StatusCode == HttpStatusCode.NotFound),
                           async (message, token) => result = message.StatusCode == HttpStatusCode.NotFound ? (default(T)) : await ReadResponseMessageAsync<T>(message, token).ConfigureAwait(false),
                           errorMappingOverrides,
                           cancellationToken).ConfigureAwait(false);
                    }
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
            T result = default(T);

            await ExecuteAsync(
                    HttpMethod.Put,
                    new Uri(_baseAddress, requestUri),
                    (requestMsg, token) =>
                    {
                        InsertEtag(requestMsg, entity, operationType);
#if NET451
                        requestMsg.Content = new ObjectContent<T>(entity, JsonFormatter);
#else
                        var str = Newtonsoft.Json.JsonConvert.SerializeObject(entity);
                        requestMsg.Content = new StringContent(str, System.Text.Encoding.UTF8, "application/json");
#endif
                        return Task.FromResult(0);
                    },
                    async (httpClient, token) => result = await ReadResponseMessageAsync<T>(httpClient, token).ConfigureAwait(false),
                    errorMappingOverrides,
                    cancellationToken).ConfigureAwait(false);

            return result;
        }

        public async Task<T2> PutAsync<T, T2>(
            Uri requestUri,
            T entity,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides,
            CancellationToken cancellationToken)
        {
            T2 result = default(T2);

            await ExecuteAsync(
                    HttpMethod.Put,
                    new Uri(_baseAddress, requestUri),
                    (requestMsg, token) =>
                    {
#if NET451
                        requestMsg.Content = new ObjectContent<T>(entity, JsonFormatter);
#else
                        var str = Newtonsoft.Json.JsonConvert.SerializeObject(entity);
                        requestMsg.Content = new StringContent(str, System.Text.Encoding.UTF8, "application/json");
#endif
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
#if NET451
                    requestMsg.Content = new ObjectContent<T>(entity, JsonFormatter);
#else
                    var str = Newtonsoft.Json.JsonConvert.SerializeObject(entity);
                    requestMsg.Content = new StringContent(str, System.Text.Encoding.UTF8, "application/json");
#endif
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
            T2 result = default(T2);

            await ExecuteAsync(
                HttpMethod.Put,
                new Uri(_baseAddress, requestUri),
                (requestMsg, token) =>
                {
                    // TODO: skintali: Use string etag when service side changes are ready
                    InsertEtag(requestMsg, etag, operationType);
#if NET451
                    requestMsg.Content = new ObjectContent<T>(entity, JsonFormatter);
#else
                    var str = Newtonsoft.Json.JsonConvert.SerializeObject(entity);
                    requestMsg.Content = new StringContent(str, System.Text.Encoding.UTF8, "application/json");
#endif
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
#if NET451
                    requestMsg.Content = new ObjectContent<T>(entity, JsonFormatter);
#else
                    var str = Newtonsoft.Json.JsonConvert.SerializeObject(entity);
                    requestMsg.Content = new StringContent(str, System.Text.Encoding.UTF8, "application/json");
#endif
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
            T2 result = default(T2);

            await ExecuteAsync(
                new HttpMethod("PATCH"),
                new Uri(_baseAddress, requestUri),
                (requestMsg, token) =>
                {
                    InsertEtag(requestMsg, etag, putOperationType);
#if NET451
                    requestMsg.Content = new ObjectContent<T>(entity, JsonFormatter);
#else
                    var str = Newtonsoft.Json.JsonConvert.SerializeObject(entity);
                    requestMsg.Content = new StringContent(str, System.Text.Encoding.UTF8, "application/json");
#endif
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

#if NET451
            T entity = await message.Content.ReadAsAsync<T>(token).ConfigureAwait(false);
#else
            var str = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
            T entity = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(str);
#endif
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
                foreach (var header in customHeaders)
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
                etag = etag + "\"";
            }

            requestMessage.Headers.IfMatch.Add(new EntityTagHeaderValue(etag));
        }

        private IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> MergeErrorMapping(
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMappingOverrides)
        {
            var mergedMapping = _defaultErrorMapping.ToDictionary(mapping => mapping.Key, mapping => mapping.Value);

            if (errorMappingOverrides != null)
            {
                foreach (var @override in errorMappingOverrides)
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
            T2 result = default(T2);
            await PostAsyncHelper(
                requestUri,
                entity,
                TimeSpan.Zero,
                errorMappingOverrides,
                customHeaders,
                null,
                null,
                async (message, token) => result = await ReadResponseMessageAsync<T2>(message, token).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);

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
            T2 result = default(T2);
            await PostAsyncHelper(
                requestUri,
                entity,
                operationTimeout,
                errorMappingOverrides,
                customHeaders,
                null,
                null,
                async (message, token) => result = await ReadResponseMessageAsync<T2>(message, token).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);

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
            T2 result = default(T2);
            await PostAsyncHelper(
                requestUri,
                entity,
                TimeSpan.Zero,
                errorMappingOverrides,
                customHeaders,
                customContentType,
                customContentEncoding,
                async (message, token) => result = await ReadResponseMessageAsync<T2>(message, token).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);

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
            HttpResponseMessage result = default(HttpResponseMessage);
            await PostAsyncHelper(
                requestUri,
                entity,
                TimeSpan.Zero,
                errorMappingOverrides,
                customHeaders,
                customContentType,
                customContentEncoding,
                (message, token) => { result = message; return TaskHelpers.CompletedTask; },
                cancellationToken).ConfigureAwait(false);
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
                        var str = Newtonsoft.Json.JsonConvert.SerializeObject(entity);
                        requestMsg.Content = new StringContent(str, System.Text.Encoding.UTF8, "application/json");
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
            T result = default(T);

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
                    cancellationToken).ConfigureAwait(false);

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
            using (var httpClient = BuildHttpClient(_defaultOperationTimeout))
            {
                await ExecuteAsync(
                    httpClient,
                    httpMethod,
                    requestUri,
                    modifyRequestMessageAsync,
                    IsMappedToException,
                    processResponseMessageAsync,
                    errorMappingOverrides,
                    cancellationToken).ConfigureAwait(false);
            }
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

            using (var httpClient = BuildHttpClient(Timeout.InfiniteTimeSpan))
            {
                var cts = new CancellationTokenSource(operationTimeout);
                CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
                await ExecuteAsync(
                    httpClient,
                    httpMethod,
                    requestUri,
                    modifyRequestMessageAsync,
                    isMappedToException,
                    processResponseMessageAsync,
                    errorMappingOverrides,
                    linkedCts.Token).ConfigureAwait(false);
            }
        }

        public static bool IsMappedToException(HttpResponseMessage message)
        {
            bool isMappedToException = !message.IsSuccessStatusCode;

            // Get any IotHubErrorCode information from the header for special case exemption of exception throwing
            string iotHubErrorCodeAsString = message.Headers.GetFirstValueOrNull(CommonConstants.IotHubErrorCode);
            ErrorCode iotHubErrorCode;
            if (Enum.TryParse(iotHubErrorCodeAsString, out iotHubErrorCode))
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
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> mergedErrorMapping =
                MergeErrorMapping(errorMappingOverrides);

            using (var msg = new HttpRequestMessage(httpMethod, requestUri))
            {
                msg.Headers.Add(HttpRequestHeader.Authorization.ToString(), _authenticationHeaderProvider.GetAuthorizationHeader());
                msg.Headers.Add(HttpRequestHeader.UserAgent.ToString(), Utils.GetClientVersion());

                if (modifyRequestMessageAsync != null) await modifyRequestMessageAsync(msg, cancellationToken).ConfigureAwait(false);

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
                    var innerExceptions = ex.Flatten().InnerExceptions;
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
                catch (TaskCanceledException ex)
                {
                    // Unfortunately TaskCanceledException is thrown when HttpClient times out.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new IotHubException(ex.Message, ex);
                    }

                    throw new IotHubCommunicationException(string.Format(CultureInfo.InvariantCulture, "The {0} operation timed out.", httpMethod), ex);
                }
                catch (Exception ex)
                {
                    if (Fx.IsFatal(ex)) throw;

                    throw new IotHubException(ex.Message, ex);
                }

                if (isMappedToException(responseMsg))
                {
                    Exception mappedEx = await MapToExceptionAsync(responseMsg, mergedErrorMapping).ConfigureAwait(false);
                    throw mappedEx;
                }
            }
        }

        private static async Task<Exception> MapToExceptionAsync(
            HttpResponseMessage response,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMapping)
        {
            Func<HttpResponseMessage, Task<Exception>> func;
            if (!errorMapping.TryGetValue(response.StatusCode, out func))
            {
                return new IotHubException(
                    await ExceptionHandlingHelper.GetExceptionMessageAsync(response).ConfigureAwait(false),
                    isTransient: true);
            }

            var mapToExceptionFunc = errorMapping[response.StatusCode];
            var exception = mapToExceptionFunc(response);
            return await exception.ConfigureAwait(false);
        }

        private HttpClient BuildHttpClient(TimeSpan timeout)
        {
            var httpClientHandler = new HttpClientHandler
            {
#if !NET451
                SslProtocols = TlsVersions.Instance.Preferred,
#endif
            };

            if (_customHttpProxy != DefaultWebProxySettings.Instance)
            {
                httpClientHandler.UseProxy = (_customHttpProxy != null);
                httpClientHandler.Proxy = _customHttpProxy;
            }

            var httpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = _baseAddress,
                Timeout = timeout
            };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(CommonConstants.MediaTypeForDeviceManagementApis));
            httpClient.DefaultRequestHeaders.ExpectContinue = false;

            _preRequestActionForAllRequests?.Invoke(httpClient);

            return httpClient;
        }

        public void Dispose()
        {
        }
    }
}
