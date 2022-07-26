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

namespace Microsoft.Azure.Devices.Client.Edge
{
    /// <summary>
    /// Factory that creates ModuleClient based on the IoT Edge environment.
    /// </summary>
    internal class EdgeModuleClientFactory
    {
        private const string DefaultApiVersion = "2018-06-28";
        private const string IotEdgedUriVariableName = "IOTEDGE_WORKLOADURI";
        private const string IotHubHostnameVariableName = "IOTEDGE_IOTHUBHOSTNAME";
        private const string GatewayHostnameVariableName = "IOTEDGE_GATEWAYHOSTNAME";
        private const string DeviceIdVariableName = "IOTEDGE_DEVICEID";
        private const string ModuleIdVariableName = "IOTEDGE_MODULEID";
        private const string ModuleGenerationIdVariableName = "IOTEDGE_MODULEGENERATIONID";
        private const string AuthSchemeVariableName = "IOTEDGE_AUTHSCHEME";
        private const string SasTokenAuthScheme = "SasToken";
        private const string EdgehubConnectionstringVariableName = "EdgeHubConnectionString";
        private const string IothubConnectionstringVariableName = "IotHubConnectionString";
        private const string EdgeCaCertificateFileVariableName = "EdgeModuleCACertificateFile";

        private readonly ITrustBundleProvider _trustBundleProvider;
        private readonly ClientOptions _options;

        /// <summary>
        /// Initializes a new instance of the class with transport settings.
        /// </summary>
        /// <param name="trustBundleProvider">Provider implementation to get trusted bundle for certificate validation.</param>
        /// <param name="options">The options that allow configuration of the module client instance during initialization.</param>
        public EdgeModuleClientFactory(ITrustBundleProvider trustBundleProvider, ClientOptions options = default)
        {
            _trustBundleProvider = trustBundleProvider ?? throw new ArgumentNullException(nameof(trustBundleProvider));
            _options = options ?? new();
        }

        /// <summary>
        /// Creates an instance based on environment.
        /// </summary>
        public Task<ModuleClient> CreateAsync()
        {
            return CreateInternalClientFromEnvironmentAsync();
        }

        private async Task<ModuleClient> CreateInternalClientFromEnvironmentAsync()
        {
            IDictionary envVariables = Environment.GetEnvironmentVariables();

            string connectionString = GetValueFromEnvironment(envVariables, EdgehubConnectionstringVariableName) ?? GetValueFromEnvironment(envVariables, IothubConnectionstringVariableName);

            // First try to create from connection string and if env variable for connection string is not found try to create from edgedUri
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                string certPath = Environment.GetEnvironmentVariable(EdgeCaCertificateFileVariableName);

                ICertificateValidator certificateValidator = NullCertificateValidator.Instance;
                if (!string.IsNullOrWhiteSpace(certPath))
                {
                    Debug.WriteLine("EdgeModuleClientFactory setupTrustBundle from file");
                    var expectedRoot = new X509Certificate2(certPath);
                    certificateValidator = GetCertificateValidator(new List<X509Certificate2>() { expectedRoot });
                }

                return new ModuleClient(CreateInternalClientFromConnectionString(connectionString, _options), certificateValidator);
            }
            else
            {
                string edgedUri = GetValueFromEnvironment(envVariables, IotEdgedUriVariableName) ?? throw new InvalidOperationException($"Environment variable {IotEdgedUriVariableName} is required.");
                string deviceId = GetValueFromEnvironment(envVariables, DeviceIdVariableName) ?? throw new InvalidOperationException($"Environment variable {DeviceIdVariableName} is required.");
                string moduleId = GetValueFromEnvironment(envVariables, ModuleIdVariableName) ?? throw new InvalidOperationException($"Environment variable {ModuleIdVariableName} is required.");
                string hostname = GetValueFromEnvironment(envVariables, IotHubHostnameVariableName) ?? throw new InvalidOperationException($"Environment variable {IotHubHostnameVariableName} is required.");
                string authScheme = GetValueFromEnvironment(envVariables, AuthSchemeVariableName) ?? throw new InvalidOperationException($"Environment variable {AuthSchemeVariableName} is required.");
                string generationId = GetValueFromEnvironment(envVariables, ModuleGenerationIdVariableName) ?? throw new InvalidOperationException($"Environment variable {ModuleGenerationIdVariableName} is required.");
                string gateway = GetValueFromEnvironment(envVariables, GatewayHostnameVariableName);

                if (!string.Equals(authScheme, SasTokenAuthScheme, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Unsupported authentication scheme. Supported scheme is {SasTokenAuthScheme}.");
                }

                ISignatureProvider signatureProvider = new HttpHsmSignatureProvider(edgedUri, DefaultApiVersion);

                TimeSpan sasTokenTimeToLive = _options.SasTokenTimeToLive;
                int sasTokenRenewalBuffer = _options.SasTokenRenewalBuffer;

#pragma warning disable CA2000 // Dispose objects before losing scope - IDisposable ModuleAuthenticationWithHsm is disposed when the client is disposed.
                // Since the sdk creates the instance of disposable ModuleAuthenticationWithHsm, the sdk needs to dispose it once the client is disposed.
                var authMethod = new ModuleAuthenticationWithHsm(signatureProvider, deviceId, moduleId, generationId, sasTokenTimeToLive, sasTokenRenewalBuffer, disposeWithClient: true);
#pragma warning restore CA2000 // Dispose objects before losing scope - IDisposable ModuleAuthenticationWithHsm is disposed when the client is disposed.

                Debug.WriteLine("EdgeModuleClientFactory setupTrustBundle from service");

                ICertificateValidator certificateValidator = NullCertificateValidator.Instance;
                if (!string.IsNullOrEmpty(gateway))
                {
                    IList<X509Certificate2> certs = await _trustBundleProvider.GetTrustBundleAsync(new Uri(edgedUri), DefaultApiVersion).ConfigureAwait(false);
                    certificateValidator = GetCertificateValidator(certs);
                    _options.GatewayHostName = gateway;
                }

                return new ModuleClient(CreateInternalClientFromAuthenticationMethod(hostname, authMethod, _options), certificateValidator);
            }
        }

        private ICertificateValidator GetCertificateValidator(IList<X509Certificate2> certs)
        {
            if (certs.Count != 0)
            {
                Debug.WriteLine("EdgeModuleClientFactory.GetCertificateValidator()");
                if (IsOSPlatform(OSPlatform.Windows))
                {
                    Debug.WriteLine("EdgeModuleClientFactory GetCertificateValidator on Windows");
                    var certValidator = CustomCertificateValidator.Create(certs, _options.TransportSettings);
                    return certValidator;
                }
                else
                {
                    Debug.WriteLine("EdgeModuleClientFactory GetCertificateValidator on Linux");
                    var certValidator = InstalledCertificateValidator.Create(certs);
                    return certValidator;
                }
            }

            return NullCertificateValidator.Instance;
        }

        private InternalClient CreateInternalClientFromConnectionString(string connectionString, ClientOptions options)
        {
            return ClientFactory.CreateFromConnectionString(connectionString, options);
        }

        private InternalClient CreateInternalClientFromAuthenticationMethod(string hostname, IAuthenticationMethod authMethod, ClientOptions options)
        {
            return ClientFactory.Create(hostname, authMethod, options);
        }

        private static string GetValueFromEnvironment(IDictionary envVariables, string variableName)
        {
            return envVariables.Contains(variableName)
                ? envVariables[variableName].ToString()
                : null;
        }
    }
}
