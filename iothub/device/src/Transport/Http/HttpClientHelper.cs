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
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal sealed class HttpClientHelper : IDisposable
    {
        private readonly Uri _baseAddress;
        private readonly IConnectionCredentials _connectionCredentials;
        private readonly IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> _defaultErrorMapping;
        private readonly bool _usingX509ClientCert;
        private readonly HttpClient _httpClient;
        private readonly AdditionalClientInformation _additionalClientInformation;

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

        public HttpClientHelper(
            Uri baseAddress,
            IConnectionCredentials connectionCredentials,
            AdditionalClientInformation additionalClientInformation,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> defaultErrorMapping,
            TimeSpan timeout,
            IotHubClientHttpSettings iotHubClientHttpSettings)
        {
            _baseAddress = baseAddress;
            _connectionCredentials = connectionCredentials;
            _defaultErrorMapping = new ReadOnlyDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>(defaultErrorMapping);
            _additionalClientInformation = additionalClientInformation;

            if (iotHubClientHttpSettings.HttpClient != null)
            {
                _httpClient = iotHubClientHttpSettings.HttpClient;
                return;
            }

#pragma warning disable CA2000 // Dispose objects before losing scope
            // This HttpClientHandler is used by the locally saved HttpClient. When dispose() is called
            // on this object, it will dispose both the HttpClient and this handler
            var httpClientHandler = new HttpClientHandler
            {
                SslProtocols = iotHubClientHttpSettings.SslProtocols,
                CheckCertificateRevocationList = iotHubClientHttpSettings.CertificateRevocationCheck,
                ServerCertificateCustomValidationCallback = iotHubClientHttpSettings.ServerCertificateCustomValidationCallback,
            };
#pragma warning restore CA2000 // Dispose objects before losing scope

            X509Certificate2 clientCert = _connectionCredentials.ClientCertificate;
            if (clientCert != null)
            {
                httpClientHandler.ClientCertificates.Add(clientCert);
                _usingX509ClientCert = true;
            }

            IWebProxy proxy = iotHubClientHttpSettings.Proxy;
            if (proxy != null)
            {
                httpClientHandler.UseProxy = true;
                httpClientHandler.Proxy = proxy;
            }

            httpClientHandler.MaxConnectionsPerServer = DefaultMaxConnectionsPerServer;
            ServicePoint servicePoint = ServicePointManager.FindServicePoint(_baseAddress);
            servicePoint.ConnectionLeaseTimeout = s_defaultConnectionLeaseTimeout.Milliseconds;

            _httpClient = new HttpClient(httpClientHandler, true)
            {
                BaseAddress = _baseAddress,
                Timeout = timeout,
            };

            _httpClient.BaseAddress = _baseAddress;
            _httpClient.Timeout = timeout;
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(CommonConstants.MediaTypeForDeviceManagementApis));
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;
        }

        private static async Task<T> ReadResponseMessageAsync<T>(HttpResponseMessage message, CancellationToken token)
        {
            if (typeof(T) == typeof(HttpResponseMessage))
            {
                return (T)(object)message;
            }

            return await ReadAsAsync<T>(message.Content, token).ConfigureAwait(false);
        }

        private static void AddCustomHeaders(HttpRequestMessage requestMessage, IDictionary<string, string> customHeaders)
        {
            if (customHeaders != null)
            {
                foreach (KeyValuePair<string, string> header in customHeaders)
                {
                    requestMessage.Headers.Add(header.Key, header.Value);
                }
            }
        }

        public async Task<T2> PostAsync<T1, T2>(
             Uri requestUri,
             T1 entity,
             IDictionary<string, string> customHeaders,
             CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, requestUri, nameof(PostAsync));

            T2 result = default;

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var msg = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseAddress, requestUri));
                if (!_usingX509ClientCert)
                {
                    string authHeader = await _connectionCredentials.GetPasswordAsync().ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(authHeader))
                    {
                        msg.Headers.Add(HttpRequestHeader.Authorization.ToString(), authHeader);
                    }
                }

                if (_additionalClientInformation.ProductInfo != null)
                {
                    msg.Headers.UserAgent.ParseAdd(_additionalClientInformation.ProductInfo.ToString(UserAgentFormats.Http));
                }

                AddCustomHeaders(msg, customHeaders);
                if (entity != null)
                {
                    if (typeof(T1) == typeof(byte[]))
                    {
                        msg.Content = new ByteArrayContent((byte[])(object)entity);
                    }
                    else if (typeof(T1) == typeof(string))
                    {
                        // only used to send batched messages on Http runtime
                        msg.Content = new StringContent((string)(object)entity);
                        msg.Content.Headers.ContentType = new MediaTypeHeaderValue(CommonConstants.BatchedMessageContentType);
                    }
                    else
                    {
                        msg.Content = CreateContent(entity);
                    }
                }

                HttpResponseMessage responseMsg;
                try
                {
                    responseMsg = await _httpClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);
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

                    if (innerExceptions.Any(e => e is TimeoutException))
                    {
                        throw new IotHubClientException(ex.Message, IotHubClientErrorCode.NetworkErrors, ex);
                    }

                    throw new IotHubClientException(ex.Message, innerException: ex);
                }
                catch (Exception ex) when (ex is TimeoutException || ex is IOException || ex is HttpRequestException)
                {
                    throw new IotHubClientException(
                        ex.Message,
                        IotHubClientErrorCode.NetworkErrors,
                        ex);
                }
                catch (Exception ex) when (!Fx.IsFatal(ex) && ex is not OperationCanceledException)
                {
                    throw new IotHubClientException(ex.Message, innerException: ex);
                }

                if (!responseMsg.IsSuccessStatusCode)
                {
                    Exception mappedEx = await MapToExceptionAsync(responseMsg, _defaultErrorMapping).ConfigureAwait(false);
                    throw mappedEx;
                }

                return result;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, requestUri, nameof(PostAsync));
            }
        }

        private static async Task<Exception> MapToExceptionAsync(
            HttpResponseMessage response,
            IDictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>> errorMapping)
        {
            if (!errorMapping.TryGetValue(response.StatusCode, out Func<HttpResponseMessage, Task<Exception>> func))
            {
                return new IotHubClientException(await ClientExceptionHandlingHelper.GetExceptionMessageAsync(response).ConfigureAwait(false))
                {
                    IsTransient = true,
                };
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

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
