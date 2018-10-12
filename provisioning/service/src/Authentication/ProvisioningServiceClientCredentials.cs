// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Common;
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
        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add(HttpRequestHeader.Authorization.ToString(), GetSasToken());
            request.Headers.Add("User-Agent", _productInfo.ToString());
            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }

        /// <summary>
        /// Return the SAS token
        /// </summary>
        /// <returns></returns>
        protected abstract string GetSasToken();
    }
}
