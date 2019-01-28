// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Microsoft.Azure.Devices.Shared;

    /// <summary>
    ///     Query on device twins, device twin aggregates and device jobs
    /// </summary>
    class Query : IQuery
    {
        private string continuationToken = string.Empty;
        private bool newQuery = true;
        private readonly Func<string, Task<QueryResult>> queryTaskFunc;

        /// <summary>
        ///     internal ctor
        /// </summary>
        /// <param name="queryTaskFunc"></param>
        internal Query(Func<string, Task<QueryResult>> queryTaskFunc)
        {
            this.queryTaskFunc = queryTaskFunc;
        }

        /// <summary>
        ///     return true before any next calls or when a continuation token is present
        /// </summary>
        public bool HasMoreResults => newQuery || !string.IsNullOrEmpty(this.continuationToken);

        /// <summary>
        ///     fetch the next paged result as twins
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Twin>> GetNextAsTwinAsync()
        {
            return await GetNextAsTwinAsync(null).ConfigureAwait(false);
        }

        public async Task<QueryResponse<Twin>> GetNextAsTwinAsync(QueryOptions options)
        {
            IEnumerable<Twin> result = this.HasMoreResults
                ? await GetAndCastNextResultAsync<Twin>(QueryResultType.Twin, options).ConfigureAwait(false)
                : new List<Twin>();

            return new QueryResponse<Twin>(result, this.continuationToken);
        }

        /// <summary>
        ///     fetch the next paged result as device jobs
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<DeviceJob>> GetNextAsDeviceJobAsync()
        {
            return await GetNextAsDeviceJobAsync(null).ConfigureAwait(false);
        }

        public async Task<QueryResponse<DeviceJob>> GetNextAsDeviceJobAsync(QueryOptions options)
        {
            IEnumerable<DeviceJob> result = this.HasMoreResults
                ? await GetAndCastNextResultAsync<DeviceJob>(QueryResultType.DeviceJob, options).ConfigureAwait(false)
                : new List<DeviceJob>();

            return new QueryResponse<DeviceJob>(result, this.continuationToken);
        }

        /// <summary>
        ///     fetch the next paged result as job responses
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<JobResponse>> GetNextAsJobResponseAsync()
        {
            return await GetNextAsJobResponseAsync(null).ConfigureAwait(false);
        }

        public async Task<QueryResponse<JobResponse>> GetNextAsJobResponseAsync(QueryOptions options)
        {
            IEnumerable<JobResponse> result = this.HasMoreResults
                ? await GetAndCastNextResultAsync<JobResponse>(QueryResultType.JobResponse, options).ConfigureAwait(false)
                : new List<JobResponse>();

            return new QueryResponse<JobResponse>(result, this.continuationToken);
        }

        /// <summary>
        ///     fetch the next paged result as Json strings
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GetNextAsJsonAsync()
        {
            return await GetNextAsJsonAsync(null).ConfigureAwait(false);
        }

        public async Task<QueryResponse<string>> GetNextAsJsonAsync(QueryOptions options)
        {
            IEnumerable<string> response;
            if (!this.HasMoreResults)
            {
                response = new List<string>();
            }
            else
            {
                QueryResult r = await GetNextAsync(options).ConfigureAwait(false);
                response = r.Items.Select(o => o.ToString());
            }

            return new QueryResponse<string>(response, this.continuationToken);
        }

        async Task<IEnumerable<T>> GetAndCastNextResultAsync<T>(QueryResultType type, QueryOptions options)
        {
            QueryResult r = await GetNextAsync(options).ConfigureAwait(false);
            return CastResultContent<T>(r, type);
        }

        static IEnumerable<T> CastResultContent<T>(QueryResult result, QueryResultType expected)
        {
            if (result.Type != expected)
            {
                throw new InvalidCastException($"result type is {result.Type}");
            }

            // TODO: optimize this 2nd parse from JObject to target object type T
            return result.Items.Select(o => ((JObject) o).ToObject<T>());
        }

        async Task<QueryResult> GetNextAsync(QueryOptions options)
        {
            QueryResult result = await queryTaskFunc(!string.IsNullOrWhiteSpace(options?.ContinuationToken) ? options.ContinuationToken : continuationToken).ConfigureAwait(false);
            this.continuationToken = result.ContinuationToken;
            this.newQuery = false;
            return result;
        }
    }
}
