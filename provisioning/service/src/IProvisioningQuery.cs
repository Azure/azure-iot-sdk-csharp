// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    ///     This wraps underlying paged query result access logic.
    ///     Use pattern:
    /// 
    ///     IProvisioningQuery q = await registryManager.CreateQuery(sql, pagesize);
    ///     while (q.HasMoreResults) {
    ///         IEnumerable&lt;Enrollment&gt; result = r.GetNextAsEnrollmentAsync();
    ///         // access individual enrollment records
    ///     }
    /// </summary>
    public interface IProvisioningQuery
    {
        /// <summary>
        ///     Indicate if more results can be fetched
        /// </summary>
        bool HasMoreResults { get; }

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

        /// <summary>
        /// Retrieves the next paged result as <see cref="Enrollment"/> objects
        /// </summary>
        /// <returns>List of <see cref="Enrollment"/> objects</returns>
        Task<IEnumerable<Enrollment>> GetNextAsEnrollmentAsync();

        /// <summary>
        /// Retrieves the next paged result as <see cref="Enrollment"/> objects
        /// </summary>
        /// <param name="options">Query options</param>
        /// <returns>An enumerable <see cref="QueryResponse{T}"/> object</returns>
        Task<QueryResponse<Enrollment>> GetNextAsEnrollmentAsync(QueryOptions options);

        /// <summary>
        /// Retrieves the next paged result as <see cref="EnrollmentGroup"/> objects
        /// </summary>
        /// <returns>List of <see cref="EnrollmentGroup"/> objects</returns>
        Task<IEnumerable<EnrollmentGroup>> GetNextAsEnrollmentGroupAsync();

        /// <summary>
        /// Retrieves the next paged result as <see cref="EnrollmentGroup"/> objects
        /// </summary>
        /// <param name="options">Query options</param>
        /// <returns>An enumerable <see cref="QueryResponse{T}"/> object</returns>
        Task<QueryResponse<EnrollmentGroup>> GetNextAsEnrollmentGroupAsync(QueryOptions options);

        /// <summary>
        /// Retrieves the next paged result as <see cref="RegistrationStatus"/> objects
        /// </summary>
        /// <returns>List of <see cref="RegistrationStatus"/> objects</returns>
        Task<IEnumerable<RegistrationStatus>> GetNextAsRegistrationStatusAsync();

        /// <summary>
        /// Retrieves the next paged result as <see cref="RegistrationStatus"/> objects
        /// </summary>
        /// <param name="options">Query options</param>
        /// <returns>An enumerable <see cref="QueryResponse{T}"/> object</returns>
        Task<QueryResponse<RegistrationStatus>> GetNextAsRegistrationStatusAsync(QueryOptions options);
    }
}
