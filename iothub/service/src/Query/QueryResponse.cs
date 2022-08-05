// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Represents the template class for the results of an IQuery request
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    public class QueryResponse<T>
    {
        private readonly QueryClient _client;
        private readonly string _originalQuery;
        private readonly JobType? _jobType;
        private readonly JobStatus? _jobStatus;
        private readonly int? _defaultPageSize;

        private IEnumerator<T> _items;

        internal QueryResponse(QueryClient client, string query, IEnumerable<T> queryResults, string continuationToken, int? defaultPageSize)
        {
            _client = client;
            _originalQuery = query;
            CurrentPage = queryResults;
            _items = queryResults.GetEnumerator();
            ContinuationToken = continuationToken;
            Current = _items.Current;
            _defaultPageSize = defaultPageSize;
        }

        internal QueryResponse(QueryClient client, JobType? jobType, JobStatus? jobStatus, IEnumerable<T> queryResults, string continuationToken, int? defaultPageSize)
        {
            _client = client;
            _jobType = jobType;
            _jobStatus = jobStatus;
            CurrentPage = queryResults;
            _items = queryResults.GetEnumerator();
            ContinuationToken = continuationToken;
            Current = _items.Current;
            _defaultPageSize = defaultPageSize;
        }

        /// <summary>
        /// Gets the ContinuationToken to use for continuing the enumeration
        /// </summary>
        public string ContinuationToken { get; internal set; }

        /// <summary>
        /// The current page of queried items.
        /// </summary>
        public IEnumerable<T> CurrentPage { get; internal set; }

        /// <summary>
        /// Advances to the next element of the query results.
        /// </summary>
        /// <returns>True if there was a next item in the query results. False if there were no more items.</returns>
        /// <example>
        /// <c>
        /// QueryResponse&lt;Twin&gt; queriedTwins = await iotHubServiceClient.Query.CreateAsync&lt;Twin&gt;("SELECT * FROM devices");
        /// while (await queriedTwins.MoveNextAsync())
        /// {
        ///     Twin queriedTwin = queriedTwins.Current;
        ///     Console.WriteLine(queriedTwin);
        /// }
        /// </c>
        /// </example>
        /// <remarks>
        /// Like with a more typical implementation of IEnumerator, this function should be called once before checking
        /// <see cref="Current"/>.
        ///
        /// This function is async because it may make a service request to fetch the next page of results if the current page
        /// of results has been advanced through already.
        /// </remarks>
        public async Task<bool> MoveNextAsync(QueryOptions queryOptions = null, CancellationToken cancellationToken = default)
        {
            if (_items.MoveNext())
            {
                // Current page of results still had an item to return to the user.
                Current = _items.Current;
                return true;
            }

            if (ContinuationToken == null && queryOptions?.ContinuationToken == null)
            {
                // The query has no more pages of results to return and the last page has been
                // completely exhausted.
                return false;
            }

            // User's can pass in a continuation token themselves, but the default behavior
            // is to use the continuation token saved by this class when it last retrieved a page.
            var queryOptionsClone = new QueryOptions()
            {
                ContinuationToken = queryOptions?.ContinuationToken ?? ContinuationToken,
                PageSize = queryOptions?.PageSize ?? _defaultPageSize,
            };

            //TODO document thrown exceptions
            if (!string.IsNullOrEmpty(_originalQuery))
            {
                QueryResponse<T> response = await _client.CreateAsync<T>(_originalQuery, queryOptionsClone, cancellationToken);
                CurrentPage = response.CurrentPage;
                Current = CurrentPage.GetEnumerator().Current;
                ContinuationToken = response.ContinuationToken;
            }
            else
            {
                // Job type and job status may still be null here, but that's okay
                QueryResponse<ScheduledJob> response = await _client.CreateAsync(_jobType, _jobStatus, queryOptionsClone, cancellationToken);
                CurrentPage = (IEnumerable<T>)response.CurrentPage;
                Current = CurrentPage.GetEnumerator().Current;
                ContinuationToken = response.ContinuationToken;
            }

            return true;
        }

        /// <summary>
        /// Get the current query result. Can be called multiple times without advancing the query.
        /// </summary>
        /// <remarks>
        /// Like with a more typical implementation of IEnumerator, this value is null until the first
        /// <see cref="MoveNextAsync(QueryOptions, CancellationToken)"/> call is made.
        /// </remarks>
        public T Current { get; private set; }
    }
}
