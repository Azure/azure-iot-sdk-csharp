// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal interface IContractApiHttp : IDisposable
    {
        Task<ContractApiResponse> RequestAsync(
            HttpMethod httpMethod,
            Uri requestUri,
            IDictionary<string, string> customHeaders,
            string body,
            ETag ifMatch,
            CancellationToken cancellationToken);
    }
}
