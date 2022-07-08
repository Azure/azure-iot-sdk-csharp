// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal class ApiVersionDelegatingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{request.RequestUri}", nameof(SendAsync));

            NameValueCollection valueCollection = HttpUtility.ParseQueryString(request.RequestUri.Query);
            valueCollection[ClientApiVersionHelper.ApiVersionName] = ClientApiVersionHelper.ApiVersion;

            var builder = new UriBuilder(request.RequestUri)
            {
                Query = valueCollection.ToString(),
            };

            request.RequestUri = builder.Uri;

            if (Logging.IsEnabled)
                Logging.Exit(this, $"{request.RequestUri}", nameof(SendAsync));

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
