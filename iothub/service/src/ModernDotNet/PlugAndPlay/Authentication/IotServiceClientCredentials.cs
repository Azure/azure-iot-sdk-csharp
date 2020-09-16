// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using Microsoft.Rest;

namespace Microsoft.Azure.Devices.PlugAndPlay.Authentication
{
    public abstract class IotServiceClientCredentials : ServiceClientCredentials
    {
        /// <summary>
        /// Add a sas token to the outgoing http request, then send it to the next pipeline segment
        /// </summary>
        /// <param name="request">The request that is being sent</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The result of calling the next pipeline to process the request</returns>
        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.ThrowIfNull(nameof(HttpRequestMessage));

            request.Headers.Add(HttpRequestHeader.Authorization.ToString(), GetSasToken());
            request.Headers.Add(HttpRequestHeader.UserAgent.ToString(), Utils.GetClientVersion());

            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }

        /// <summary>
        /// Return a SAS token
        /// </summary>
        /// <returns>A sas token</returns>
        protected abstract string GetSasToken();
    }
}
