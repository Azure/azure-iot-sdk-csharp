// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Common.Service.Auth;
using System.Globalization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The query iterator.
    /// </summary>
    /// <remarks>
    /// The <see cref="Query"/> iterator is the result of the query factory for
    /// <list type="bullet">
    ///     <item><b>IndividualEnrollment:</b>
    ///         <see cref="ProvisioningServiceClient.CreateIndividualEnrollmentQuery(QuerySpecification, int)"/>
    ///     </item>
    ///     <item><b>EnrollmentGroup:</b>
    ///         <see cref="ProvisioningServiceClient.CreateEnrollmentGroupQuery(QuerySpecification, int)"/>
    ///     </item>    
    ///     <item><b>RegistrationStatus:</b>
    ///         <see cref="ProvisioningServiceClient.CreateEnrollmentGroupRegistrationStateQuery(QuerySpecification, String, int)"/>
    ///     </item>
    /// </list>
    /// On all cases, the <see cref="QuerySpecification"/> contains a SQL query that must follow the
    ///     Query Language for the Device Provisioning Service.
    ///
    /// Optionally, an <code>Integer</code> with the <b>page size</b>, can determine the maximum number of the items in the
    ///     <see cref="QueryResult"/> returned by the <see cref="NextAsync()"/>. It must be any positive integer, and if it 
    ///     contains 0, the Device Provisioning Service will ignore it and use a standard page size.
    ///
    /// You can use this Object as a standard iterator, just using the <code>HasNext</code> and <code>NextAsync</code> in a
    ///     <code>while</code> loop, up to the point where the <code>HasNext</code> contains <code>false</code>. But, keep 
    ///     in mind that the <see cref="QueryResult"/> can contain a empty list, even if the <code>HasNext</code> contained 
    ///     <code>true</code>. For example, image that you have 10 IndividualEnrollment in the Device Provisioning Service 
    ///     and you created new query with the <code>PageSize</code> equals 5. In the first iteration, <code>HasNext</code> 
    ///     will contains <code>true</code>, and the first <code>NextAsync</code> will return a <code>QueryResult</code> with 
    ///     5 items. After, your code will check the <code>HasNext</code>, which will contains <code>true</code> again. Now, 
    ///     before you get the next page, somebody deletes all the IndividualEnrollment. What happened, when you call the 
    ///     <code>NextAsync</code>, it will return a valid <code>QueryResult</code>, but the <see cref="QueryResult.Items"/> 
    ///     will contain an empty list.
    ///
    /// Besides the <code>Items</code>, the <code>QueryResult</code> contains the <see cref="QueryResult.ContinuationToken"/>.
    ///     You can also store a query context (QuerySpecification + ContinuationToken) and restart it in the future, from
    ///     the point where you stopped. Just recreating the query with the same <see cref="QuerySpecification"/> and calling
    ///     the <see cref="NextAsync(string)"/> passing the stored <code>ContinuationToken</code>.
    /// </remarks>
    public class Query : IDisposable
    {
        private const string ContinuationTokenHeaderKey = "x-ms-continuation";
        private const string ItemTypeHeaderKey = "x-ms-item-type";
        private const string PageSizeHeaderKey = "x-ms-max-item-count";
        private const string QueryUriFormat = "{0}/query?{1}";


        private string _querySpecificationJson;
        private IContractApiHttp _contractApiHttp;
        private Uri _queryPath;
        private CancellationToken _cancellationToken;
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

#if !NET451
        internal Query(
            string path,
            IAuthorizationHeaderProvider headerProvider,
            string serviceName,
            QuerySpecification querySpecification,
            HttpTransportSettings httpTransportSettings,
            int pageSize,
            CancellationToken cancellationToken)
        {
            /* SRS_QUERY_21_001: [The constructor shall throw ArgumentNullException if the provided path is null.] */
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
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
            /* SRS_QUERY_21_005: [The constructor shall create and store a `contractApiHttp` using the provided path and Authorization Header Provider.] */
            _contractApiHttp = new ContractApiHttp(
                new UriBuilder("https", path).Uri,
                headerProvider, httpTransportSettings);

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
#endif

        /// <summary>
        /// Getter for has next
        /// </summary>
        /// <remarks>
        /// Contains <code>true</code> if the query is not finished in the Device Provisioning Service, and another
        ///     iteration with <see cref="NextAsync()"/> may return more items. Call <see cref="NextAsync()"/> after 
        ///     a <code>true</code> <code>HasNext</code> will result in a <see cref="QueryResult"/> that can or 
        ///     cannot contains elements. But call <see cref="NextAsync()"/> after a <code>false</code> <code>HasNext</code> 
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
        /// <param name="continuationToken">the <code>String</code> with the previous continuationToken. It cannot be <code>null</code> or empty.</param>
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
