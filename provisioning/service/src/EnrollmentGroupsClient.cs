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
    /// Subclient of <see cref="ProvisioningServiceClient"/> that handles all enrollment group operations including
    /// getting/creating/setting/deleting enrollment groups, querying enrollment groups, and getting attestation mechanisms
    /// for particular enrollment groups.
    /// </summary>
    public class EnrollmentGroupsClient
    {
        private const string EnrollmentsUriFormat = "enrollmentGroups";
        private const string EnrollmentIdUriFormat = "enrollmentGroups/{0}";
        private const string EnrollmentAttestationUriFormat = "enrollmentGroups/{0}/attestationmechanism";
        private const string EnrollmentGroupQueryUriFormat = "enrollmentGroups/query";

        private readonly ContractApiHttp _contractApiHttp;
        private readonly RetryHandler _internalRetryHandler;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected EnrollmentGroupsClient()
        {
        }

        internal EnrollmentGroupsClient(ContractApiHttp contractApiHttp, RetryHandler retryHandler)
        {
            _contractApiHttp = contractApiHttp;
            _internalRetryHandler = retryHandler;
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
        /// <exception cref="ProvisioningServiceException">
        /// If the service was not able to create or update the enrollment.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<EnrollmentGroup> CreateOrUpdateAsync(EnrollmentGroup enrollmentGroup, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(enrollmentGroup, nameof(enrollmentGroup));

            cancellationToken.ThrowIfCancellationRequested();

            HttpResponseMessage response = null;

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        response = await _contractApiHttp
                            .RequestAsync(
                                HttpMethod.Put,
                                GetEnrollmentUri(enrollmentGroup.Id),
                                null,
                                JsonConvert.SerializeObject(enrollmentGroup),
                                enrollmentGroup.ETag,
                                cancellationToken)
                            .ConfigureAwait(false);
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            string payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<EnrollmentGroup>(payload);
        }

        /// <summary>
        /// Get an enrollment group by its Id.
        /// </summary>
        /// <param name="enrollmentGroupId">The Id of the enrollmentGroup.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The retrieved enrollment group.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        /// <exception cref="ProvisioningServiceException">
        /// If the service was not able to retrieve the enrollment group information for the provided <paramref name="enrollmentGroupId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<EnrollmentGroup> GetAsync(string enrollmentGroupId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));

            cancellationToken.ThrowIfCancellationRequested();

            HttpResponseMessage response = null;

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        response = await _contractApiHttp
                            .RequestAsync(
                                HttpMethod.Get,
                                GetEnrollmentUri(enrollmentGroupId),
                                null,
                                null,
                                new ETag(),
                                cancellationToken)
                            .ConfigureAwait(false);
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            string payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<EnrollmentGroup>(payload);
        }

        /// <summary>
        /// Delete an enrollment group.
        /// </summary>
        /// <param name="enrollmentGroupId">The string that identifies the enrollmentGroup. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        /// <exception cref="ProvisioningServiceException">
        /// If the service was not able to delete the enrollment group information for the provided <paramref name="enrollmentGroupId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task DeleteAsync(string enrollmentGroupId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));

            cancellationToken.ThrowIfCancellationRequested();

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        await _contractApiHttp
                            .RequestAsync(
                                HttpMethod.Delete,
                                GetEnrollmentUri(enrollmentGroupId),
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
        /// Delete the enrollment group information.
        /// </summary>
        /// <param name="enrollmentGroup">The <see cref="EnrollmentGroup"/> that identifies the enrollmentGroup. It cannot be null.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroup"/> is null.</exception>
        /// <exception cref="ProvisioningServiceException">
        /// If the service was not able to delete the enrollment group information for the provided <paramref name="enrollmentGroup"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task DeleteAsync(EnrollmentGroup enrollmentGroup, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(enrollmentGroup, nameof(enrollmentGroup));

            cancellationToken.ThrowIfCancellationRequested();

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        await _contractApiHttp
                            .RequestAsync(
                                HttpMethod.Delete,
                                GetEnrollmentUri(enrollmentGroup.Id),
                                null,
                                null,
                                enrollmentGroup.ETag,
                                cancellationToken)
                            .ConfigureAwait(false);
                    },
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
        /// <exception cref="ProvisioningServiceException">
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
                Enrollments = enrollmentGroups.ToList(),
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
        /// Create an enrollment group query.
        /// </summary>
        /// <remarks>
        /// The service expects a SQL-like query such as
        ///
        /// <c>"SELECT * FROM enrollmentGroups"</c>.
        /// </remarks>
        /// <param name="query">The <see cref="QuerySpecification"/> with the SQL query. It cannot be null.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The iterable set of query results.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="query"/> is empty or white space.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        /// <example>
        /// Iterate over enrollment groups:
        /// <code language="csharp">
        /// AsyncPageable&lt;EnrollmentGroup&gt; enrollmentGroupsQuery = dpsServiceClient.EnrollmentGroups.CreateQuery("SELECT * FROM enrollmentGroups");
        /// await foreach (EnrollmentGroup queriedEnrollment in enrollmentGroupsQuery)
        /// {
        ///     Console.WriteLine(queriedEnrollment.Id);
        /// }
        /// </code>
        /// Iterate over pages of enrollment groups:
        /// <code language="csharp">
        /// IAsyncEnumerable&lt;Page&lt;EnrollmentGroup&gt;&gt; enrollmentGroupsQuery = dpsServiceClient.EnrollmentGroups.CreateQuery("SELECT * FROM enrollmentGroups").AsPages();
        /// await foreach (Page&lt;EnrollmentGroup&gt; queriedEnrollmentPage in enrollmentGroupsQuery)
        /// {
        ///     foreach (EnrollmentGroup queriedEnrollment in queriedEnrollmentPage.Values)
        ///     {
        ///         Console.WriteLine(queriedEnrollment.Id);
        ///     }
        ///     
        ///     // Note that this is disposed for you while iterating item-by-item, but not when
        ///     // iterating page-by-page. That is why this sample has to manually call dispose
        ///     // on the response object here.
        ///     queriedEnrollmentPage.GetRawResponse().Dispose();
        /// }
        /// </code>
        /// </example>
        public AsyncPageable<EnrollmentGroup> CreateQuery(string query, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, "Creating query.", nameof(CreateQuery));

            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                async Task<Page<EnrollmentGroup>> NextPageFunc(string continuationToken, int? pageSizeHint)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await QueryBuilder
                        .BuildAndSendRequestAsync<EnrollmentGroup>(
                            _contractApiHttp,
                            _internalRetryHandler,
                            query,
                            GetEnrollmentGroupQueryUri(),
                            continuationToken,
                            pageSizeHint,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                async Task<Page<EnrollmentGroup>> FirstPageFunc(int? pageSizeHint)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await QueryBuilder
                        .BuildAndSendRequestAsync<EnrollmentGroup>(
                            _contractApiHttp,
                            _internalRetryHandler,
                            query,
                            GetEnrollmentGroupQueryUri(),
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
        /// Get an enrollment group's attestation information.
        /// </summary>
        /// <param name="enrollmentGroupId">The Id of the enrollment group.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The attestation mechanism of the enrollment group.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        /// <exception cref="ProvisioningServiceException">
        /// If the service was not able to retrieve the enrollment group attestation information for the provided <paramref name="enrollmentGroupId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<AttestationMechanism> GetAttestationAsync(string enrollmentGroupId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));

            cancellationToken.ThrowIfCancellationRequested();

            HttpResponseMessage response = null;

            await _internalRetryHandler
                .RunWithRetryAsync(
                    async () =>
                    {
                        response = await _contractApiHttp
                            .RequestAsync(
                                HttpMethod.Post,
                                GetEnrollmentAttestationUri(enrollmentGroupId),
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

        private static Uri GetEnrollmentUri(string enrollmentGroupId = "")
        {
            if (string.IsNullOrWhiteSpace(enrollmentGroupId))
            {
                return new Uri(EnrollmentsUriFormat, UriKind.Relative);
            }

            enrollmentGroupId = WebUtility.UrlEncode(enrollmentGroupId);
            return new Uri(string.Format(CultureInfo.InvariantCulture, EnrollmentIdUriFormat, enrollmentGroupId), UriKind.Relative);
        }

        private static Uri GetEnrollmentAttestationUri(string enrollmentGroupId)
        {
            enrollmentGroupId = WebUtility.UrlEncode(enrollmentGroupId);
            return new Uri(
                string.Format(CultureInfo.InvariantCulture, EnrollmentAttestationUriFormat, enrollmentGroupId),
                UriKind.Relative);
        }

        private static Uri GetEnrollmentGroupQueryUri()
        {
            return new Uri(EnrollmentGroupQueryUriFormat, UriKind.Relative);
        }
    }
}
