// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class SymmetricKeyCredentials : ServiceClientCredentials
    {
        private readonly string SymmetricKey;

        public SymmetricKeyCredentials(string symmetricKey) : base()
        {
            SymmetricKey = symmetricKey;
        }
    }
}