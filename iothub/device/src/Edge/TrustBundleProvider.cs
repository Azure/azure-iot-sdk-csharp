// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.HsmAuthentication;
using Microsoft.Azure.Devices.Client.HsmAuthentication.GeneratedCode;
using Microsoft.Azure.Devices.Client.TransientFaultHandling;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;

namespace Microsoft.Azure.Devices.Client.Edge
{
    internal class TrustBundleProvider : ITrustBundleProvider
    {
        static readonly ITransientErrorDetectionStrategy TransientErrorDetectionStrategy = new ErrorDetectionStrategy();
        static readonly RetryStrategy TransientRetryStrategy =
            new TransientFaultHandling.ExponentialBackoff(retryCount: 3, minBackoff: TimeSpan.FromSeconds(2), maxBackoff: TimeSpan.FromSeconds(30), deltaBackoff: TimeSpan.FromSeconds(3));

        public async Task SetupTrustBundle(Uri providerUri, string apiVersion, ITransportSettings[] transportSettings)
        {
            TrustBundleResponse trustBundleResponse = await this.GetTrustBundleAsync(providerUri, apiVersion).ConfigureAwait(false);
            var certs = ParseCertificates(trustBundleResponse.Certificate);

            Debug.WriteLine("TrustBundleProvider.SetupTrustBundle from service provider");
            SetupCerts(transportSettings, certs);
        }

        public void SetupTrustBundle(string filePath, ITransportSettings[] transportSettings)
        {
            var expectedRoot = new X509Certificate2(filePath);

            Debug.WriteLine("TrustBundleProvider.SetupTrustBundle from file");
            SetupCerts(transportSettings, new List<X509Certificate2>() { expectedRoot });
        }

        internal void WindowsCertificatesSetup(IEnumerable<X509Certificate2> certs, ITransportSettings[] transportSettings)
        {
            foreach (ITransportSettings transportSetting in transportSettings)
            {
                switch (transportSetting.GetTransportType())
                {
                    case TransportType.Amqp_WebSocket_Only:
                    case TransportType.Amqp_Tcp_Only:
                        if (transportSetting is AmqpTransportSettings amqpTransportSettings)
                        {
                            if (amqpTransportSettings.RemoteCertificateValidationCallback == null)
                            {
                                amqpTransportSettings.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                                {
                                    return ValidateCertificate(certs.First(), certificate, chain, sslPolicyErrors);
                                };
                            }
                        }
                        break;
                    case TransportType.Http1:
                        if (transportSetting is Http1TransportSettings httpTransportSettings)
                        {
                            //TODO: set callback for http
                        }
                        break;
                    case TransportType.Mqtt_WebSocket_Only:
                    case TransportType.Mqtt_Tcp_Only:
                        if (transportSetting is MqttTransportSettings mqttTransportSettings)
                        {
                            if (mqttTransportSettings.RemoteCertificateValidationCallback == null)
                            {
                                mqttTransportSettings.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                                {
                                    return ValidateCertificate(certs.First(), certificate, chain, sslPolicyErrors);
                                };
                            }
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported Transport Type {0}".FormatInvariant(transportSetting.GetTransportType()));
                }
            }
        }

        internal void LinuxCertificatesSetup(IEnumerable<X509Certificate2> certs)
        {
            foreach (var cert in certs)
            {
                var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);
            }
        }

        private void SetupCerts(ITransportSettings[] transportSettings, IList<X509Certificate2> certs)
        {
            if (certs.Count() != 0)
            {
                Debug.WriteLine("TrustBundleProvider.SetupCerts()");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Debug.WriteLine("TrustBundleProvider.SetupCerts() on Windows");
                    WindowsCertificatesSetup(certs, transportSettings);
                }
                else
                {
                    Debug.WriteLine("TrustBundleProvider.SetupCerts() on Linux");
                    LinuxCertificatesSetup(certs);
                }
            }
        }

        private async Task<TrustBundleResponse> GetTrustBundleAsync(Uri providerUri, string apiVersion)
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

                return response;
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
                if (httpClient != null)
                {
                    httpClient.Dispose();
                }
            }
        }

        private async Task<TrustBundleResponse> GetTrustBundleWithRetry(HttpHsmClient hsmHttpClient, string apiVersion)
        {
            var transientRetryPolicy = new RetryPolicy(TransientErrorDetectionStrategy, TransientRetryStrategy);
            return await transientRetryPolicy.ExecuteAsync(() => hsmHttpClient.TrustBundleAsync(apiVersion)).ConfigureAwait(false);
        }

        private bool ValidateCertificate(X509Certificate2 trustedCertificate, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // Terminate on errors other than those caused by a chain failure
            SslPolicyErrors terminatingErrors = sslPolicyErrors & ~SslPolicyErrors.RemoteCertificateChainErrors;
            if (terminatingErrors != SslPolicyErrors.None)
            {
                Debug.WriteLine("Discovered SSL session errors: {0}", terminatingErrors);
                return false;
            }

            // Allow the chain the chance to rebuild itself with the expected root
            chain.ChainPolicy.ExtraStore.Add(trustedCertificate);
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
#if NETSTANDARD2_0
            if (!chain.Build(new X509Certificate2(certificate)))
            {
                Debug.WriteLine("Unable to build the chain using the expected root certificate.");
                return false;
            }
#else
            if (!chain.Build(new X509Certificate2(certificate.Export(X509ContentType.Cert))))
            {
                Debug.WriteLine("Unable to build the chain using the expected root certificate.");
                return false;
            }
#endif

            // Pin the trusted root of the chain to the expected root certificate
            X509Certificate2 actualRoot = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
            if (!trustedCertificate.Equals(actualRoot))
            {
                Debug.WriteLine("The certificate chain was not signed by the trusted root certificate.");
                return false;
            }
            return true;
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
