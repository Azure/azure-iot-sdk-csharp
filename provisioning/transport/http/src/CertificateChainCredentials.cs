// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class CertificateChainCredentials : ServiceClientCredentials
    {
        readonly IEnumerable<X509Certificate2> _certificateChain;

        public CertificateChainCredentials(IEnumerable<X509Certificate2> certificateChain)
        {
            _certificateChain = certificateChain;
        }

        public override void InitializeServiceClient<T>(ServiceClient<T> client)
        {
            base.InitializeServiceClient(client);
            
            var httpClientHandler = client.HttpMessageHandlers.FirstOrDefault((handler) => handler is HttpClientHandler) as HttpClientHandler;

            Debug.Assert(httpClientHandler != null);
            httpClientHandler.ClientCertificates.AddRange(_certificateChain.ToArray());
        }
    }
}
