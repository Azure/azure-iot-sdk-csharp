// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Specifies the options associated with queries
    /// </summary>
    public class QueryOptions
    {
        /// <summary>
        /// The token to use for continuing the query enumeration
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// The page size to request for each page of query results.
        /// </summary>
        public int? PageSize { get; set; }
    }
}
