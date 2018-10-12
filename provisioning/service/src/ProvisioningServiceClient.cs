// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Microsoft.Azure.Devices.Common.Service.Auth;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    public partial class ProvisioningServiceClient
    {
        private static ServiceConnectionString _provisioningConnectionString;

        /// <summary>
        /// Creates a ServiceClientCredential from the DPS Connection String
        /// </summary>
        /// <param name="connectionString"> The DPS Connection String </param>
        /// <returns></returns>
        public static ServiceClientCredentials CreateCredentialsFromConnectionString(String connectionString)
        {
            _provisioningConnectionString = ServiceConnectionString.Parse(connectionString);
            string sharedAccessSignature = _provisioningConnectionString.SharedAccessSignature;
            if (sharedAccessSignature != null)
            {
                return new SharedAccessSignatureCredentials(sharedAccessSignature);
            }
            else
            {
                return new SharedAccessKeyCredentials(connectionString);
            }
        }
    }
}
