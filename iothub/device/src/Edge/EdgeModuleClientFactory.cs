// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Microsoft.Azure.Devices.Client.HsmAuthentication;

namespace Microsoft.Azure.Devices.Client.Edge
{
    /// <summary>
    /// Factory that creates ModuleClient based on the IoT Edge environment.
    /// </summary>
    internal class EdgeModuleClientFactory
    {
        const string IotEdgedUriVariableName = "IOTEDGE_IOTEDGEDURI";
        const string IotEdgedApiVersionVariableName = "IOTEDGE_IOTEDGEDVERSION";
        const string IotHubHostnameVariableName = "IOTEDGE_IOTHUBHOSTNAME";
        const string GatewayHostnameVariableName = "IOTEDGE_GATEWAYHOSTNAME";
        const string DeviceIdVariableName = "IOTEDGE_DEVICEID";
        const string ModuleIdVariableName = "IOTEDGE_MODULEID";
        const string AuthSchemeVariableName = "IOTEDGE_AUTHSCHEME";
        const string SasTokenAuthScheme = "SasToken";
        const string EdgehubConnectionstringVariableName = "EdgeHubConnectionString";
        const string IothubConnectionstringVariableName = "IotHubConnectionString";

        readonly ITransportSettings[] transportSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgeModuleClientFactory"/> class with transport settings.
        /// </summary>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings.</param>
        public EdgeModuleClientFactory(ITransportSettings[] transportSettings)
        {
            this.transportSettings = transportSettings ?? throw new ArgumentNullException(nameof(transportSettings));
        }

        /// <summary>
        /// Creates a ModuleClient instance based on environment.
        /// </summary>
        /// <returns></returns>
        public ModuleClient Create()
        {
            return this.CreateDeviceClientFromEnvironment();
        }

        ModuleClient CreateDeviceClientFromEnvironment()
        {
            IDictionary envVariables = Environment.GetEnvironmentVariables();

            string connectionString = this.GetValueFromEnvironment(envVariables, EdgehubConnectionstringVariableName) ?? this.GetValueFromEnvironment(envVariables, IothubConnectionstringVariableName);

            // First try to create from connection string and if env variable for connection string is not found try to create from edgedUri
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return this.CreateDeviceClientFromConnectionString(connectionString);
            }
            else
            {
                string edgedUri = this.GetValueFromEnvironment(envVariables, IotEdgedUriVariableName) ?? throw new InvalidOperationException($"Environement variable {IotEdgedUriVariableName} is required.");
                string deviceId = this.GetValueFromEnvironment(envVariables, DeviceIdVariableName) ?? throw new InvalidOperationException($"Environement variable {DeviceIdVariableName} is required.");
                string moduleId = this.GetValueFromEnvironment(envVariables, ModuleIdVariableName) ?? throw new InvalidOperationException($"Environement variable {ModuleIdVariableName} is required.");
                string hostname = this.GetValueFromEnvironment(envVariables, IotHubHostnameVariableName) ?? throw new InvalidOperationException($"Environement variable {IotHubHostnameVariableName} is required.");
                string authScheme = this.GetValueFromEnvironment(envVariables, AuthSchemeVariableName) ?? throw new InvalidOperationException($"Environement variable {AuthSchemeVariableName} is required.");
                string gateway = this.GetValueFromEnvironment(envVariables, GatewayHostnameVariableName);
                string apiVersion = this.GetValueFromEnvironment(envVariables, IotEdgedApiVersionVariableName);

                if (!string.Equals(authScheme, SasTokenAuthScheme, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Unsupported authentication scheme. Supported scheme is {SasTokenAuthScheme}.");
                }

                ISignatureProvider signatureProvider = string.IsNullOrWhiteSpace(apiVersion)
                    ? new HttpHsmSignatureProvider(edgedUri)
                    : new HttpHsmSignatureProvider(edgedUri, apiVersion);
                var authMethod = new ModuleAuthenticationWithHsm(signatureProvider, deviceId, moduleId);

                return this.CreateDeviceClientFromAuthenticationMethod(hostname, gateway, authMethod);
            }
        }

        ModuleClient CreateDeviceClientFromConnectionString(string connectionString)
        {
            return ModuleClient.CreateFromConnectionString(connectionString, this.transportSettings);
        }

        ModuleClient CreateDeviceClientFromAuthenticationMethod(string hostname, string gateway, IAuthenticationMethod authMethod)
        {
            return ModuleClient.Create(hostname, gateway, authMethod, this.transportSettings);
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
