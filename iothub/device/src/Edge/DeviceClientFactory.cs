// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Microsoft.Azure.Devices.Client.HsmAuthentication;

namespace Microsoft.Azure.Devices.Client.Edge
{
    /// <summary>
    /// Factory that creates DeviceClient based on the IoT Edge environment.
    /// </summary>
    public class DeviceClientFactory
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

        readonly TransportType? transportType;
        readonly ITransportSettings[] transportSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceClientFactory"/> class.
        /// </summary>
        public DeviceClientFactory()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceClientFactory"/> class with transport type.
        /// </summary>
        /// <param name="transportType">Specifies whether AMQP, MQTT or HTTP transport is used.</param>
        public DeviceClientFactory(TransportType transportType)
        {
            this.transportType = transportType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceClientFactory"/> class with transport settings.
        /// </summary>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings.</param>
        public DeviceClientFactory(ITransportSettings[] transportSettings)
        {
            this.transportSettings = transportSettings;
        }

        /// <summary>
        /// Creates a DeviceClient instance based on environment.
        /// </summary>
        /// <returns></returns>
        public DeviceClient Create()
        {
            return this.CreateDeviceClientFromEnvironment();
        }

        DeviceClient CreateDeviceClientFromEnvironment()
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

        DeviceClient CreateDeviceClientFromConnectionString(string connectionString)
        {
            if (this.transportSettings != null)
            {
                return DeviceClient.CreateFromConnectionString(connectionString, this.transportSettings);
            }

            if (this.transportType.HasValue)
            {
                return DeviceClient.CreateFromConnectionString(connectionString, this.transportType.Value);
            }

            return DeviceClient.CreateFromConnectionString(connectionString);
        }

        DeviceClient CreateDeviceClientFromAuthenticationMethod(string hostname, string gateway, IAuthenticationMethod authMethod)
        {
            if (this.transportSettings != null)
            {
                return DeviceClient.Create(hostname, gateway, authMethod, this.transportSettings);
            }

            if (this.transportType.HasValue)
            {
                return DeviceClient.Create(hostname, gateway, authMethod, this.transportType.Value);
            }

            return DeviceClient.Create(hostname, gateway, authMethod);
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
