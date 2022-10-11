// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http;
using System.Net;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Subclient of <see cref="ProvisioningServiceClient"/> that handles all individual enrollment operations including
    /// getting/creating/setting/deleting individual enrollments, querying individual enrollments, and getting attestation mechanisms
    /// for particular individual enrollments.
    /// </summary>
    public class IndividualEnrollmentsClient
    {
        private const string ServiceName = "enrollments";
        private const string EnrollmentIdUriFormat = "{0}/{1}?{2}";
        private const string EnrollmentAttestationName = "attestationmechanism";
        private const string EnrollmentUriFormat = "{0}?{1}";
        private const string EnrollmentAttestationUriFormat = "{0}/{1}/{2}?{3}";

        private readonly IContractApiHttp _contractApiHttp;
        private readonly ServiceConnectionString _serviceConnectionString;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected IndividualEnrollmentsClient()
        {
        }

        internal IndividualEnrollmentsClient(ServiceConnectionString serviceConnectionString, IContractApiHttp contractApiHttp)
        {
            _serviceConnectionString = serviceConnectionString;
            _contractApiHttp = contractApiHttp;
        }

        /// <summary>
        /// Creates or updates an individual enrollment.
        /// </summary>
        /// <param name="individualEnrollment">The individual enrollment object.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An individual enrollment</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="individualEnrollment"/> is null.</exception>
        /// <exception cref="DeviceProvisioningServiceException">If the service was not able to create or update the enrollment.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<IndividualEnrollment> CreateOrUpdateAsync(IndividualEnrollment individualEnrollment, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(individualEnrollment, nameof(individualEnrollment));

            ContractApiResponse contractApiResponse = await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Put,
                    GetEnrollmentUri(individualEnrollment.RegistrationId),
                    null,
                    JsonConvert.SerializeObject(individualEnrollment),
                    individualEnrollment.ETag,
                    cancellationToken)
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<IndividualEnrollment>(contractApiResponse.Body);
        }

        /// <summary>
        /// Gets the individual enrollment object.
        /// </summary>
        /// <param name="registrationId">The registration Id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The enrollment.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="registrationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="registrationId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">If the service was not able to get the enrollment.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<IndividualEnrollment> GetAsync(string registrationId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));

            ContractApiResponse contractApiResponse = await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Get,
                    GetEnrollmentUri(registrationId),
                    null,
                    null,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<IndividualEnrollment>(contractApiResponse.Body);
        }

        /// <summary>
        /// Delete the individual enrollment information.
        /// </summary>
        /// <remarks>
        /// This method will remove the individualEnrollment from the Device Provisioning Service using the
        /// provided registrationId. It will delete the enrollment regardless the eTag.
        ///
        /// Note that delete the enrollment will not remove the Device itself from the IotHub.
        /// </remarks>
        /// <param name="registrationId">The string that identifies the individualEnrollment. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="registrationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="registrationId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the client failed to send the request or service was not able to execute the operation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task DeleteAsync(string registrationId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));

            return DeleteAsync(new IndividualEnrollment(registrationId, null), cancellationToken);
        }

        /// <summary>
        /// Deletes an individual enrollment.
        /// </summary>
        /// <param name="individualEnrollment">The individual enrollment.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="individualEnrollment"/> is null.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the client failed to send the request or service was not able to execute the operation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task DeleteAsync(IndividualEnrollment individualEnrollment, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(individualEnrollment, nameof(individualEnrollment));

            await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Delete,
                    GetEnrollmentUri(individualEnrollment.RegistrationId),
                    null,
                    null,
                    individualEnrollment.ETag,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Create, update or delete a set of individual Device Enrollments.
        /// </summary>
        /// <remarks>
        /// This API provide the means to do a single operation over multiple individualEnrollments. A valid operation
        /// is determined by <see cref="BulkOperationMode"/>, and can be 'create', 'update', 'updateIfMatchETag', or 'delete'.
        /// </remarks>
        /// <param name="bulkOperationMode">The <see cref="BulkOperationMode"/> that defines the single operation to do over the individualEnrollments. It cannot be null.</param>
        /// <param name="individualEnrollments">The collection of <see cref="IndividualEnrollment"/> that contains the description of each individualEnrollment. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="BulkEnrollmentOperationResult"/> object with the result of operation for each enrollment.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="individualEnrollments"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="individualEnrollments"/> is an empty collection.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the client failed to send the request or service was not able to execute the bulk operation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<BulkEnrollmentOperationResult> RunBulkEnrollmentOperationAsync(
            BulkOperationMode bulkOperationMode,
            IEnumerable<IndividualEnrollment> individualEnrollments,
            CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrEmpty(individualEnrollments, nameof(individualEnrollments));
            ContractApiResponse contractApiResponse = await _contractApiHttp
                            .RequestAsync(
                                HttpMethod.Post,
                                GetEnrollmentUri(),
                                null,
                                BulkEnrollmentOperation.ToJson(bulkOperationMode, individualEnrollments),
                                null,
                                cancellationToken)
                            .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<BulkEnrollmentOperationResult>(contractApiResponse.Body);
        }

        /// <summary>
        /// Factory to create a individualEnrollment query.
        /// </summary>
        /// <remarks>
        /// This method will create a new individualEnrollment query for Device Provisioning Service and return it
        /// as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        /// <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the Query will return a List of objects correspondent to the query result. The maximum
        /// number of items per iteration can be specified by the pageSize.
        /// </remarks>
        /// <param name="query">The SQL query. It cannot be null.</param>
        /// <param name="pageSize">The int with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="query"/> is empty or white space.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the provided <paramref name="pageSize"/> is less than zero.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Query CreateQuery(string query, int pageSize = 0, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));

            return new Query(_serviceConnectionString, ServiceName, query, _contractApiHttp, pageSize, cancellationToken);
        }

        /// <summary>
        /// Retrieve the attestation information for an individual enrollment.
        /// </summary>
        /// <param name="registrationId">The registration Id of the individual enrollment to retrieve the attestation information of. This may not be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="AttestationMechanism"/> of the individual enrollment associated with the provided <paramref name="registrationId"/>.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="registrationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="registrationId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to retrieve the individual enrollment attestation information for the provided <paramref name="registrationId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<AttestationMechanism> GetAttestationAsync(string registrationId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));

            ContractApiResponse contractApiResponse = await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Post,
                    GetEnrollmentAttestationUri(registrationId),
                    null,
                    null,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<AttestationMechanism>(contractApiResponse.Body);
        }

        private static Uri GetEnrollmentUri(string registrationId)
        {
            registrationId = WebUtility.UrlEncode(registrationId);
            return new Uri(string.Format(CultureInfo.InvariantCulture, EnrollmentIdUriFormat, ServiceName, registrationId, SdkUtils.ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetEnrollmentUri()
        {
            return new Uri(string.Format(CultureInfo.InvariantCulture, EnrollmentUriFormat, ServiceName, SdkUtils.ApiVersionQueryString), UriKind.Relative);
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
