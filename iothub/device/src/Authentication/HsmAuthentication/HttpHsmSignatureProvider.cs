// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.HsmAuthentication.GeneratedCode;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication
{
    internal sealed class HttpHsmSignatureProvider : ISignatureProvider
    {
        private const SignRequestAlgo DefaultSignRequestAlgo = SignRequestAlgo.HMACSHA256;
        private const string DefaultKeyId = "primary";

        private readonly string _apiVersion;
        private readonly Uri _providerUri;

        private static readonly IIotHubClientRetryPolicy s_retryPolicy = new IotHubClientExponentialBackoffRetryPolicy(3, TimeSpan.FromSeconds(30));

        public HttpHsmSignatureProvider(string providerUri, string apiVersion)
        {
            Debug.Assert(!string.IsNullOrEmpty(providerUri), $"{nameof(providerUri)} cannot be null. Validate parameter upstream.");
            Debug.Assert(!string.IsNullOrEmpty(apiVersion), $"{nameof(apiVersion)} cannot be null. Validate parameter upstream.");

            _providerUri = new Uri(providerUri);
            _apiVersion = apiVersion;
        }

        public async Task<string> SignAsync(string moduleId, string generationId, string data)
        {
            Debug.Assert(!string.IsNullOrEmpty(moduleId), $"{nameof(moduleId)} cannot be null. Validate parameter upstream.");
            Debug.Assert(!string.IsNullOrEmpty(generationId), $"{nameof(generationId)} cannot be null. Validate parameter upstream.");

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

                SignResponse response = await SignWithRetryAsync(
                        hsmHttpClient,
                        moduleId,
                        generationId,
                        signRequest)
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

        private async Task<SignResponse> SignWithRetryAsync(
            HttpHsmClient hsmHttpClient,
            string moduleId,
            string generationId,
            SignRequest signRequest)
        {
            var retryHandler = new RetryHandler(s_retryPolicy);
            return await retryHandler
                .RunWithRetryAsync(
                    () => hsmHttpClient.SignAsync(_apiVersion, moduleId, generationId, signRequest),
                    (Exception ex) => ex is SwaggerException se && se.StatusCode >= 500)
                .ConfigureAwait(false);
        }
    }
}
