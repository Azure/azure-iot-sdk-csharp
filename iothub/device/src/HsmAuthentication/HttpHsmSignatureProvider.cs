// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
#if NETSTANDARD2_0
using Microsoft.Azure.Devices.Client.HsmAuthentication.Transport;
#endif
using Microsoft.Azure.Devices.Client.TransientFaultHandling;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication
{
    class HttpHsmSignatureProvider : ISignatureProvider
    {
        private const string DefaultApiVersion = "2018-06-28";
        private const string HttpScheme = "http";
        private const string HttpsScheme = "https";
        private const string UnixScheme = "unix";
        private const SignRequestAlgo DefaultSignRequestAlgo = SignRequestAlgo.HMACSHA256;
        private const string DefaultKeyId = "primary";
        private readonly string _apiVersion;
        private readonly Uri _providerUri;

        static readonly ITransientErrorDetectionStrategy TransientErrorDetectionStrategy = new ErrorDetectionStrategy();
        static readonly RetryStrategy TransientRetryStrategy =
            new TransientFaultHandling.ExponentialBackoff(retryCount: 3, minBackoff: TimeSpan.FromSeconds(2), maxBackoff: TimeSpan.FromSeconds(30), deltaBackoff: TimeSpan.FromSeconds(3));

        public HttpHsmSignatureProvider(string providerUri, string apiVersion)
        {
            if (string.IsNullOrEmpty(providerUri))
            {
                throw new ArgumentNullException(nameof(providerUri));
            }
            if (string.IsNullOrEmpty(apiVersion))
            {
                throw new ArgumentNullException(nameof(apiVersion));
            }

            this._providerUri = new Uri(providerUri);
            this._apiVersion = apiVersion;
        }

        public async Task<string> SignAsync(string moduleId, string generationId, string data)
        {
            if (string.IsNullOrEmpty(moduleId))
            {
                throw new ArgumentNullException(nameof(moduleId));
            }
            if (string.IsNullOrEmpty(generationId))
            {
                throw new ArgumentNullException(nameof(generationId));
            }

            var signRequest = new SignRequest()
            {
                KeyId = DefaultKeyId,
                Algo = DefaultSignRequestAlgo,
                Data = Encoding.UTF8.GetBytes(data)
            };

            HttpClient httpClient = GetHttpClient();
            try
            {
                var hsmHttpClient = new HsmHttpClient(httpClient)
                {
                    BaseUrl = GetBaseUrl()
                };

                SignResponse response = await this.SignAsyncWithRetry(hsmHttpClient, moduleId, generationId, signRequest);

                return Convert.ToBase64String(response.Digest);
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case SwaggerException<ErrorResponse> errorResponseException:
                        throw new HttpHsmComunicationException(
                            $"Error calling SignAsync: {errorResponseException.Result?.Message ?? string.Empty}",
                            errorResponseException.StatusCode);
                    case SwaggerException swaggerException:
                        throw new HttpHsmComunicationException(
                            $"Error calling SignAsync: {swaggerException.Response ?? string.Empty}",
                            swaggerException.StatusCode);
                    default:
                        throw;
                }
            }
            finally
            {
                httpClient.Dispose();
            }
        }

        private HttpClient GetHttpClient()
        {
            HttpClient client;

            if (_providerUri.Scheme.Equals(HttpScheme, StringComparison.OrdinalIgnoreCase) || _providerUri.Scheme.Equals(HttpsScheme, StringComparison.OrdinalIgnoreCase))
            {
                client = new HttpClient();
                return client;
            }

#if NETSTANDARD2_0
            if (_providerUri.Scheme.Equals(UnixScheme, StringComparison.OrdinalIgnoreCase))
            {
                client = new HttpClient(new HttpUdsMessageHandler(_providerUri));
                return client;
            }
#endif

            throw new InvalidOperationException("ProviderUri scheme is not supported");
        }

        private string GetBaseUrl()
        {

#if NETSTANDARD2_0
            if (_providerUri.Scheme.Equals(UnixScheme, StringComparison.OrdinalIgnoreCase))
            {
                return $"{HttpScheme}://{_providerUri.Segments.Last()}";
            }
#endif

            return _providerUri.OriginalString;
        }

        private async Task<SignResponse> SignAsyncWithRetry(HsmHttpClient hsmHttpClient, string moduleId, string generationId, SignRequest signRequest)
        {
            var transientRetryPolicy = new RetryPolicy(TransientErrorDetectionStrategy, TransientRetryStrategy);
            SignResponse response = await transientRetryPolicy.ExecuteAsync(() => hsmHttpClient.SignAsync(_apiVersion, moduleId, generationId, signRequest));
            return response;
        }

        class ErrorDetectionStrategy : ITransientErrorDetectionStrategy
        {
            public bool IsTransient(Exception ex) => ex is SwaggerException se && se.StatusCode >= 500;
        }
    }
}
