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
        private string continuationToken;
        private bool newQuery = true;
        private readonly Func<string, Task<QueryResult>> queryTaskFunc;

        /// <summary>
        ///     internal ctor
        /// </summary>
        /// <param name="queryTaskFunc"></param>
        /// <param name="continuationToken"></param>
        internal Query(Func<string, Task<QueryResult>> queryTaskFunc, string continuationToken = null)
        {
            this.queryTaskFunc = queryTaskFunc;
            this.continuationToken = continuationToken;
        }

        /// <summary>
        ///     return true before any next calls or when a continuation token is present
        /// </summary>
        public bool HasMoreResults => this.newQuery || !string.IsNullOrEmpty(this.continuationToken);

        /// <summary>
        ///     fetch the next paged result as twins
        /// </summary>
        /// <returns></returns>
        public async Task<QueryResponse<Twin>> GetNextAsTwinAsync()
        {
            IEnumerable<Twin> result = this.HasMoreResults
                ? await this.GetAndCastNextResultAsync<Twin>(QueryResultType.Twin)
                : new List<Twin>();
            return new QueryResponse<Twin>(result, this.continuationToken);
        }

        /// <summary>
        ///     fetch the next paged result as device jobs
        /// </summary>
        /// <returns></returns>
        public async Task<QueryResponse<DeviceJob>> GetNextAsDeviceJobAsync()
        {
            IEnumerable<DeviceJob> result = this.HasMoreResults
                ? await this.GetAndCastNextResultAsync<DeviceJob>(QueryResultType.DeviceJob)
                : new List<DeviceJob>();
            return new QueryResponse<DeviceJob>(result, this.continuationToken);
        }

        /// <summary>
        ///     fetch the next paged result as job responses
        /// </summary>
        /// <returns></returns>
        public async Task<QueryResponse<JobResponse>> GetNextAsJobResponseAsync()
        {
            IEnumerable<JobResponse> result = this.HasMoreResults
                ? await this.GetAndCastNextResultAsync<JobResponse>(QueryResultType.JobResponse)
                : new List<JobResponse>();
            return new QueryResponse<JobResponse>(result, this.continuationToken);
        }

        /// <summary>
        ///     fetch the next paged result as Json strings
        /// </summary>
        /// <returns></returns>
        public async Task<QueryResponse<string>> GetNextAsJsonAsync()
        {
            if (!this.HasMoreResults)
            {
                return new QueryResponse<string>(new List<string>(), this.continuationToken);
            }

            QueryResult r = await this.GetNextAsync();
            IEnumerable<string> result = r.Items.Select(o => o.ToString());
            return new QueryResponse<string>(result, this.continuationToken);
        }

        async Task<IEnumerable<T>> GetAndCastNextResultAsync<T>(QueryResultType type)
        {
            QueryResult r = await this.GetNextAsync();
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

        async Task<QueryResult> GetNextAsync()
        {
            this.newQuery = false;
            QueryResult result = await this.queryTaskFunc(this.continuationToken);
            this.continuationToken = result.ContinuationToken;
            return result;
        }
    }
}
