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
        /// <param name="pageSize"></param>
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
            return await this.GetNextAsTwinAsync(null);
        }

        public async Task<QueryResponse<Twin>> GetNextAsTwinAsync(QueryOptions options)
        {
            IEnumerable<Twin> result = this.HasMoreResults
                ? await this.GetAndCastNextResultAsync<Twin>(QueryResultType.Twin, options)
                : new List<Twin>();

            return new QueryResponse<Twin>(result, this.continuationToken);
        }

        /// <summary>
        ///     fetch the next paged result as device jobs
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<DeviceJob>> GetNextAsDeviceJobAsync()
        {
            return await this.GetNextAsDeviceJobAsync(null);
        }

        public async Task<QueryResponse<DeviceJob>> GetNextAsDeviceJobAsync(QueryOptions options)
        {
            IEnumerable<DeviceJob> result = this.HasMoreResults
                ? await this.GetAndCastNextResultAsync<DeviceJob>(QueryResultType.DeviceJob, options)
                : new List<DeviceJob>();

            return new QueryResponse<DeviceJob>(result, this.continuationToken);
        }

        /// <summary>
        ///     fetch the next paged result as job responses
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<JobResponse>> GetNextAsJobResponseAsync()
        {
            return await this.GetNextAsJobResponseAsync(null);
        }

        public async Task<QueryResponse<JobResponse>> GetNextAsJobResponseAsync(QueryOptions options)
        {
            IEnumerable<JobResponse> result = this.HasMoreResults
                ? await this.GetAndCastNextResultAsync<JobResponse>(QueryResultType.JobResponse, options)
                : new List<JobResponse>();

            return new QueryResponse<JobResponse>(result, this.continuationToken);
        }

        /// <summary>
        ///     fetch the next paged result as Json strings
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GetNextAsJsonAsync()
        {
            return await this.GetNextAsJsonAsync(null);
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
                QueryResult r = await this.GetNextAsync(options);
                response = r.Items.Select(o => o.ToString());
            }

            return new QueryResponse<string>(response, this.continuationToken);
        }

        async Task<IEnumerable<T>> GetAndCastNextResultAsync<T>(QueryResultType type, QueryOptions options)
        {
            QueryResult r = await this.GetNextAsync(options);
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
            QueryResult result = await this.queryTaskFunc(!string.IsNullOrWhiteSpace(options?.ContinuationToken) ? options.ContinuationToken : this.continuationToken);
            this.continuationToken = result.ContinuationToken;
            this.newQuery = false;
            return result;
        }
    }
}
