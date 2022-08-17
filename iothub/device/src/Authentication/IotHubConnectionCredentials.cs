// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed class IotHubConnectionCredentials
    {
        internal IotHubConnectionCredentials(string connectionString)
        {
            IotHubConnectionString = IotHubConnectionStringParser.Parse(connectionString);
            AuthenticationMethod = AuthenticationMethodFactory.GetAuthenticationMethod(this);
        }

        internal IotHubConnectionCredentials(IAuthenticationMethod authenticationMethod)
        {
            AuthenticationMethod = authenticationMethod;
        }

        public IotHubConnectionString IotHubConnectionString { get; internal set; }

        public IAuthenticationMethod AuthenticationMethod { get; internal set; }

        public bool UsingX509Cert { get; internal set; }

        // Device certificate
        internal X509Certificate2 Certificate { get; set; }

        // Full chain of certificates from the one used to sign the device certificate to the one uploaded to the service.
        internal X509Certificate2Collection ChainCertificates { get; set; }
    }
}
