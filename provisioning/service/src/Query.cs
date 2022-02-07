// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Service.Auth;
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
    ///         <see cref="ProvisioningServiceClient.CreateIndividualEnrollmentQuery(QuerySpecification, int)">IndividualEnrollment</see>
    ///     </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <see cref="ProvisioningServiceClient.CreateEnrollmentGroupQuery(QuerySpecification, int)">EnrollmentGroup</see>
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <see cref="ProvisioningServiceClient.CreateEnrollmentGroupRegistrationStateQuery(QuerySpecification, String, int)">RegistrationStatus</see>
    ///         </description>
    ///     </item>
    /// </list>
    /// On all cases, the <see cref="QuerySpecification"/> contains a SQL query that must follow the
    ///     Query Language for the Device Provisioning Service.
    ///
    /// Optionally, an <c>Integer</c> with the <b>page size</b>, can determine the maximum number of the items in the
    ///     <see cref="QueryResult"/> returned by the <see cref="NextAsync()"/>. It must be any positive integer, and if it 
    ///     contains 0, the Device Provisioning Service will ignore it and use a standard page size.
    ///
    /// You can use this Object as a standard iterator, just using the <c>HasNext</c> and <c>NextAsync</c> in a
    ///     <c>while</c> loop, up to the point where the <c>HasNext</c> contains <c>false</c>. But, keep 
    ///     in mind that the <see cref="QueryResult"/> can contain a empty list, even if the <c>HasNext</c> contained 
    ///     <c>true</c>. For example, image that you have 10 IndividualEnrollment in the Device Provisioning Service 
    ///     and you created new query with the <c>PageSize</c> equals 5. In the first iteration, <c>HasNext</c> 
    ///     will contains <c>true</c>, and the first <c>NextAsync</c> will return a <c>QueryResult</c> with 
    ///     5 items. After, your code will check the <c>HasNext</c>, which will contains <c>true</c> again. Now, 
    ///     before you get the next page, somebody deletes all the IndividualEnrollment. What happened, when you call the 
    ///     <c>NextAsync</c>, it will return a valid <c>QueryResult</c>, but the <see cref="QueryResult.Items"/> 
    ///     will contain an empty list.
    ///
    /// Besides the <c>Items</c>, the <c>QueryResult</c> contains the <see cref="QueryResult.ContinuationToken"/>.
    ///     You can also store a query context (QuerySpecification + ContinuationToken) and restart it in the future, from
    ///     the point where you stopped. Just recreating the query with the same <see cref="QuerySpecification"/> and calling
    ///     the <see cref="NextAsync(string)"/> passing the stored <c>ContinuationToken</c>.
    /// </remarks>
    public class Query : IDisposable
    {
        private const string ContinuationTokenHeaderKey = "x-ms-continuation";
        private const string ItemTypeHeaderKey = "x-ms-item-type";
        private const string PageSizeHeaderKey = "x-ms-max-item-count";
        private const string QueryUriFormat = "{0}/query?{1}";


        private readonly string _querySpecificationJson;
        private IContractApiHttp _contractApiHttp;
        private readonly Uri _queryPath;
        private readonly CancellationToken _cancellationToken;
        private bool _hasNext;

        internal Query(
            ServiceConnectionString serviceConnectionString, 
            string serviceName,
            QuerySpecification querySpecification,
            HttpTransportSettings httpTransportSettings,
            int pageSize,
            CancellationToken cancellationToken)
        {
            /* SRS_QUERY_21_001: [The constructor shall throw ArgumentNullException if the provided serviceConnectionString is null.] */
            if (serviceConnectionString == null)
            {
                throw new ArgumentNullException(nameof(serviceConnectionString));
            }

            /* SRS_QUERY_21_002: [The constructor shall throw ArgumentException if the provided serviceName is null or empty.] */
            if (string.IsNullOrWhiteSpace(serviceName ?? throw new ArgumentNullException(nameof(serviceName))))
            {
                throw new ArgumentException($"{nameof(serviceName)} cannot be an empty string");
            }

            /* SRS_QUERY_21_003: [The constructor shall throw ArgumentException if the provided querySpecification is null.] */
            if (querySpecification == null)
            {
                throw new ArgumentNullException(nameof(querySpecification));
            }

            /* SRS_QUERY_21_004: [The constructor shall throw ArgumentException if the provided pageSize is negative.] */
            if (pageSize < 0)
            {
                throw new ArgumentException($"{nameof(pageSize)} cannot be negative.");
            }

            // TODO: Refactor ContractApiHttp being created again
            /* SRS_QUERY_21_005: [The constructor shall create and store a `contractApiHttp` using the provided Service Connection String.] */
            _contractApiHttp = new ContractApiHttp(
                serviceConnectionString.HttpsEndpoint,
                serviceConnectionString, httpTransportSettings);

            /* SRS_QUERY_21_006: [The constructor shall store the provided  `pageSize`, and `cancelationToken`.] */
            PageSize = pageSize;
            _cancellationToken = cancellationToken;

            /* SRS_QUERY_21_007: [The constructor shall create and store a JSON from the provided querySpecification.] */
            _querySpecificationJson = JsonConvert.SerializeObject(querySpecification);

            /* SRS_QUERY_21_008: [The constructor shall create and store a queryPath adding `/query` to the provided `targetPath`.] */
            _queryPath = GetQueryUri(serviceName);

            /* SRS_QUERY_21_009: [The constructor shall set continuationToken and current as null.] */
            ContinuationToken = null;

            /* SRS_QUERY_21_010: [The constructor shall set hasNext as true.] */
            _hasNext = true;
        }

        /// <summary>
        /// Getter for has next
        /// </summary>
        /// <remarks>
        /// Contains <c>true</c> if the query is not finished in the Device Provisioning Service, and another
        ///     iteration with <see cref="NextAsync()"/> may return more items. Call <see cref="NextAsync()"/> after 
        ///     a <c>true</c> <c>HasNext</c> will result in a <see cref="QueryResult"/> that can or 
        ///     cannot contains elements. But call <see cref="NextAsync()"/> after a <c>false</c> <c>HasNext</c> 
        ///     will result in a exception.
        /// </remarks>
        public bool HasNext()
        { 
            return _hasNext;
        }

        /// <summary>
        /// Getter and setter for PageSize.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Getter and setter for the ContinuationToken.
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Return the next page of result for the query using a new continuationToken.
        /// </summary>
        /// <param name="continuationToken">the <c>String</c> with the previous continuationToken. It cannot be <c>null</c> or empty.</param>
        /// <returns>The <see cref="QueryResult"/> with the next page of items for the query.</returns>
        /// <exception cref="IndexOutOfRangeException">if the query does no have more pages to return.</exception>
        public async Task<QueryResult> NextAsync(string continuationToken)
        {
            /* SRS_QUERY_21_011: [The next shall throw IndexOutOfRangeException if the provided continuationToken is null or empty.] */
            if (string.IsNullOrWhiteSpace(continuationToken))
            {
                throw new IndexOutOfRangeException($"There is no {nameof(continuationToken)} to get pending elements.");
            }

            /* SRS_QUERY_21_012: [The next shall store the provided continuationToken.] */
            ContinuationToken = continuationToken;
            _hasNext = true;

            /* SRS_QUERY_21_013: [The next shall return the next page of results by calling the next().] */
            return await NextAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Return the next page of result for the query.
        /// </summary>
        /// <returns>The <see cref="QueryResult"/> with the next page of items for the query.</returns>
        /// <exception cref="IndexOutOfRangeException">if the query does no have more pages to return.</exception>
        public async Task<QueryResult> NextAsync()
        {
            /* SRS_QUERY_21_014: [The next shall throw IndexOutOfRangeException if the hasNext is false.] */
            if (!_hasNext)
            {
                throw new IndexOutOfRangeException("There are no more pending elements");
            }

            /* SRS_QUERY_21_015: [If the pageSize is not 0, the next shall send the HTTP request with `x-ms-max-item-count=[pageSize]` in the header.] */
            IDictionary<string, string> headerParameters = new Dictionary<string, string>();
            if (PageSize != 0)
            {
                headerParameters.Add(PageSizeHeaderKey, PageSize.ToString(CultureInfo.InvariantCulture));
            }
            /* SRS_QUERY_21_016: [If the continuationToken is not null or empty, the next shall send the HTTP request with `x-ms-continuation=[continuationToken]` in the header.] */
            if (!string.IsNullOrWhiteSpace(ContinuationToken))
            {
                headerParameters.Add(ContinuationTokenHeaderKey, ContinuationToken);
            }

            /* SRS_QUERY_21_017: [The next shall send a HTTP request with a HTTP verb `POST`.] */
            ContractApiResponse httpResponse = await _contractApiHttp.RequestAsync(
                HttpMethod.Post,
                _queryPath,
                headerParameters,
                _querySpecificationJson,
                null,
                _cancellationToken).ConfigureAwait(false);

            /* SRS_QUERY_21_018: [The next shall create and return a new instance of the QueryResult using the `x-ms-item-type` as type, `x-ms-continuation` as the next continuationToken, and the message body.] */
            httpResponse.Fields.TryGetValue(ItemTypeHeaderKey, out string type);
            httpResponse.Fields.TryGetValue(ContinuationTokenHeaderKey, out string continuationToken);
            ContinuationToken = continuationToken;

            /* SRS_QUERY_21_017: [The next shall set hasNext as true if the continuationToken is not null, or false if it is null.] */
            _hasNext = (ContinuationToken != null);

            var result = new QueryResult(type, httpResponse.Body, ContinuationToken);

            return result;
        }

        /// <summary>
        /// Dispose the HTTP resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the Component and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_contractApiHttp != null)
                { 
                    _contractApiHttp.Dispose();
                    _contractApiHttp = null;
                }
            }
        }

        private static Uri GetQueryUri(string path)
        {
            return new Uri(QueryUriFormat.FormatInvariant(path, SDKUtils.ApiVersionQueryString), UriKind.Relative);
        }
    }
}
