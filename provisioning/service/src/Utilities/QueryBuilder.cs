// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Azure;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.Net.Http;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal class QueryBuilder
    {
        private const string ContinuationTokenHeaderKey = "x-ms-continuation";
        private const string ItemTypeHeaderKey = "x-ms-item-type";
        private const string PageSizeHeaderKey = "x-ms-max-item-count";
        private const string QueryUriFormat = "{0}/query";

        internal static async Task<Page<T>> BuildAndSendRequestAsync<T>(ContractApiHttp contractApiHttp, RetryHandler retryHandler, string query, Uri path, string continuationToken, int? pageSizeHint, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                headers.Add(ContinuationTokenHeaderKey, continuationToken);
            }

            if (pageSizeHint != null)
            {
                headers.Add(PageSizeHeaderKey, pageSizeHint.ToString());
            }

            headers.Add("Content-Type", "application/json");

            HttpResponseMessage response = null;

            await retryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        response = await contractApiHttp
                            .RequestAsync(
                                HttpMethod.Post,
                                path,
                                null,
                                JsonConvert.SerializeObject(new QuerySpecification(query)),
                                new ETag(),
                                cancellationToken)
                            .ConfigureAwait(false);
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            string responsePayload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            QueriedPage<T> page = new QueriedPage<T>(response, responsePayload);
#pragma warning disable CA2000 // Dispose objects before losing scope
            // The disposable QueryResponse object is the user's responsibility, not the SDK's
            return Page<T>.FromValues(page.Items, page.ContinuationToken, new QueryResponse(response, responseStream));
#pragma warning restore CA2000 // Dispose objects before losing scope
        }
    }
}
