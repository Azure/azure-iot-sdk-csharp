﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Discovery.Client.Transport.Http;
using Microsoft.Azure.Devices.Discovery.Client.Transport.Http.Models;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Discovery.Client.Transport
{
    /// <summary>
    /// Represents the HTTP protocol implementation for the Provisioning Transport Handler.
    /// </summary>
    public class DiscoveryTransportHandlerHttp : DiscoveryTransportHandler
    {
        private const int DefaultHttpsPort = 443;
        private static readonly TimeSpan s_defaultOperationPoolingIntervalMilliseconds = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Creates an instance of the ProvisioningTransportHandlerHttp class.
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

                string registrationId = request.Security.GetRegistrationID();

                var onboardRequest = new ChallengeRequest(request.RegistrationId, request.EndorsementKey, request.StorageRootKey);

                Challenge challenge = await client.DiscoveryRegistrations
                    .IssueChallengeAsync("v1", onboardRequest, cancellationToken)
                    .ConfigureAwait(false);

                return challenge.Key;
            }
            catch (HttpOperationException ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(DiscoveryTransportHandlerHttp)} threw exception {ex}", nameof(IssueChallengeAsync));

                bool isTransient = ex.Response.StatusCode >= HttpStatusCode.InternalServerError
                    || (int)ex.Response.StatusCode == 429;

                try
                {
                    AzureCoreFoundationsError errorDetails = JsonConvert.DeserializeObject<AzureCoreFoundationsError>(ex.Response.Content);
                    throw new DiscoveryTransportException(ex.Response.Content, ex, isTransient);
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
        /// Issue challenge
        /// </summary>
        /// <returns></returns>
        public override async Task<string> GetOnboardingInfoAsync(DiscoveryTransportIssueChallengeRequest request, CancellationToken cancellationToken)
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

                string registrationId = request.Security.GetRegistrationID();

                var onboardInfoRequest = new BootstrapRequest(request.RegistrationId, request.EndorsementKey, request.StorageRootKey, "CSR");

                BootstrapResponse onboardInfo = await client.DiscoveryRegistrations
                    .GetOnboardingInfoAsync("v1", onboardInfoRequest,"token", cancellationToken)
                    .ConfigureAwait(false);

                return onboardInfo.ProvisioningEndpoint;
            }
            catch (HttpOperationException ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(DiscoveryTransportHandlerHttp)} threw exception {ex}", nameof(IssueChallengeAsync));

                bool isTransient = ex.Response.StatusCode >= HttpStatusCode.InternalServerError
                    || (int)ex.Response.StatusCode == 429;

                try
                {
                    AzureCoreFoundationsError errorDetails = JsonConvert.DeserializeObject<AzureCoreFoundationsError>(ex.Response.Content);
                    throw new DiscoveryTransportException(ex.Response.Content, ex, isTransient);
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
    }
}
