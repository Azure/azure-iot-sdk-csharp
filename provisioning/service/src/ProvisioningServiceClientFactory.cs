// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Factory class for creating ProvisioningServiceClient
    /// </summary>
    public static class ProvisioningServiceClientFactory
    {
        private static ServiceConnectionString _provisioningConnectionString;

        /// <summary>
        /// Creates a ProvisioningServiceClient from the DPS Connection String
        /// </summary>
        /// <param name="connectionString"> The DPS Connection String </param>
        /// <returns></returns>
        public static ProvisioningServiceClient CreateFromConnectionString(String connectionString)
        {
            return CreateFromConnectionString(connectionString, new HttpClientHandler());
        }

        /// <summary>
        /// Creates a ProvisioningServiceClient from the DPS Connection String, HttpClientHandler
        /// and DelegatingHandler[]
        /// </summary>
        /// <param name="connectionString"> The DPS Connection String </param>
        /// <param name="rootHandler"> The HttpClientHandler </param>
        /// <param name="handlers"> The list of DelegatingHandlers </param>
        /// <returns></returns>
        public static ProvisioningServiceClient CreateFromConnectionString(String connectionString, HttpClientHandler rootHandler, params DelegatingHandler[] handlers)
        {
            _provisioningConnectionString = ServiceConnectionString.Parse(connectionString);
            Uri dpsHost = _provisioningConnectionString.HttpsEndpoint;
            string sharedAccessSignature = _provisioningConnectionString.SharedAccessSignature;
            if (sharedAccessSignature != null)
            {
                var credential = new SharedAccessSignatureCredentials(sharedAccessSignature);
                return new ProvisioningServiceClient(dpsHost, credential, rootHandler, handlers);
            }
            else
            {
                var credential = new SharedAccessKeyCredentials(_provisioningConnectionString);
                return new ProvisioningServiceClient(dpsHost, credential, rootHandler, handlers);
            }
        }
    }
}
