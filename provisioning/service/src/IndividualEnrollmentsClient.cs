// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Subclient of <see cref="ProvisioningServiceClient"/> that handles all individual enrollment operations including
    /// getting/creating/setting/deleting individual enrollments, querying individual enrollments, and getting attestation mechanisms
    /// for particular individual enrollments.
    /// </summary>
    public class IndividualEnrollmentsClient
    {
        private const string EnrollmentIdUriFormat = "enrollments/{0}";
        private const string EnrollmentUriFormat = "enrollments";
        private const string EnrollmentAttestationUriFormat = "enrollments/{0}/attestationmechanism";
        private const string EnrollmentQueryUriFormat = "enrollments/query";

        private readonly ContractApiHttp _contractApiHttp;
        private readonly RetryHandler _internalRetryHandler;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected IndividualEnrollmentsClient()
        {
        }

        internal IndividualEnrollmentsClient(ContractApiHttp contractApiHttp, RetryHandler retryHandler)
        {
            _contractApiHttp = contractApiHttp;
            _internalRetryHandler = retryHandler;
        }

        /// <summary>
        /// Create or update an individual enrollment.
        /// </summary>
        /// <param name="individualEnrollment">The individual enrollment to create or update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created or updated individual enrollment.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="individualEnrollment"/> is null.</exception>
        /// <exception cref="ProvisioningServiceException">If the service was not able to create or update the enrollment.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<IndividualEnrollment> CreateOrUpdateAsync(IndividualEnrollment individualEnrollment, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(individualEnrollment, nameof(individualEnrollment));

            cancellationToken.ThrowIfCancellationRequested();

            HttpResponseMessage response = null;

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        response = await _contractApiHttp
                            .RequestAsync(
                                HttpMethod.Put,
                                GetEnrollmentUri(individualEnrollment.RegistrationId),
                                null,
                                JsonConvert.SerializeObject(individualEnrollment),
                                individualEnrollment.ETag,
                                cancellationToken)
                            .ConfigureAwait(false);
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            string payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<IndividualEnrollment>(payload);
        }

        /// <summary>
        /// Gets an individual enrollment by Id.
        /// </summary>
        /// <param name="registrationId">The registration Id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The retrieved enrollment.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="registrationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="registrationId"/> is empty or white space.</exception>
        /// <exception cref="ProvisioningServiceException">If the service was not able to get the enrollment.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<IndividualEnrollment> GetAsync(string registrationId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));

            cancellationToken.ThrowIfCancellationRequested();

            HttpResponseMessage response = null;

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        response = await _contractApiHttp
                            .RequestAsync(
                                HttpMethod.Get,
                                GetEnrollmentUri(registrationId),
                                null,
                                null,
                                new ETag(),
                                cancellationToken)
                            .ConfigureAwait(false);
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            string payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<IndividualEnrollment>(payload);
        }

        /// <summary>
        /// Delete an individual enrollment.
        /// </summary>
        /// <param name="registrationId">The Id of the individual enrollment to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="registrationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="registrationId"/> is empty or white space.</exception>
        /// <exception cref="ProvisioningServiceException">
        /// If the client failed to send the request or service was not able to execute the operation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task DeleteAsync(string registrationId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));

            cancellationToken.ThrowIfCancellationRequested();

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        await _contractApiHttp
                            .RequestAsync(
                                HttpMethod.Delete,
                                GetEnrollmentUri(registrationId),
                                null,
                                null,
                                new ETag(),
                                cancellationToken)
                            .ConfigureAwait(false);
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Delete an individual enrollment.
        /// </summary>
        /// <param name="individualEnrollment">The individual enrollment to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="individualEnrollment"/> is null.</exception>
        /// <exception cref="ProvisioningServiceException">
        /// If the client failed to send the request or service was not able to execute the operation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task DeleteAsync(IndividualEnrollment individualEnrollment, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(individualEnrollment, nameof(individualEnrollment));

            cancellationToken.ThrowIfCancellationRequested();

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        await _contractApiHttp
                            .RequestAsync(
                                HttpMethod.Delete,
                                GetEnrollmentUri(individualEnrollment.RegistrationId),
                                null,
                                null,
                                individualEnrollment.ETag,
                                cancellationToken)
                            .ConfigureAwait(false);
                    },
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
        /// <exception cref="ProvisioningServiceException">
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
                Enrollments = individualEnrollments.ToList(),
            };

            HttpResponseMessage response = null;

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        response = await _contractApiHttp
                            .RequestAsync(
                                HttpMethod.Post,
                                GetEnrollmentUri(),
                                null,
                                JsonConvert.SerializeObject(bulkOperation),
                                new ETag(),
                                cancellationToken)
                            .ConfigureAwait(false);
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            string payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<BulkEnrollmentOperationResult>(payload);
        }

        /// <summary>
        /// Create an individual enrollment query.
        /// </summary>
        /// <remarks>
        /// The service expects a SQL-like query such as
        ///
        /// <c>"SELECT * FROM enrollments"</c>.
        /// </remarks>
        /// <param name="query">The SQL query. It cannot be null.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The iterable set of query results.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="query"/> is empty or white space.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        /// <example>
        /// Iterate over individual enrollments:
        /// <code language="csharp">
        /// AsyncPageable&lt;IndividualEnrollment&gt; individualEnrollmentsQuery = dpsServiceClient.IndividualEnrollments.CreateQuery&lt;EnrollmentGroup&gt;("SELECT * FROM enrollments");
        /// await foreach (IndividualEnrollment queriedEnrollment in individualEnrollmentsQuery)
        /// {
        ///     Console.WriteLine(queriedEnrollment);
        /// }
        /// </code>
        /// </example>
        public AsyncPageable<IndividualEnrollment> CreateQuery(string query, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Creating query.", nameof(CreateQuery));

            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                async Task<Page<IndividualEnrollment>> NextPageFunc(string continuationToken, int? pageSizeHint)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await QueryBuilder
                        .BuildAndSendRequestAsync<IndividualEnrollment>(
                            _contractApiHttp,
                            _internalRetryHandler,
                            query,
                            GetEnrollmentQueryUri(),
                            continuationToken,
                            pageSizeHint,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                async Task<Page<IndividualEnrollment>> FirstPageFunc(int? pageSizeHint)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await QueryBuilder
                        .BuildAndSendRequestAsync<IndividualEnrollment>(
                            _contractApiHttp,
                            _internalRetryHandler,
                            query,
                            GetEnrollmentQueryUri(),
                            null,
                            pageSizeHint,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                return PageableHelpers.CreateAsyncEnumerable(FirstPageFunc, NextPageFunc, null);
            }
            catch (Exception ex) when (Logging.IsEnabled)
            {
                Logging.Error(this, $"Creating query threw an exception: {ex}", nameof(CreateQuery));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, "Creating query.", nameof(CreateQuery));
            }
        }

        /// <summary>
        /// Get an individual enrollment's attestation information.
        /// </summary>
        /// <param name="registrationId">The registration Id of the individual enrollment.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="AttestationMechanism"/> of the individual enrollment associated with the provided <paramref name="registrationId"/>.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="registrationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="registrationId"/> is empty or white space.</exception>
        /// <exception cref="ProvisioningServiceException">
        /// If the service was not able to retrieve the individual enrollment attestation information for the provided <paramref name="registrationId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<AttestationMechanism> GetAttestationAsync(string registrationId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));

            cancellationToken.ThrowIfCancellationRequested();

            HttpResponseMessage response = null;

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        response = await _contractApiHttp
                            .RequestAsync(
                                HttpMethod.Post,
                                GetEnrollmentAttestationUri(registrationId),
                                null,
                                null,
                                new ETag(),
                                cancellationToken)
                            .ConfigureAwait(false);
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            string payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<AttestationMechanism>(payload);
        }

        private static Uri GetEnrollmentUri(string registrationId)
        {
            registrationId = WebUtility.UrlEncode(registrationId);
            return new Uri(string.Format(CultureInfo.InvariantCulture, EnrollmentIdUriFormat, registrationId), UriKind.Relative);
        }

        private static Uri GetEnrollmentUri()
        {
            return new Uri(EnrollmentUriFormat, UriKind.Relative);
        }

        private static Uri GetEnrollmentAttestationUri(string registrationId)
        {
            registrationId = WebUtility.UrlEncode(registrationId);
            return new Uri(
                string.Format(CultureInfo.InvariantCulture, EnrollmentAttestationUriFormat, registrationId),
                UriKind.Relative);
        }

        private static Uri GetEnrollmentQueryUri()
        {
            return new Uri(EnrollmentQueryUriFormat, UriKind.Relative);
        }
    }
}
