// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.HsmAuthentication.GeneratedCode;
using Microsoft.Azure.Devices.Client.TransientFaultHandling;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication
{
    internal class HttpHsmSignatureProvider : ISignatureProvider
    {
        private const SignRequestAlgo DefaultSignRequestAlgo = SignRequestAlgo.HMACSHA256;
        private const string DefaultKeyId = "primary";

        private readonly string _apiVersion;
        private readonly Uri _providerUri;

        private static readonly ITransientErrorDetectionStrategy s_transientErrorDetectionStrategy = new ErrorDetectionStrategy();

        private static readonly RetryStrategy s_transientRetryStrategy = new ExponentialBackoffRetryStrategy(
            retryCount: 3,
            minBackoff: TimeSpan.FromSeconds(2),
            maxBackoff: TimeSpan.FromSeconds(30),
            deltaBackoff: TimeSpan.FromSeconds(3));

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

            _providerUri = new Uri(providerUri);
            _apiVersion = apiVersion;
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

            var signRequest = new SignRequest
            {
                KeyId = DefaultKeyId,
                Algo = DefaultSignRequestAlgo,
                Data = Encoding.UTF8.GetBytes(data)
            };

            using HttpClient httpClient = HttpClientHelper.GetHttpClient(_providerUri);
            try
            {
                var hsmHttpClient = new HttpHsmClient(httpClient)
                {
                    BaseUrl = HttpClientHelper.GetBaseUrl(_providerUri)
                };

                SignResponse response = await SignAsyncWithRetryAsync(hsmHttpClient, moduleId, generationId, signRequest)
                    .ConfigureAwait(false);

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
        }

        private async Task<SignResponse> SignAsyncWithRetryAsync(
            HttpHsmClient hsmHttpClient,
            string moduleId,
            string generationId,
            SignRequest signRequest)
        {
            var transientRetryPolicy = new RetryPolicy(s_transientErrorDetectionStrategy, s_transientRetryStrategy);
            SignResponse response = await transientRetryPolicy
                .RunWithRetryAsync(() => hsmHttpClient.SignAsync(_apiVersion, moduleId, generationId, signRequest))
                .ConfigureAwait(false);
            return response;
        }

        private class ErrorDetectionStrategy : ITransientErrorDetectionStrategy
        {
            public bool IsTransient(Exception ex) => ex is SwaggerException se && se.StatusCode >= 500;
        }
    }
}
