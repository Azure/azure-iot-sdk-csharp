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
        /// The maximum number of results to fetch in the enumeration operation
        /// </summary>
        public int? PageSize { get; set; }

        /// <summary>
        /// The token to use for continuing the query enumeration
        /// </summary>
        public string ContinuationToken { get; set; }
    }
}
