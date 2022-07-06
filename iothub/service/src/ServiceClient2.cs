﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Microsoft.Azure.Devices.Http2;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// TODO
    /// </summary>
    public class ServiceClient2 : IDisposable
    {
        private string _hostName;
        private IotHubConnectionProperties _credentialProvider;
        private HttpClient _httpClient;
        private HttpRequestMessageFactory _httpRequestMessageFactory;

        /// <summary>
        /// TODO
        /// </summary>
        public DevicesClient Devices;

        /// <summary>
        /// TODO
        /// </summary>
        public ModulesClient Modules;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected ServiceClient2()
        {
        }

        /// <summary>
        /// Create an instance of this class that authenticates service requests using an IoT hub connection string.
        /// </summary>
        /// <param name="connectionString">The IoT hub connection string.</param>
        /// <param name="transportSettings">The optional HTTP transport settings.</param>
        public ServiceClient2(string connectionString, HttpTransportSettings2 transportSettings = default)
        {
            Argument.RequireNotNullOrEmpty(connectionString, nameof(connectionString));

            if (transportSettings == null)
            {
                transportSettings = new HttpTransportSettings2();
            }

            var iotHubConnectionString = IotHubConnectionString.Parse(connectionString);
            _credentialProvider = iotHubConnectionString;
            _hostName = iotHubConnectionString.HostName;
            _httpClient = HttpClientFactory.Create(_hostName, transportSettings);
            _httpRequestMessageFactory = new HttpRequestMessageFactory(_credentialProvider.HttpsEndpoint);

            InitializeSubclients();
        }

        /// <summary>
        /// Create an instance of this class that authenticates service requests using an identity in Azure Active
        /// Directory (AAD).
        /// </summary>
        /// <remarks>
        /// For more about information on the options of authenticating using a derived instance of <see cref="TokenCredential"/>, see
        /// <see href="https://docs.microsoft.com/dotnet/api/overview/azure/identity-readme"/>.
        /// For more information on configuring IoT hub with Azure Active Directory, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-dev-guide-azure-ad-rbac"/>
        /// </remarks>
        /// <param name="hostName">IoT hub host name. For instance: "my-iot-hub.azure-devices.net".</param>
        /// <param name="credential">Azure Active Directory (AAD) credentials to authenticate with IoT hub.</param>
        /// <param name="transportSettings">The optional HTTP transport settings.</param>
        public ServiceClient2(string hostName, TokenCredential credential, HttpTransportSettings2 transportSettings = default)
        {
            Argument.RequireNotNullOrEmpty(hostName, nameof(hostName));
            Argument.RequireNotNull(credential, nameof(credential));

            if (transportSettings == null)
            {
                transportSettings = new HttpTransportSettings2();
            }

            _credentialProvider = new IotHubTokenCrendentialProperties(hostName, credential);
            _hostName = hostName;
            _httpClient = HttpClientFactory.Create(_hostName, transportSettings);
            _httpRequestMessageFactory = new HttpRequestMessageFactory(_credentialProvider.HttpsEndpoint);

            InitializeSubclients();
        }

        /// <summary>
        /// Create an instance of this class that authenticates service requests with a shared access signature
        /// provided and refreshed as necessary by the caller.
        /// </summary>
        /// <remarks>
        /// Users may wish to build their own shared access signature (SAS) tokens rather than give the shared key to the SDK and let it manage signing and renewal.
        /// The <see cref="AzureSasCredential"/> object gives the SDK access to the SAS token, while the caller can update it as necessary using the
        /// <see cref="AzureSasCredential.Update(string)"/> method.
        /// </remarks>
        /// <param name="hostName">IoT hub host name. For instance: "my-iot-hub.azure-devices.net".</param>
        /// <param name="credential">Credential that generates a SAS token to authenticate with IoT hub. See <see cref="AzureSasCredential"/>.</param>
        /// <param name="transportSettings">The optional HTTP transport settings.</param>
        public ServiceClient2(string hostName, AzureSasCredential credential, HttpTransportSettings2 transportSettings = default)
        {
            Argument.RequireNotNullOrEmpty(hostName, nameof(hostName));
            Argument.RequireNotNull(credential, nameof(credential));

            if (transportSettings == null)
            {
                transportSettings = new HttpTransportSettings2();
            }

            _credentialProvider = new IotHubSasCredentialProperties(hostName, credential);
            _hostName = hostName;
            _httpClient = HttpClientFactory.Create(_hostName, transportSettings);
            _httpRequestMessageFactory = new HttpRequestMessageFactory(_credentialProvider.HttpsEndpoint);

            InitializeSubclients();
        }

        private void InitializeSubclients()
        {
            Devices = new DevicesClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory);
            Modules = new ModulesClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory);
        }

        /// <summary>
        /// Dispose this client and all the disposable resources it has.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
