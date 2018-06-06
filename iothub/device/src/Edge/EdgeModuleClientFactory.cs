// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        const string DefaultApiVersion = "2018-06-28";
        const string IotEdgedUriVariableName = "IOTEDGE_WORKLOADURI";
        const string IotHubHostnameVariableName = "IOTEDGE_IOTHUBHOSTNAME";
        const string GatewayHostnameVariableName = "IOTEDGE_GATEWAYHOSTNAME";
        const string DeviceIdVariableName = "IOTEDGE_DEVICEID";
        const string ModuleIdVariableName = "IOTEDGE_MODULEID";
        const string ModuleGenerationIdVariableName = "IOTEDGE_MODULEGENERATIONID";
        const string AuthSchemeVariableName = "IOTEDGE_AUTHSCHEME";
        const string SasTokenAuthScheme = "SasToken";
        const string EdgehubConnectionstringVariableName = "EdgeHubConnectionString";
        const string IothubConnectionstringVariableName = "IotHubConnectionString";
        const string EdgeCaCertificateFileVariableName = "EdgeModuleCACertificateFile";

        readonly ITransportSettings[] transportSettings;
        readonly ITrustBundleProvider trustBundleProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgeModuleClientFactory"/> class with transport settings.
        /// </summary>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings.</param>
        /// <param name="trustBundleProvider">Provider implementation to get trusted bundle for certificate validation.</param>
        public EdgeModuleClientFactory(ITransportSettings[] transportSettings, ITrustBundleProvider trustBundleProvider)
        {
            this.transportSettings = transportSettings ?? throw new ArgumentNullException(nameof(transportSettings));
            this.trustBundleProvider = trustBundleProvider ?? throw new ArgumentNullException(nameof(trustBundleProvider));
        }

        /// <summary>
        /// Creates a ModuleClient instance based on environment.
        /// </summary>
        /// <returns></returns>
        public Task<ModuleClient> CreateAsync()
        {
            return this.CreateInternalClientFromEnvironmentAsync();
        }

        async Task<ModuleClient> CreateInternalClientFromEnvironmentAsync()
        {
            IDictionary envVariables = Environment.GetEnvironmentVariables();

            string connectionString = this.GetValueFromEnvironment(envVariables, EdgehubConnectionstringVariableName) ?? this.GetValueFromEnvironment(envVariables, IothubConnectionstringVariableName);

            // First try to create from connection string and if env variable for connection string is not found try to create from edgedUri
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                string certPath = Environment.GetEnvironmentVariable(EdgeCaCertificateFileVariableName);

                ICertificateValidator certificateValidator = NullCertificateValidator.Instance;
                if (!string.IsNullOrWhiteSpace(certPath))
                {
                    Debug.WriteLine("EdgeModuleClientFactory setupTrustBundle from file");
                    var expectedRoot = new X509Certificate2(certPath);
                    certificateValidator = this.GetCertificateValidator(new List<X509Certificate2>() { expectedRoot });
                }

                return new ModuleClient(this.CreateInternalClientFromConnectionString(connectionString), certificateValidator);
            }
            else
            {
                string edgedUri = this.GetValueFromEnvironment(envVariables, IotEdgedUriVariableName) ?? throw new InvalidOperationException($"Environment variable {IotEdgedUriVariableName} is required.");
                string deviceId = this.GetValueFromEnvironment(envVariables, DeviceIdVariableName) ?? throw new InvalidOperationException($"Environment variable {DeviceIdVariableName} is required.");
                string moduleId = this.GetValueFromEnvironment(envVariables, ModuleIdVariableName) ?? throw new InvalidOperationException($"Environment variable {ModuleIdVariableName} is required.");
                string hostname = this.GetValueFromEnvironment(envVariables, IotHubHostnameVariableName) ?? throw new InvalidOperationException($"Environment variable {IotHubHostnameVariableName} is required.");
                string authScheme = this.GetValueFromEnvironment(envVariables, AuthSchemeVariableName) ?? throw new InvalidOperationException($"Environment variable {AuthSchemeVariableName} is required.");
                string generationId = this.GetValueFromEnvironment(envVariables, ModuleGenerationIdVariableName) ?? throw new InvalidOperationException($"Environment variable {ModuleGenerationIdVariableName} is required.");
                string gateway = this.GetValueFromEnvironment(envVariables, GatewayHostnameVariableName);

                if (!string.Equals(authScheme, SasTokenAuthScheme, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Unsupported authentication scheme. Supported scheme is {SasTokenAuthScheme}.");
                }

                ISignatureProvider signatureProvider = new HttpHsmSignatureProvider(edgedUri, DefaultApiVersion);
                var authMethod = new ModuleAuthenticationWithHsm(signatureProvider, deviceId, moduleId, generationId);

                Debug.WriteLine("EdgeModuleClientFactory setupTrustBundle from service");
                IList<X509Certificate2> certs = await trustBundleProvider.GetTrustBundleAsync(new Uri(edgedUri), DefaultApiVersion).ConfigureAwait(false);
                ICertificateValidator certificateValidator = this.GetCertificateValidator(certs);

                return new ModuleClient(this.CreateInternalClientFromAuthenticationMethod(hostname, gateway, authMethod), certificateValidator);
            }
        }

        private ICertificateValidator GetCertificateValidator(IList<X509Certificate2> certs)
        {
            if (certs.Count() != 0)
            {
                Debug.WriteLine("EdgeModuleClientFactory.GetCertificateValidator()");
                if (IsOSPlatform(OSPlatform.Windows))
                {
                    Debug.WriteLine("EdgeModuleClientFactory GetCertificateValidator on Windows");
                    var certValidator = CustomCertificateValidator.Create(certs, transportSettings);
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

        InternalClient CreateInternalClientFromConnectionString(string connectionString)
        {
            return ClientFactory.CreateFromConnectionString(connectionString, this.transportSettings);
        }

        InternalClient CreateInternalClientFromAuthenticationMethod(string hostname, string gateway, IAuthenticationMethod authMethod)
        {
            return ClientFactory.Create(hostname, gateway, authMethod, this.transportSettings);
        }

        string GetValueFromEnvironment(IDictionary envVariables, string variableName)
        {
            if (envVariables.Contains(variableName))
            {
                return envVariables[variableName].ToString();
            }

            return null;
        }
    }
}
