// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Extensions;
using Microsoft.Rest;

namespace Microsoft.Azure.Devices.Authentication
{
    /// <summary>
    /// This class adds the authentication tokens to the header before calling the digital twin APIs.
    /// </summary>
    internal abstract class DigitalTwinServiceClientCredentials : ServiceClientCredentials, IAuthorizationHeaderProvider
    {
        /// <summary>
        /// Add a JWT for Azure Active Directory or SAS token to the outgoing http request, then send it to the next pipeline segment.
        /// </summary>
        /// <param name="request">The request that is being sent</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The result of calling the next pipeline to process the request</returns>
        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.ThrowIfNull(nameof(HttpRequestMessage));

            request.Headers.Add(HttpRequestHeader.Authorization.ToString(), GetAuthorizationHeader());
            request.Headers.Add(HttpRequestHeader.UserAgent.ToString(), Utils.GetClientVersion());

            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }

        /// <summary>
        /// Return a SAS token
        /// </summary>
        /// <returns>A SAS token</returns>
        public abstract string GetAuthorizationHeader();
    }
}
