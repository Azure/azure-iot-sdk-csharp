// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.HsmAuthentication;
using static System.Runtime.InteropServices.RuntimeInformation;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Helper to create an Edge <see cref="IotHubModuleClient"/> based on the IoT Edge environment.
    /// </summary>
    internal static class EdgeModuleClientHelper
    {
        private const string DefaultApiVersion = "2018-06-28";
        private const string IotEdgedUriVariableName = "IOTEDGE_WORKLOADURI";
        private const string IotHubHostNameVariableName = "IOTEDGE_IOTHUBHOSTNAME";
        private const string GatewayHostnameVariableName = "IOTEDGE_GATEWAYHOSTNAME";
        private const string DeviceIdVariableName = "IOTEDGE_DEVICEID";
        private const string ModuleIdVariableName = "IOTEDGE_MODULEID";
        private const string ModuleGenerationIdVariableName = "IOTEDGE_MODULEGENERATIONID";
        private const string AuthSchemeVariableName = "IOTEDGE_AUTHSCHEME";
        private const string SasTokenAuthScheme = "SasToken";
        private const string EdgehubConnectionstringVariableName = "EdgeHubConnectionString";
        private const string IothubConnectionstringVariableName = "IotHubConnectionString";
        private const string EdgeCaCertificateFileVariableName = "EdgeModuleCACertificateFile";

        internal static IotHubConnectionCredentials CreateIotHubConnectionCredentialsFromEnvironment()
        {
            IDictionary envVariables = Environment.GetEnvironmentVariables();

            string connectionString = GetValueFromEnvironment(envVariables, EdgehubConnectionstringVariableName)
                ?? GetValueFromEnvironment(envVariables, IothubConnectionstringVariableName);

            // First try to create from connection string and if env variable for connection string is not found try to create from edgedUri
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return new IotHubConnectionCredentials(connectionString);
            }

            string edgedUri = GetValueFromEnvironment(envVariables, IotEdgedUriVariableName)
                ?? throw new InvalidOperationException($"Environment variable {IotEdgedUriVariableName} is required.");
            string deviceId = GetValueFromEnvironment(envVariables, DeviceIdVariableName)
                ?? throw new InvalidOperationException($"Environment variable {DeviceIdVariableName} is required.");
            string moduleId = GetValueFromEnvironment(envVariables, ModuleIdVariableName)
                ?? throw new InvalidOperationException($"Environment variable {ModuleIdVariableName} is required.");
            string hostName = GetValueFromEnvironment(envVariables, IotHubHostNameVariableName)
                ?? throw new InvalidOperationException($"Environment variable {IotHubHostNameVariableName} is required.");
            string authScheme = GetValueFromEnvironment(envVariables, AuthSchemeVariableName)
                ?? throw new InvalidOperationException($"Environment variable {AuthSchemeVariableName} is required.");
            string generationId = GetValueFromEnvironment(envVariables, ModuleGenerationIdVariableName)
                ?? throw new InvalidOperationException($"Environment variable {ModuleGenerationIdVariableName} is required.");
            string gateway = GetValueFromEnvironment(envVariables, GatewayHostnameVariableName);

            if (!StringComparer.OrdinalIgnoreCase.Equals(authScheme, SasTokenAuthScheme))
            {
                throw new InvalidOperationException($"Unsupported authentication scheme. Supported scheme is {SasTokenAuthScheme}.");
            }

            ISignatureProvider signatureProvider = new HttpHsmSignatureProvider(edgedUri, DefaultApiVersion);

            // TODO: environment variables need to be added to accept SasTokenTimeToLive and SasTokenRenewalBuffer.
            // These values can then be passed on to ModuleAuthenticationWithHsm (internal class).

            var authMethod = new ModuleAuthenticationWithHsm(
                signatureProvider,
                deviceId,
                moduleId,
                generationId);

            if (Logging.IsEnabled)
                Logging.Info("EdgeModuleClientFactory setupTrustBundle from service");

            return new IotHubConnectionCredentials(authMethod, hostName, gateway);
        }

        internal static async Task<ICertificateValidator> CreateCertificateValidatorFromEnvironmentAsync(ITrustBundleProvider trustBundleProvider, IotHubClientOptions options)
        {
            Debug.Assert(options != null);

            ICertificateValidator certificateValidator = NullCertificateValidator.Instance;

            IDictionary envVariables = Environment.GetEnvironmentVariables();

            string connectionString = GetValueFromEnvironment(envVariables, EdgehubConnectionstringVariableName)
                ?? GetValueFromEnvironment(envVariables, IothubConnectionstringVariableName);

            // First try to create from connection string and if env variable for connection string is not found try to create from edgedUri
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                string certPath = Environment.GetEnvironmentVariable(EdgeCaCertificateFileVariableName);

                if (!string.IsNullOrWhiteSpace(certPath))
                {
                    if (Logging.IsEnabled)
                        Logging.Info($"EdgeModuleClientFactory setupTrustBundle from file {certPath}.");

                    var expectedRoot = new X509Certificate2(certPath);
                    certificateValidator = CreateCertificateValidator(new List<X509Certificate2>() { expectedRoot }, options);
                }

                return certificateValidator;
            }

            string edgedUri = GetValueFromEnvironment(envVariables, IotEdgedUriVariableName) ?? throw new InvalidOperationException($"Environment variable {IotEdgedUriVariableName} is required.");
            string gateway = GetValueFromEnvironment(envVariables, GatewayHostnameVariableName);

            if (Logging.IsEnabled)
                Logging.Info("EdgeModuleClientFactory setupTrustBundle from service");

            if (!string.IsNullOrEmpty(gateway))
            {
                IList<X509Certificate2> certs = await trustBundleProvider.GetTrustBundleAsync(new Uri(edgedUri), DefaultApiVersion).ConfigureAwait(false);
                certificateValidator = CreateCertificateValidator(certs, options);
            }

            return certificateValidator;
        }

        private static ICertificateValidator CreateCertificateValidator(IList<X509Certificate2> certs, IotHubClientOptions options)
        {
            if (certs.Count != 0)
            {
                if (Logging.IsEnabled)
                {
                    string os = IsOSPlatform(OSPlatform.Windows) ? "Windows" : "Linux";
                    Logging.Info($"EdgeModuleClientFactory.GetCertificateValidator() on {os}.");
                }

                return IsOSPlatform(OSPlatform.Windows)
                    ? CustomCertificateValidator.Create(certs, options.TransportSettings)
                    : InstalledCertificateValidator.Create(certs);
            }

            return NullCertificateValidator.Instance;
        }

        private static string GetValueFromEnvironment(IDictionary envVariables, string variableName)
        {
            return envVariables.Contains(variableName)
                ? envVariables[variableName].ToString()
                : null;
        }
    }
}
