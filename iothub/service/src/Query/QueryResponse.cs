// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// An iterable set of queried items.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the queried items. For instance, when using a query such as "SELECT * FROM devices",
    /// this type should be type <see cref="Twin"/>. When using a query such as "SELECT * FROM devices.jobs",
    /// this type should be type <see cref="ScheduledJob"/>.
    /// </typeparam>
    public class QueryResponse<T>
    {
        private readonly QueryClient _client;
        private readonly string _originalQuery;
        private readonly JobType? _jobType;
        private readonly JobStatus? _jobStatus;
        private readonly int? _defaultPageSize;
        private readonly IEnumerator<T> _items;

        internal QueryResponse(
            QueryClient client,
            string query,
            IEnumerable<T> queryResults,
            string continuationToken,
            int? defaultPageSize)
        {
            _client = client;
            _originalQuery = query;
            CurrentPage = queryResults;
            _items = queryResults.GetEnumerator();
            ContinuationToken = continuationToken;
            Current = _items.Current;
            _defaultPageSize = defaultPageSize;
        }

        internal QueryResponse(
            QueryClient client,
            JobType? jobType,
            JobStatus? jobStatus,
            IEnumerable<T> queryResults,
            string continuationToken,
            int? defaultPageSize)
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
        /// Gets the continuation token to use for continuing the enumeration.
        /// </summary>
        /// <remarks>
        /// This library will handle this value for you automatically when fetching the next
        /// pages of results. This value is exposed only for more unusual cases where users
        /// choose to continue a previously interrupted query from a different machine, for example.
        /// </remarks>
        public string ContinuationToken { get; internal set; }

        /// <summary>
        /// The current page of queried items.
        /// </summary>
        /// <remarks>
        /// While you can iterate over the queried page of items using this, there is no logic
        /// built into it that allows you to fetch the next page of results automatically. Because
        /// of that, most users are better off following the sample code that iterates item by item
        /// rather than page by page.
        /// </remarks>
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
        public IEnumerable<T> CurrentPage { get; internal set; }

        /// <summary>
        /// Get the current item in the current page of the query results. Can be called multiple times without advancing the query.
        /// </summary>
        /// <remarks>
        /// Like with a more typical implementation of IEnumerator, this value is null until the first
        /// <see cref="MoveNextAsync(QueryOptions, CancellationToken)"/> call is made.
        /// </remarks>
        /// <example>
        /// <c>
        /// QueryResponse&lt;Twin&gt; queriedTwins = await iotHubServiceClient.Query.CreateAsync&lt;Twin&gt;("SELECT * FROM devices");
        /// while (await queriedTwins.MoveNextAsync()) // no item is skipped by calling this first
        /// {
        ///     Twin queriedTwin = queriedTwins.Current;
        ///     Console.WriteLine(queriedTwin);
        /// }
        /// </c>
        /// </example>
        public T Current { get; private set; }

        /// <summary>
        /// Advances to the next element of the query results.
        /// </summary>
        /// <returns>True if there was a next item in the query results. False if there were no more items.</returns>
        /// <exception cref="IotHubServiceException">
        /// If this method made a request to IoT hub to get the next page of items but IoT hub responded to
        /// the request with a non-successful status code. For example, if the provided request was throttled,
        /// <see cref="IotHubServiceException"/> with <see cref="IotHubErrorCode.ThrottlingException"/> is thrown. For a complete list of possible error cases,
        /// see <see cref="IotHubErrorCode"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If this method made a request to IoT hub to get the next page of items but the HTTP request fails due to
        /// an underlying issue such as network connectivity, DNS failure, or server certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
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
        /// of results has been advanced through already. Note that this function will return True even if it is at the end
        /// of a particular page of items as long as there is at least one more page to be fetched.
        /// </remarks>
        public async Task<bool> MoveNextAsync(QueryOptions queryOptions = default, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
            var queryOptionsClone = new JobQueryOptions
            {
                ContinuationToken = queryOptions?.ContinuationToken ?? ContinuationToken,
                PageSize = queryOptions?.PageSize ?? _defaultPageSize,
                JobType = _jobType,
                JobStatus = _jobStatus,
            };

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
                QueryResponse<ScheduledJob> response = await _client.CreateJobsQueryAsync(queryOptionsClone, cancellationToken);
                CurrentPage = (IEnumerable<T>)response.CurrentPage;
                Current = CurrentPage.GetEnumerator().Current;
                ContinuationToken = response.ContinuationToken;
            }

            return true;
        }
    }
}
