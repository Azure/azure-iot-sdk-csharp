// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Discovery.Client.Transport.Http;
using Microsoft.Azure.Devices.Discovery.Client.Transport.Http.Models;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Microsoft.Azure.Devices.Discovery.Client.Transport
{
    /// <summary>
    /// Represents the HTTP protocol implementation for the Discovery Transport Handler.
    /// </summary>
    public class DiscoveryTransportHandlerHttp : DiscoveryTransportHandler
    {
        private const int DefaultHttpsPort = 443;
        private static readonly TimeSpan s_defaultOperationPoolingIntervalMilliseconds = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Creates an instance of the <see cref="DiscoveryTransportHandlerHttp"/> class.
        /// </summary>
        public DiscoveryTransportHandlerHttp()
        {
            Port = DefaultHttpsPort;
            Proxy = DefaultWebProxySettings.Instance;
        }

        /// <summary>
        /// Issue challenge
        /// </summary>
        /// <returns></returns>
        public override async Task<string> IssueChallengeAsync(DiscoveryTransportIssueChallengeRequest request, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(DiscoveryTransportHandlerHttp)}.{nameof(IssueChallengeAsync)}");

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var securityProvider = request.Security;

                using var httpClientHandler = new HttpClientHandler()
                {
                    // Cannot specify a specific protocol here, as desired due to an error:
                    //   ProvisioningDeviceClient_ValidRegistrationId_AmqpWithProxy_SymmetricKey_RegisterOk_GroupEnrollment failing for me with System.PlatformNotSupportedException: Operation is not supported on this platform.	
                    // When revisiting TLS12 work for DPS, we should figure out why. Perhaps the service needs to support it.	

                    //SslProtocols = TlsVersions.Preferred,
                };

                if (Proxy != DefaultWebProxySettings.Instance)
                {
                    httpClientHandler.UseProxy = Proxy != null;
                    httpClientHandler.Proxy = Proxy;
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"{nameof(IssueChallengeAsync)} Setting HttpClientHandler.Proxy");
                }

                var builder = new UriBuilder
                {
                    Scheme = Uri.UriSchemeHttps,
                    Host = request.GlobalDeviceEndpoint,
                    Port = Port,
                };

                using EdgeDiscoveryService client = new EdgeDiscoveryService(builder.Uri, httpClientHandler);
                client.HttpClient.DefaultRequestHeaders.Add("User-Agent", request.ProductInfo);
                if (Logging.IsEnabled)
                    Logging.Info(this, $"Uri: {builder.Uri}; User-Agent: {request.ProductInfo}");

                string registrationId = securityProvider.GetRegistrationID();

                var onboardRequest = new ChallengeRequest(
                    registrationId, 
                    Convert.ToBase64String(securityProvider.GetEndorsementKey()), 
                    Convert.ToBase64String(securityProvider.GetStorageRootKey()));

                Challenge challenge = await client.DiscoveryRegistrations
                    .IssueChallengeAsync(onboardRequest, cancellationToken)
                    .ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.IssueChallenge(
                        this,
                        registrationId);

                return challenge.Key;
            }
            catch (AzureCoreFoundationsErrorResponseException ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(DiscoveryTransportHandlerHttp)} threw exception {ex}", nameof(IssueChallengeAsync));

                bool isTransient = ex.Response.StatusCode >= HttpStatusCode.InternalServerError
                    || (int)ex.Response.StatusCode == 429;

                AzureCoreFoundationsError errorDetails = JsonConvert.DeserializeObject<AzureCoreFoundationsError>(ex.Response.Content);
                throw new DiscoveryTransportException(ex.Response.Content, ex, isTransient, errorDetails.Code, errorDetails.Message);
            }
            catch (Exception ex) when (!(ex is DiscoveryTransportException))
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(DiscoveryTransportHandlerHttp)} threw exception {ex}", nameof(IssueChallengeAsync));

                throw new DiscoveryTransportException($"HTTP transport exception", ex, true);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(DiscoveryTransportHandlerHttp)}.{nameof(IssueChallengeAsync)}");
            }
        }

        /// <summary>
        /// Get onboarding info
        /// </summary>
        /// <returns></returns>
        public override async Task<OnboardingInfo> GetOnboardingInfoAsync(DiscoveryTransportGetOnboardingInfoRequest request, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(DiscoveryTransportHandlerHttp)}.{nameof(IssueChallengeAsync)}");

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var securityProvider = request.Security;

                using var httpClientHandler = new HttpClientHandler()
                {
                    // Cannot specify a specific protocol here, as desired due to an error:
                    //   ProvisioningDeviceClient_ValidRegistrationId_AmqpWithProxy_SymmetricKey_RegisterOk_GroupEnrollment failing for me with System.PlatformNotSupportedException: Operation is not supported on this platform.	
                    // When revisiting TLS12 work for DPS, we should figure out why. Perhaps the service needs to support it.	

                    //SslProtocols = TlsVersions.Preferred,
                };

                if (Proxy != DefaultWebProxySettings.Instance)
                {
                    httpClientHandler.UseProxy = Proxy != null;
                    httpClientHandler.Proxy = Proxy;
                    if (Logging.IsEnabled)
                        Logging.Info(this, $"{nameof(GetOnboardingInfoAsync)} Setting HttpClientHandler.Proxy");
                }

                var builder = new UriBuilder
                {
                    Scheme = Uri.UriSchemeHttps,
                    Host = request.GlobalDeviceEndpoint,
                    Port = Port,
                };

                using EdgeDiscoveryService client = new EdgeDiscoveryService(builder.Uri, httpClientHandler);
                client.HttpClient.DefaultRequestHeaders.Add("User-Agent", request.ProductInfo);
                if (Logging.IsEnabled)
                    Logging.Info(this, $"Uri: {builder.Uri}; User-Agent: {request.ProductInfo}");

                string registrationId = securityProvider.GetRegistrationID();

                using RSA rsa = RSA.Create();

                string csr = GenerateCSR(rsa, registrationId);

                var onboardInfoRequest = new BootstrapRequest(
                    registrationId, 
                    Convert.ToBase64String(securityProvider.GetEndorsementKey()), 
                    Convert.ToBase64String(securityProvider.GetStorageRootKey()), 
                    csr);

                string target = $"registrations/{registrationId}";

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Target of token: {target}");

                string sasToken = ProvisioningSasBuilder.ExtractServiceAuthKey(
                            securityProvider,
                            target,
                            Convert.FromBase64String(request.Nonce));

                BootstrapResponse onboardInfo = await client.DiscoveryRegistrations
                    .GetOnboardingInfoAsync(onboardInfoRequest, sasToken, cancellationToken)
                    .ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.GetOnboardingInfo(
                        this,
                        registrationId);

                X509Certificate2 certWithKey;

                try
                {
                    string x509String = ((X509Credential)onboardInfo.IssuedCredential).X509;

                    string pemHeader = "-----BEGIN CERTIFICATE-----";
                    int indexOfStart = x509String.IndexOf(pemHeader) + pemHeader.Length;
                    string certString = x509String.Substring(indexOfStart, x509String.IndexOf("-----END CERTIFICATE-----") - indexOfStart);

                    using X509Certificate2 cert = new X509Certificate2(Convert.FromBase64String(certString));
                    certWithKey = cert.CopyWithPrivateKey(rsa);
                }
                catch (Exception e)
                {
                    throw new DiscoveryTransportException("Invalid certificate supplied by discovery service.", e);
                }
                

                return new OnboardingInfo(onboardInfo.EdgeProvisioningEndpoint, certWithKey);
            }
            catch (AzureCoreFoundationsErrorResponseException ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(DiscoveryTransportHandlerHttp)} threw exception {ex}", nameof(IssueChallengeAsync));

                bool isTransient = ex.Response.StatusCode >= HttpStatusCode.InternalServerError
                    || (int)ex.Response.StatusCode == 429;

                AzureCoreFoundationsError errorDetails = JsonConvert.DeserializeObject<AzureCoreFoundationsError>(ex.Response.Content);
                throw new DiscoveryTransportException(ex.Response.Content, ex, isTransient, errorDetails.Code, errorDetails.Message);
            }
            catch (Exception ex) when (!(ex is DiscoveryTransportException))
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(DiscoveryTransportHandlerHttp)} threw exception {ex}", nameof(GetOnboardingInfoAsync));

                throw new DiscoveryTransportException($"HTTP transport exception", ex, true);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(DiscoveryTransportHandlerHttp)}.{nameof(GetOnboardingInfoAsync)}");
            }
        }

        private string GenerateCSR(RSA rsa, string deviceId)
        {
            // Create a new CertificateRequest object
            CertificateRequest request = new CertificateRequest(
                subjectName: "CN=" + deviceId,
                key: rsa,
                hashAlgorithm: HashAlgorithmName.SHA256,
                padding: RSASignaturePadding.Pkcs1);

            // Generate the CSR
            byte[] csrBytes = request.CreateSigningRequest();

            // Convert the CSR to a Base64-encoded string
            string csrBase64 = PemEncodeSigningRequest(csrBytes);

            return csrBase64;
        }

        private string PemEncodeSigningRequest(byte[] csr)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("-----BEGIN CERTIFICATE REQUEST-----");
            builder.AppendLine(Convert.ToBase64String(csr, Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine("-----END CERTIFICATE REQUEST-----");

            return builder.ToString();
        }
    }
}
