// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Subclient of <see cref="ProvisioningServiceClient"/> that handles all enrollment group operations including
    /// getting/creating/setting/deleting enrollment groups, querying enrollment groups, and getting attestation mechanisms
    /// for particular enrollment groups.
    /// </summary>
    public class EnrollmentGroupsClient
    {
        private const string ServiceName = "enrollmentGroups";
        private const string EnrollmentsUriFormat = "{0}?{1}";
        private const string EnrollmentIdUriFormat = "{0}/{1}?{2}";
        private const string EnrollmentAttestationName = "attestationmechanism";
        private const string EnrollmentAttestationUriFormat = "{0}/{1}/{2}?{3}";

        private readonly IContractApiHttp _contractApiHttp;
        private readonly ServiceConnectionString _serviceConnectionString;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected EnrollmentGroupsClient()
        {
        }

        internal EnrollmentGroupsClient(ServiceConnectionString serviceConnectionString, IContractApiHttp contractApiHttp)
        {
            _serviceConnectionString = serviceConnectionString;
            _contractApiHttp = contractApiHttp;
        }

        /// <summary>
        /// Create or update an enrollment group.
        /// </summary>
        /// <remarks>
        /// This API creates a new enrollment group or update a existed one. All enrollment group in the Device
        /// Provisioning Service contains a unique identifier called enrollmentGroupId. If this API is called
        /// with an enrollmentGroupId that already exists, it will replace the existed enrollment group information
        /// by the new one. On the other hand, if the enrollmentGroupId does not exit, it will be created.
        /// </remarks>
        /// <param name="enrollmentGroup">The enrollment group to create or update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created or updated enrollment group.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroup"/> is null.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to create or update the enrollment.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<EnrollmentGroup> CreateOrUpdateAsync(EnrollmentGroup enrollmentGroup, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(enrollmentGroup, nameof(enrollmentGroup));

            cancellationToken.ThrowIfCancellationRequested();

            ContractApiResponse contractApiResponse = await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Put,
                    GetEnrollmentUri(enrollmentGroup.EnrollmentGroupId),
                    null,
                    JsonConvert.SerializeObject(enrollmentGroup),
                    enrollmentGroup.ETag,
                    cancellationToken)
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<EnrollmentGroup>(contractApiResponse.Body);
        }

        /// <summary>
        /// Get an enrollment group by its Id.
        /// </summary>
        /// <param name="enrollmentGroupId">The Id of the enrollmentGroup.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The retrieved enrollment group.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to retrieve the enrollment group information for the provided <paramref name="enrollmentGroupId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<EnrollmentGroup> GetAsync(string enrollmentGroupId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));

            cancellationToken.ThrowIfCancellationRequested();

            ContractApiResponse contractApiResponse = await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Get,
                    GetEnrollmentUri(enrollmentGroupId),
                    null,
                    null,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<EnrollmentGroup>(contractApiResponse.Body);
        }

        /// <summary>
        /// Delete an enrollment group.
        /// </summary>
        /// <param name="enrollmentGroupId">The string that identifies the enrollmentGroup. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to delete the enrollment group information for the provided <paramref name="enrollmentGroupId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task DeleteAsync(string enrollmentGroupId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));

            cancellationToken.ThrowIfCancellationRequested();

            await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Delete,
                    GetEnrollmentUri(enrollmentGroupId),
                    null,
                    null,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Delete the enrollment group information.
        /// </summary>
        /// <param name="enrollmentGroup">The <see cref="EnrollmentGroup"/> that identifies the enrollmentGroup. It cannot be null.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroup"/> is null.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to delete the enrollment group information for the provided <paramref name="enrollmentGroup"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task DeleteAsync(EnrollmentGroup enrollmentGroup, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(enrollmentGroup, nameof(enrollmentGroup));

            cancellationToken.ThrowIfCancellationRequested();

            await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Delete,
                    GetEnrollmentUri(enrollmentGroup.EnrollmentGroupId),
                    null,
                    null,
                    enrollmentGroup.ETag,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Create, update or delete a set of enrollment groups.
        /// </summary>
        /// <remarks>
        /// This API provide the means to do a single operation over multiple enrollment groups. A valid operation
        /// is determined by <see cref="BulkOperationMode"/>, and can be 'create', 'update', 'updateIfMatchETag', or 'delete'.
        /// </remarks>
        /// <param name="bulkOperationMode">The <see cref="BulkOperationMode"/> that defines the single operation to do over the enrollment group. It cannot be null. </param>
        /// <param name="enrollmentGroups">The collection of <see cref="EnrollmentGroup"/> that contains the description of each enrollment group. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An object with the result of each operation.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroups"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroups"/> is an empty collection.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the client failed to send the request or service was not able to execute the bulk operation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<BulkEnrollmentOperationResult> RunBulkOperationAsync(
            BulkOperationMode bulkOperationMode,
            IEnumerable<EnrollmentGroup> enrollmentGroups,
            CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrEmpty(enrollmentGroups, nameof(enrollmentGroups));

            cancellationToken.ThrowIfCancellationRequested();

            var bulkOperation = new EnrollmentGroupBulkOperation
            {
                Mode = bulkOperationMode,
                Enrollments = enrollmentGroups,
            };

            ContractApiResponse contractApiResponse = await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Post,
                    GetEnrollmentUri(),
                    null,
                    JsonConvert.SerializeObject(bulkOperation),
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<BulkEnrollmentOperationResult>(contractApiResponse.Body);
        }

        /// <summary>
        /// Create an enrollment group query.
        /// </summary>
        /// <remarks>
        /// The service expects a SQL-like query such as
        ///
        /// <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the query will return a page of results. The maximum number of
        /// items per page can be specified by the pageSize parameter.
        /// </remarks>
        /// <param name="query">The <see cref="QuerySpecification"/> with the SQL query. It cannot be null.</param>
        /// <param name="pageSize">The int with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The iterable set of query results.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="query"/> is empty or white space.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the provided <paramref name="pageSize"/> value is less than zero.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Query CreateQuery(string query, int pageSize = 0, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));
            return new Query(_serviceConnectionString, ServiceName, query, _contractApiHttp, pageSize, cancellationToken);
        }

        /// <summary>
        /// Get an enrollment group's attestation information.
        /// </summary>
        /// <param name="enrollmentGroupId">The Id of the enrollment group.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The attestation mechanism of the enrollment group.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to retrieve the enrollment group attestation information for the provided <paramref name="enrollmentGroupId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<AttestationMechanism> GetAttestationAsync(string enrollmentGroupId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));

            cancellationToken.ThrowIfCancellationRequested();

            ContractApiResponse contractApiResponse = await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Post,
                    GetEnrollmentAttestationUri(enrollmentGroupId),
                    null,
                    null,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<AttestationMechanism>(contractApiResponse.Body);
        }

        private static Uri GetEnrollmentUri(string enrollmentGroupId = "")
        {
            if (string.IsNullOrWhiteSpace(enrollmentGroupId))
            {
                return new Uri(string.Format(CultureInfo.InvariantCulture, EnrollmentsUriFormat, ServiceName, SdkUtils.ApiVersionQueryString), UriKind.Relative);
            }

            enrollmentGroupId = WebUtility.UrlEncode(enrollmentGroupId);
            return new Uri(string.Format(CultureInfo.InvariantCulture, EnrollmentIdUriFormat, ServiceName, enrollmentGroupId, SdkUtils.ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetEnrollmentAttestationUri(string enrollmentGroupId)
        {
            enrollmentGroupId = WebUtility.UrlEncode(enrollmentGroupId);
            return new Uri(
                string.Format(CultureInfo.InvariantCulture, EnrollmentAttestationUriFormat, ServiceName, enrollmentGroupId, EnrollmentAttestationName, SdkUtils.ApiVersionQueryString),
                UriKind.Relative);
        }
    }
}
