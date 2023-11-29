// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Discovery.Client.Transport.Http;
using Microsoft.Azure.Devices.Discovery.Client.Transport.Http.Models;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

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
        /// Issue TPM Challenge. Will return an encrypted nonce, that can be used to
        /// sign a SAS Token for the GetOnboardingInfo request.
        /// </summary>
        /// <returns></returns>
        public override async Task<byte[]> IssueChallengeAsync(DiscoveryTransportIssueChallengeRequest request, CancellationToken cancellationToken)
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

                using var httpClientHandler = new HttpClientHandler();

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

                using MicrosoftFairfieldGardensDiscovery client = new MicrosoftFairfieldGardensDiscovery(builder.Uri, httpClientHandler);
                client.HttpClient.DefaultRequestHeaders.Add("User-Agent", request.ProductInfo);
                if (Logging.IsEnabled)
                    Logging.Info(this, $"Uri: {builder.Uri}; User-Agent: {request.ProductInfo}");

                string registrationId = securityProvider.GetRegistrationID();

                var onboardRequest = new ChallengeRequest(
                    registrationId, 
                    securityProvider.GetEndorsementKey(), 
                    securityProvider.GetStorageRootKey());

                Challenge challenge = await client.DiscoveryRegistrations
                    .IssueChallengeAsync("2023-12-01-preview", onboardRequest, cancellationToken: cancellationToken)
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

                try
                {
                    AzureCoreFoundationsError errorDetails = JsonConvert.DeserializeObject<AzureCoreFoundationsError>(ex.Response.Content);
                    throw new DiscoveryTransportException(ex.Response.Content, ex, isTransient, errorDetails.Code, errorDetails.Message);
                }
                catch (JsonException jex)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(
                            this,
                            $"{nameof(DiscoveryTransportHandlerHttp)} server returned malformed error response. Parsing error: {jex}. Server response: {ex.Response.Content}",
                            nameof(IssueChallengeAsync));

                    throw new DiscoveryTransportException(
                        $"HTTP transport exception: malformed server error message: '{ex.Response.Content}'",
                        jex,
                        false);
                }
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

                using MicrosoftFairfieldGardensDiscovery client = new MicrosoftFairfieldGardensDiscovery(builder.Uri, httpClientHandler);
                client.HttpClient.DefaultRequestHeaders.Add("User-Agent", request.ProductInfo);
                if (Logging.IsEnabled)
                    Logging.Info(this, $"Uri: {builder.Uri}; User-Agent: {request.ProductInfo}");

                string registrationId = securityProvider.GetRegistrationID();

                using RSA rsa = RSA.Create();

                byte[] csr = GenerateCSR(rsa, registrationId);

                string target = $"registrations/{registrationId}";

                if (Logging.IsEnabled)
                    Logging.Info(this, $"Target of token: {target}");

                string sasToken = ProvisioningSasBuilder.ExtractServiceAuthKey(
                            securityProvider,
                            target,
                            request.Nonce);

                var onboardInfoRequest = new BootstrapRequest(
                    registrationId,
                    securityProvider.GetEndorsementKey(),
                    securityProvider.GetStorageRootKey(),
                    csr);

                BootstrapResponse onboardInfo = await client.DiscoveryRegistrations
                    .GetOnboardingInfoAsync("2023-12-01-preview", onboardInfoRequest, sasToken, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (Logging.IsEnabled)
                    Logging.GetOnboardingInfo(
                        this,
                        registrationId);

                X509Certificate2Collection certsWithKey;

                try
                {
                    var certs = ((X509Credential)onboardInfo.IssuedCredential).X509;

                    certsWithKey = ConvertCertificateListToChainWithPrivateKey(certs, rsa);
                }
                catch (Exception e)
                {
                    throw new DiscoveryTransportException("Invalid certificate supplied by discovery service.", e);
                }
                

                return new OnboardingInfo(onboardInfo.EdgeProvisioningEndpoint, certsWithKey);
            }
            catch (AzureCoreFoundationsErrorResponseException ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(DiscoveryTransportHandlerHttp)} threw exception {ex}", nameof(IssueChallengeAsync));

                bool isTransient = ex.Response.StatusCode >= HttpStatusCode.InternalServerError
                    || (int)ex.Response.StatusCode == 429;

                try
                {
                    AzureCoreFoundationsError errorDetails = JsonConvert.DeserializeObject<AzureCoreFoundationsError>(ex.Response.Content);
                    throw new DiscoveryTransportException(ex.Response.Content, ex, isTransient, errorDetails.Code, errorDetails.Message);
                }
                catch (JsonException jex)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(
                            this,
                            $"{nameof(DiscoveryTransportHandlerHttp)} server returned malformed error response. Parsing error: {jex}. Server response: {ex.Response.Content}",
                            nameof(IssueChallengeAsync));

                    throw new DiscoveryTransportException(
                        $"HTTP transport exception: malformed server error message: '{ex.Response.Content}'",
                        jex,
                        false);
                }
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

        /// <summary>
        /// Creates a new CSR based off of a provided device ID and RSA key.
        /// </summary>
        /// <param name="rsa"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        private byte[] GenerateCSR(RSA rsa, string deviceId)
        {
            // Create a new CertificateRequest object
            CertificateRequest request = new CertificateRequest(
                subjectName: "CN=" + deviceId,
                key: rsa,
                hashAlgorithm: HashAlgorithmName.SHA256,
                padding: RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));

            // Generate the CSR
            byte[] csrBytes = request.CreateSigningRequest();

            return csrBytes;
        }

        /// <summary>
        /// Converts a list of certificates, with the first certificate being the device (leaf) certificate, to a certificate chain with a private RSA or ECDsa key.
        /// </summary>
        /// <param name="certificateByteArrays">A list of one or more DER-encoded X.509 certificates, with the first certificate being the device (leaf) certificate.</param>
        /// <param name="rsa">The private key associated with the device (leaf) certificate.</param>
        /// <returns>An X509Certificate2Collection containing the certificate chain, with leaf certificate containing private key.</returns>
        public static X509Certificate2Collection ConvertCertificateListToChainWithPrivateKey(IList<byte[]> certificateByteArrays, RSA rsa)
        {
            X509Certificate2Collection tempCol = new X509Certificate2Collection();

            foreach (byte[] certBytes in certificateByteArrays)
            {
                var certificate = new X509Certificate2(certBytes);
                if (tempCol.Count == 0)
                {
                    certificate = certificate.CopyWithPrivateKey(rsa);
                }
                _ = tempCol.Add(certificate);
            }
            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.Import(tempCol.Export(X509ContentType.Pkcs12) ?? throw new FormatException("No certificate chain was created."), null, X509KeyStorageFlags.UserKeySet);
            return collection;
        }
    }
}
