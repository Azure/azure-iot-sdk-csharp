// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.HsmAuthentication;
using Microsoft.Azure.Devices.Client.HsmAuthentication.GeneratedCode;
using Microsoft.Azure.Devices.Client.TransientFaultHandling;

namespace Microsoft.Azure.Devices.Client.Edge
{
    internal class TrustBundleProvider : ITrustBundleProvider
    {
        static readonly ITransientErrorDetectionStrategy TransientErrorDetectionStrategy = new ErrorDetectionStrategy();
        static readonly RetryStrategy TransientRetryStrategy =
            new TransientFaultHandling.ExponentialBackoff(retryCount: 3, minBackoff: TimeSpan.FromSeconds(2), maxBackoff: TimeSpan.FromSeconds(30), deltaBackoff: TimeSpan.FromSeconds(3));

        public async Task<IList<X509Certificate2>> GetTrustBundleAsync(Uri providerUri, string apiVersion)
        {
            HttpClient httpClient = null;
            try
            {
                httpClient = HttpClientHelper.GetHttpClient(providerUri);
                var hsmHttpClient = new HttpHsmClient(httpClient)
                {
                    BaseUrl = HttpClientHelper.GetBaseUrl(providerUri)
                };
                TrustBundleResponse response = await GetTrustBundleWithRetry(hsmHttpClient, apiVersion).ConfigureAwait(false);

                var certs = ParseCertificates(response.Certificate);
                return certs;
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case SwaggerException<ErrorResponse> errorResponseException:
                        throw new HttpHsmComunicationException($"Error calling GetTrustBundleWithRetry: {errorResponseException.Result?.Message ?? string.Empty}", errorResponseException.StatusCode);
                    case SwaggerException swaggerException:
                        throw new HttpHsmComunicationException($"Error calling GetTrustBundleWithRetry: {swaggerException.Response ?? string.Empty}", swaggerException.StatusCode);
                    default:
                        throw;
                }
            }
            finally
            {
                httpClient?.Dispose();
            }
        }

        private async Task<TrustBundleResponse> GetTrustBundleWithRetry(HttpHsmClient hsmHttpClient, string apiVersion)
        {
            var transientRetryPolicy = new RetryPolicy(TransientErrorDetectionStrategy, TransientRetryStrategy);
            return await transientRetryPolicy.ExecuteAsync(() => hsmHttpClient.TrustBundleAsync(apiVersion)).ConfigureAwait(false);
        }

        internal IList<X509Certificate2> ParseCertificates(string pemCerts)
        {
            if (string.IsNullOrEmpty(pemCerts))
            {
                throw new InvalidOperationException("Trusted certificates can not be null or empty.");
            }

            // Extract each certificate's string. The final string from the split will either be empty
            // or a non-certificate entry, so it is dropped.
            string delimiter = "-----END CERTIFICATE-----";
            string[] rawCerts = pemCerts.Split(new[] { delimiter }, StringSplitOptions.None);

            return rawCerts
               .Take(rawCerts.Count() - 1) // Drop the invalid entry
               .Select(c => $"{c}{delimiter}") // Re-add the certificate end-marker which was removed by split
               .Select(c => System.Text.Encoding.UTF8.GetBytes(c))
               .Select(c => new X509Certificate2(c))
               .ToList();
        }

        class ErrorDetectionStrategy : ITransientErrorDetectionStrategy
        {
            public bool IsTransient(Exception ex) => ex is SwaggerException se && se.StatusCode >= 500;
        }

    }

}
