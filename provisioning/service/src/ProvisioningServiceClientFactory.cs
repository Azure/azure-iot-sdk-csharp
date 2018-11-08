// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Rest;
using System;
using System.Net.Http;

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
            _provisioningConnectionString = ServiceConnectionString.Create(connectionString);

            Uri dpsHost = new UriBuilder("https", _provisioningConnectionString.HostName).Uri;
            string sharedAccessSignature = _provisioningConnectionString.SharedAccessSignature;
            ServiceClientCredentials credentials;

            if (sharedAccessSignature != null)
            {
                credentials = new CredentialsWithToken(sharedAccessSignature);
            }
            else
            {
                credentials = new CredentialsWithSakRefresh(_provisioningConnectionString);
            }
            return new ProvisioningServiceClient(dpsHost, credentials, rootHandler, handlers);
        }
    }
}
