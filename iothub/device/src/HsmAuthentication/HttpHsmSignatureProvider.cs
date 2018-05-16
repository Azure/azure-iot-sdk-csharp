// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.TransientFaultHandling;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication
{
    class HttpHsmSignatureProvider : ISignatureProvider
    {
        const SignRequestAlgo DefaultSignRequestAlgo = SignRequestAlgo.HMACSHA256;
        const string DefaultKeyId = "primary";
        readonly string apiVersion;
        readonly HsmHttpClient httpClient;

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

            this.httpClient = new HsmHttpClient()
            {
                BaseUrl = providerUri
            };
            this.apiVersion = apiVersion;
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

            try
            {
                SignResponse response = await this.SignAsyncWithRetry(moduleId, generationId, signRequest);

                return Convert.ToBase64String(response.Digest);
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case SwaggerException<ErrorResponse> errorResponseException:
                        throw new HttpHsmComunicationException($"Error calling SignAsync: {errorResponseException.Result?.Message ?? string.Empty}", errorResponseException.StatusCode);
                    case SwaggerException swaggerException:
                        throw new HttpHsmComunicationException($"Error calling SignAsync: {swaggerException.Response ?? string.Empty}", swaggerException.StatusCode);
                    default:
                        throw;
                }
            }
        }

        async Task<SignResponse> SignAsyncWithRetry(string moduleId, string generationId, SignRequest signRequest)
        {
            var transientRetryPolicy = new RetryPolicy(TransientErrorDetectionStrategy, TransientRetryStrategy);
            SignResponse response = await transientRetryPolicy.ExecuteAsync(() => this.httpClient.SignAsync(this.apiVersion, moduleId, generationId, signRequest));
            return response;
        }

        class ErrorDetectionStrategy : ITransientErrorDetectionStrategy
        {
            public bool IsTransient(Exception ex) => ex is SwaggerException se && se.StatusCode >= 500;
        }
    }
}
