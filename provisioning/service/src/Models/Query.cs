// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The query iterator.
    /// </summary>
    /// <remarks>
    /// The <see cref="Query"/> iterator is the result of the query factory for
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <see cref="IndividualEnrollmentsClient.CreateQuery(string, int, CancellationToken)">IndividualEnrollment</see>
    ///     </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <see cref="EnrollmentGroupsClient.CreateQuery(string, int, CancellationToken)">EnrollmentGroup</see>
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <see cref="DeviceRegistrationStatesClient.CreateEnrollmentGroupQuery(string, string, int, CancellationToken)">RegistrationStatus</see>
    ///         </description>
    ///     </item>
    /// </list>
    /// On all cases, the <see cref="QuerySpecification"/> contains a SQL query that must follow the
    ///     Query Language for the Device Provisioning Service.
    ///
    /// Optionally, an Integer with the page size, can determine the maximum number of the items in the
    ///     <see cref="QueryResult"/> returned by the <see cref="NextAsync()"/>. It must be any positive integer, and if it
    ///     contains 0, the Device Provisioning Service will ignore it and use a standard page size.
    ///
    /// You can use this Object as a standard iterator, just using the <c>HasNext</c> and <c>NextAsync</c> in a
    ///     <c>while</c> loop, up to the point where the <c>HasNext</c> contains false. But, keep
    ///     in mind that the <see cref="QueryResult"/> can contain a empty list, even if the <c>HasNext</c> contained
    ///     <c>true</c>. For example, image that you have 10 IndividualEnrollment in the Device Provisioning Service
    ///     and you created new query with the <c>PageSize</c> equals 5. In the first iteration, <c>HasNext</c>
    ///     will contains <c>true</c>, and the first <c>NextAsync</c> will return a <c>QueryResult</c> with
    ///     5 items. After, your code will check the <c>HasNext</c>, which will contains true again. Now,
    ///     before you get the next page, somebody deletes all the IndividualEnrollment. What happened, when you call the
    ///     <c>NextAsync</c>, it will return a valid <c>QueryResult</c>, but the <see cref="QueryResult.Items"/>
    ///     will contain an empty list.
    ///
    /// Besides the <c>Items</c>, the <c>QueryResult</c> contains the <see cref="QueryResult.ContinuationToken"/>.
    ///     You can also store a query context (QuerySpecification + ContinuationToken) and restart it in the future, from
    ///     the point where you stopped. Just recreating the query with the same <see cref="QuerySpecification"/> and calling
    ///     the <see cref="NextAsync(string)"/> passing the stored <c>ContinuationToken</c>.
    /// </remarks>
    public class Query
    {
        private const string ContinuationTokenHeaderKey = "x-ms-continuation";
        private const string ItemTypeHeaderKey = "x-ms-item-type";
        private const string PageSizeHeaderKey = "x-ms-max-item-count";
        private const string QueryUriFormat = "{0}/query?{1}";

        private readonly string _querySpecificationJson;
        private readonly IContractApiHttp _contractApiHttp;
        private readonly Uri _queryPath;
        private readonly CancellationToken _cancellationToken;
        private readonly RetryHandler _internalRetryHandler;
        private bool _hasNext;

        internal Query(
            string serviceName,
            string query,
            IContractApiHttp contractApiHttp,
            int pageSize,
            RetryHandler retryHandler,
            CancellationToken cancellationToken)
        {
            if (pageSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Cannot be negative.");
            }

            _contractApiHttp = contractApiHttp;
            PageSize = pageSize;
            _internalRetryHandler = retryHandler;
            _cancellationToken = cancellationToken;
            _querySpecificationJson = JsonConvert.SerializeObject(new QuerySpecification(query));
            _queryPath = GetQueryUri(serviceName);
            ContinuationToken = null;
            _hasNext = true;
        }

        /// <summary>
        /// Getter for has next.
        /// </summary>
        /// <remarks>
        /// Contains true if the query is not finished in the Device Provisioning Service, and another
        /// iteration with <see cref="NextAsync()"/> may return more items. Call <see cref="NextAsync()"/> after
        /// a true <c>HasNext</c> will result in a <see cref="QueryResult"/> that can or
        /// cannot contains elements. But call <see cref="NextAsync()"/> after a false <c>HasNext</c>
        /// will result in a exception.
        /// </remarks>
        public bool HasNext()
        {
            return _hasNext;
        }

        /// <summary>
        /// The number of items in the current page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The token to retrieve the next page.
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Return the next page of result for the query using a new continuationToken.
        /// </summary>
        /// <param name="continuationToken">the string with the previous continuationToken. It cannot be null or empty.</param>
        /// <returns>The <see cref="QueryResult"/> with the next page of items for the query.</returns>
        /// <exception cref="InvalidOperationException">If the query does no have more pages to return.</exception>
        public async Task<QueryResult> NextAsync(string continuationToken)
        {
            if (string.IsNullOrWhiteSpace(continuationToken))
            {
                throw new InvalidOperationException($"There is no {nameof(continuationToken)} to get pending elements.");
            }

            ContinuationToken = continuationToken;
            _hasNext = true;

            return await NextAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Return the next page of result for the query.
        /// </summary>
        /// <returns>The <see cref="QueryResult"/> with the next page of items for the query.</returns>
        /// <exception cref="InvalidOperationException">If the query does no have more pages to return.</exception>
        public async Task<QueryResult> NextAsync()
        {
            if (!_hasNext)
            {
                throw new InvalidOperationException("There are no more pending elements");
            }

            IDictionary<string, string> headerParameters = new Dictionary<string, string>();
            if (PageSize != 0)
            {
                headerParameters.Add(PageSizeHeaderKey, PageSize.ToString(CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrWhiteSpace(ContinuationToken))
            {
                headerParameters.Add(ContinuationTokenHeaderKey, ContinuationToken);
            }

            ContractApiResponse httpResponse = null;

            await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            httpResponse = await _contractApiHttp
                                .RequestAsync(
                                    HttpMethod.Post,
                                    _queryPath,
                                    headerParameters,
                                    _querySpecificationJson,
                                    new ETag(),
                                    _cancellationToken)
                                .ConfigureAwait(false);
                        },
                        _cancellationToken)
                    .ConfigureAwait(false);

            httpResponse.Fields.TryGetValue(ItemTypeHeaderKey, out string type);
            httpResponse.Fields.TryGetValue(ContinuationTokenHeaderKey, out string continuationToken);
            ContinuationToken = continuationToken;

            _hasNext = ContinuationToken != null;

            var result = new QueryResult(type, httpResponse.Body, ContinuationToken);

            return result;
        }

        private static Uri GetQueryUri(string path)
        {
            return new Uri(string.Format(CultureInfo.InvariantCulture, QueryUriFormat, path, SdkUtils.ApiVersionQueryString), UriKind.Relative);
        }
    }
}
