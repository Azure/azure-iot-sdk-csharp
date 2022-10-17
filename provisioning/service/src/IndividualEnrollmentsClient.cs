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
using Azure;

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
        /// Create or update an individual enrollment.
        /// </summary>
        /// <param name="individualEnrollment">The individual enrollment to create or update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created or updated individual enrollment.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="individualEnrollment"/> is null.</exception>
        /// <exception cref="DeviceProvisioningServiceException">If the service was not able to create or update the enrollment.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<IndividualEnrollment> CreateOrUpdateAsync(IndividualEnrollment individualEnrollment, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(individualEnrollment, nameof(individualEnrollment));

            cancellationToken.ThrowIfCancellationRequested();

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
        /// Gets an individual enrollment by Id.
        /// </summary>
        /// <param name="registrationId">The registration Id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The retrieved enrollment.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="registrationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="registrationId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">If the service was not able to get the enrollment.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<IndividualEnrollment> GetAsync(string registrationId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));

            cancellationToken.ThrowIfCancellationRequested();

            ContractApiResponse contractApiResponse = await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Get,
                    GetEnrollmentUri(registrationId),
                    null,
                    null,
                    new ETag(),
                    cancellationToken)
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<IndividualEnrollment>(contractApiResponse.Body);
        }

        /// <summary>
        /// Delete an individual enrollment.
        /// </summary>
        /// <param name="registrationId">The Id of the individual enrollment to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="registrationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="registrationId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the client failed to send the request or service was not able to execute the operation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task DeleteAsync(string registrationId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));

            cancellationToken.ThrowIfCancellationRequested();

            await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Delete,
                    GetEnrollmentUri(registrationId),
                    null,
                    null,
                    new ETag(),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Delete an individual enrollment.
        /// </summary>
        /// <param name="individualEnrollment">The individual enrollment to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="individualEnrollment"/> is null.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the client failed to send the request or service was not able to execute the operation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task DeleteAsync(IndividualEnrollment individualEnrollment, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(individualEnrollment, nameof(individualEnrollment));

            cancellationToken.ThrowIfCancellationRequested();

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
        /// Create, update or delete a set of individual enrollment groups.
        /// </summary>
        /// <remarks>
        /// This API provide the means to do a single operation over multiple individualEnrollments. A valid operation
        /// is determined by <see cref="BulkOperationMode"/>, and can be 'create', 'update', 'updateIfMatchETag', or 'delete'.
        /// </remarks>
        /// <param name="bulkOperationMode">The <see cref="BulkOperationMode"/> that defines the single operation to do over the individualEnrollments. It cannot be null.</param>
        /// <param name="individualEnrollments">The collection of <see cref="IndividualEnrollment"/> that contains the description of each individualEnrollment. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An object with the result of each operation.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="individualEnrollments"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="individualEnrollments"/> is an empty collection.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the client failed to send the request or service was not able to execute the bulk operation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<BulkEnrollmentOperationResult> RunBulkOperationAsync(
            BulkOperationMode bulkOperationMode,
            IEnumerable<IndividualEnrollment> individualEnrollments,
            CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrEmpty(individualEnrollments, nameof(individualEnrollments));

            cancellationToken.ThrowIfCancellationRequested();

            var bulkOperation = new IndividualEnrollmentBulkOperation
            {
                Mode = bulkOperationMode,
                Enrollments = individualEnrollments,
            };

            ContractApiResponse contractApiResponse = await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Post,
                    GetEnrollmentUri(),
                    null,
                    JsonConvert.SerializeObject(bulkOperation),
                    new ETag(),
                    cancellationToken)
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<BulkEnrollmentOperationResult>(contractApiResponse.Body);
        }

        /// <summary>
        /// Create an individual enrollment query.
        /// </summary>
        /// <remarks>
        /// The service expects a SQL-like query such as
        ///
        /// <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the query will return a page of results. The maximum number of
        /// items per page can be specified by the pageSize parameter.
        /// </remarks>
        /// <param name="query">The SQL query. It cannot be null.</param>
        /// <param name="pageSize">The int with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The iterable set of query results.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// If the provided <paramref name="query"/> is empty or white space, or <paramref name="pageSize"/> value is less than zero.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Query CreateQuery(string query, int pageSize = 0, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));

            return new Query(_serviceConnectionString, ServiceName, query, _contractApiHttp, pageSize, cancellationToken);
        }

        /// <summary>
        /// Get an individual enrollment's attestation information.
        /// </summary>
        /// <param name="registrationId">The registration Id of the individual enrollment.</param>
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

            cancellationToken.ThrowIfCancellationRequested();

            ContractApiResponse contractApiResponse = await _contractApiHttp
                .RequestAsync(
                    HttpMethod.Post,
                    GetEnrollmentAttestationUri(registrationId),
                    null,
                    null,
                    new ETag(),
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
