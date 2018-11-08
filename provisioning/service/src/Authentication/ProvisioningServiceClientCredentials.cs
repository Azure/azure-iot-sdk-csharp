// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using Microsoft.Rest;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Base credentials class.
    /// </summary>
    public abstract class ProvisioningServiceClientCredentials : ServiceClientCredentials
    {
        private ProductInfo _productInfo = new ProductInfo();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string sasToken = await GetSasToken().ConfigureAwait(false);

            request.Headers.Add(HttpRequestHeader.Authorization.ToString(), sasToken);
            request.Headers.Add("User-Agent", _productInfo.ToString());
            await base.ProcessHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Return the SAS token
        /// </summary>
        /// <returns></returns>
        public abstract Task<string> GetSasToken();
    }
}
