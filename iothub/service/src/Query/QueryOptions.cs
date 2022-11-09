// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Specifies the options associated with queries.
    /// </summary>
    public class QueryOptions
    {
        /// <summary>
        /// The token to use for continuing the query enumeration.
        /// </summary>
        /// <remarks>
        /// By default, this library will fill in this value for you as needed. For example, if you run
        /// a query of page size 5 that has 10 total items to return, this library will fetch the second
        /// page of results even if you do not provide this value when calling MoveNextAsync() at the end
        /// of the first page of results.
        /// </remarks>
        /// <example>
        /// <code language="csharp">
        /// QueryResponse&lt;Twin&gt; queriedTwins = await iotHubServiceClient.Query.CreateAsync&lt;Twin&gt;("SELECT * FROM devices");
        /// // This call will use the previous continuation token for you when it comes time to get the
        /// // next page of results.
        /// while (await queriedTwins.MoveNextAsync())
        /// {
        ///     Twin queriedTwin = queriedTwins.Current;
        ///     Console.WriteLine(queriedTwin);
        /// }
        /// </code>
        /// </example>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// The page size to request for each page of query results.
        /// </summary>
        public int? PageSize { get; set; }
    }
}
