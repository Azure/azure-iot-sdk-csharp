// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Represents the template class for the results of an IQuery request
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "CA1710:Identifiers should have correct suffix",
        Justification = "Cannot rename public facing types since they are considered behavior changes.")]
    public class QueryResponse<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _queryResults;

        /// <summary>
        /// Instantiates a QueryResponse that represents the template class for the results of an IQuery request
        /// </summary>
        public QueryResponse(IEnumerable<T> queryResults, string continuationToken)
        {
            _queryResults = queryResults;
            ContinuationToken = continuationToken;
        }

        /// <summary>
        /// Gets the ContinuationToken to use for continuing the enumeration
        /// </summary>
        public string ContinuationToken { get; private set; }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return _queryResults.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _queryResults.GetEnumerator();
        }
    }
}
