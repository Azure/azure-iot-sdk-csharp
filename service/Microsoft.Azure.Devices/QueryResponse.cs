// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the template class for the results of an IQuery request
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    public class QueryResponse<T> : IEnumerable<T>
    {
        readonly IEnumerable<T> queryResults;

        public QueryResponse(IEnumerable<T> queryResults, string continuationToken)
        {
            this.queryResults = queryResults;
            this.ContinuationToken = continuationToken;
        }

        /// <summary>
        /// Gets the ContinuationToken to use for continuing the enumeration
        /// </summary>
        public string ContinuationToken { get; private set; }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return this.queryResults.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.queryResults.GetEnumerator();
        }
    }
}
