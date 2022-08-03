// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    ///     Query on device twins, device twin aggregates and device jobs
    /// </summary>
    internal class Query : IQuery
    {
        private string _continuationToken = string.Empty;
        private bool _newQuery = true;
        private readonly Func<string, Task<QueryResult>> _queryTaskFunc;

        /// <summary>
        ///     internal ctor
        /// </summary>
        /// <param name="queryTaskFunc"></param>
        internal Query(Func<string, Task<QueryResult>> queryTaskFunc)
        {
            _queryTaskFunc = queryTaskFunc;
        }

        /// <summary>
        ///     return true before any next calls or when a continuation token is present
        /// </summary>
        public bool HasMoreResults => _newQuery || !string.IsNullOrEmpty(_continuationToken);

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
            IEnumerable<Twin> result = HasMoreResults
                ? await GetAndCastNextResultAsync<Twin>(QueryResultType.Twin, options).ConfigureAwait(false)
                : new List<Twin>();

            return new QueryResponse<Twin>(result, _continuationToken);
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
            IEnumerable<DeviceJob> result = HasMoreResults
                ? await GetAndCastNextResultAsync<DeviceJob>(QueryResultType.DeviceJob, options).ConfigureAwait(false)
                : new List<DeviceJob>();

            return new QueryResponse<DeviceJob>(result, _continuationToken);
        }

        /// <summary>
        ///     fetch the next paged result as job responses
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ScheduledJob>> GetNextAsScheduledJobAsync()
        {
            return await GetNextAsScheduledJobAsync(null).ConfigureAwait(false);
        }

        public async Task<QueryResponse<ScheduledJob>> GetNextAsScheduledJobAsync(QueryOptions options)
        {
            IEnumerable<ScheduledJob> result = HasMoreResults
                ? await GetAndCastNextResultAsync<ScheduledJob>(QueryResultType.JobResponse, options).ConfigureAwait(false)
                : new List<ScheduledJob>();

            return new QueryResponse<ScheduledJob>(result, _continuationToken);
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
            if (!HasMoreResults)
            {
                response = new List<string>();
            }
            else
            {
                QueryResult r = await GetNextAsync(options).ConfigureAwait(false);
                response = r.Items.Select(o => o.ToString());
            }

            return new QueryResponse<string>(response, _continuationToken);
        }

        private async Task<IEnumerable<T>> GetAndCastNextResultAsync<T>(QueryResultType type, QueryOptions options)
        {
            QueryResult r = await GetNextAsync(options).ConfigureAwait(false);
            return CastResultContent<T>(r, type);
        }

        private static IEnumerable<T> CastResultContent<T>(QueryResult result, QueryResultType expected)
        {
            if (result.Type != expected)
            {
                throw new InvalidCastException($"result type is {result.Type}");
            }

            // TODO: optimize this 2nd parse from JObject to target object type T
            return result.Items.Select(o => ((JObject) o).ToObject<T>());
        }

        private async Task<QueryResult> GetNextAsync(QueryOptions options)
        {
            QueryResult result = await _queryTaskFunc(!string.IsNullOrWhiteSpace(options?.ContinuationToken) ? options.ContinuationToken : _continuationToken).ConfigureAwait(false);
            _continuationToken = result.ContinuationToken;
            _newQuery = false;
            return result;
        }
    }
}
