// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// This wraps underlying paged query result access logic.
    /// </summary>
    /// <remarks>
    /// Use pattern:
    ///
    /// IQuery q = await registryManager.CreateQuery(sql, pagesize);
    /// while (q.HasMoreResults) {
    ///     IEnumerable&lt;Twin&gt; result = r.GetNextAsTwinAsync();
    ///     // access individual device twin records
    /// }
    /// </remarks>
    public interface IQuery
    {
        /// <summary>
        /// Indicate if more results can be fetched
        /// </summary>
        bool HasMoreResults { get; }

        /// <summary>
        /// Retrieves the next paged result as <see cref="Twin"/> objects
        /// </summary>
        /// <returns>List of <see cref="Twin"/> objects</returns>
        Task<IEnumerable<Twin>> GetNextAsTwinAsync();

        /// <summary>
        /// Retrieves the next paged result as <see cref="Twin"/> objects
        /// </summary>
        /// <param name="options">Query options</param>
        /// <returns>An enumerable <see cref="QueryResponse{T}"/> object</returns>
        Task<QueryResponse<Twin>> GetNextAsTwinAsync(QueryOptions options);

        /// <summary>
        /// Retrieves the next paged result as <see cref="DeviceJob"/> objects
        /// </summary>
        /// <returns>List of <see cref="DeviceJob"/> objects</returns>
        Task<IEnumerable<DeviceJob>> GetNextAsDeviceJobAsync();

        /// <summary>
        /// Retrieves the next paged result as <see cref="DeviceJob"/> objects
        /// </summary>
        /// <param name="options">Query options</param>
        /// <returns>An enumerable <see cref="QueryResponse{T}"/> object</returns>
        Task<QueryResponse<DeviceJob>> GetNextAsDeviceJobAsync(QueryOptions options);

        /// <summary>
        /// Retrieves the next paged result as <see cref="JobResponse"/> objects
        /// </summary>
        /// <returns>List of <see cref="JobResponse"/> objects</returns>
        Task<IEnumerable<JobResponse>> GetNextAsJobResponseAsync();

        /// <summary>
        /// Retrieves the next paged result as <see cref="JobResponse"/> objects
        /// </summary>
        /// <param name="options">Query options</param>
        /// <returns>An enumerable <see cref="QueryResponse{T}"/> object</returns>
        Task<QueryResponse<JobResponse>> GetNextAsJobResponseAsync(QueryOptions options);

        /// <summary>
        /// Retrieves the next paged result as JSON strings
        /// </summary>
        /// <returns>List of JSON strings</returns>
        Task<IEnumerable<string>> GetNextAsJsonAsync();

        /// <summary>
        /// Retrieves the next paged result as JSON strings
        /// </summary>
        /// <param name="options">Query options</param>
        /// <returns>An enumerable <see cref="QueryResponse{T}"/> object</returns>
        Task<QueryResponse<string>> GetNextAsJsonAsync(QueryOptions options);
    }
}
