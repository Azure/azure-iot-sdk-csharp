// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.Globalization;
using System.Net;

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
        /// Create or update an enrollment group record.
        /// </summary>
        /// <remarks>
        /// This API creates a new enrollment group or update a existed one. All enrollment group in the Device
        /// Provisioning Service contains a unique identifier called enrollmentGroupId. If this API is called
        /// with an enrollmentGroupId that already exists, it will replace the existed enrollment group information
        /// by the new one. On the other hand, if the enrollmentGroupId does not exit, it will be created.
        ///
        /// To use the Device Provisioning Service API, you must include the follow package on your application.
        /// <code>
        /// // Include the following using to use the Device Provisioning Service APIs.
        /// using Microsoft.Azure.Devices.Provisioning.Service;
        /// </code>
        /// </remarks>
        /// <param name="enrollmentGroup">The <see cref="EnrollmentGroup"/> object that describes the individualEnrollment that will be created of updated.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An <see cref="EnrollmentGroup"/> object with the result of the create or update requested.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroup"/> is null.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to create or update the enrollment.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<EnrollmentGroup> CreateOrUpdateAsync(EnrollmentGroup enrollmentGroup, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(enrollmentGroup, nameof(enrollmentGroup));

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
        /// Retrieve the enrollment group information.
        /// </summary>
        /// <remarks>
        /// This method will return the enrollment group information for the provided enrollmentGroupId. It will retrieve
        /// the correspondent enrollment group from the Device Provisioning Service, and return it in the
        /// <see cref="EnrollmentGroup"/> object.
        /// </remarks>
        /// <param name="enrollmentGroupId">The string that identifies the enrollmentGroup. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="EnrollmentGroup"/> with the content of the enrollment group in the Provisioning Device Service.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to retrieve the enrollment group information for the provided <paramref name="enrollmentGroupId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<EnrollmentGroup> GetAsync(string enrollmentGroupId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));

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
        /// Delete the enrollment group information.
        /// </summary>
        /// <remarks>
        /// This method will remove the enrollment group from the Device Provisioning Service using the
        /// provided enrollmentGroupId. It will delete the enrollment group regardless the eTag.
        ///
        /// Note that delete the enrollment group will not remove the Devices itself from the IotHub.
        /// </remarks>
        /// <param name="enrollmentGroupId">The string that identifies the enrollmentGroup. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to delete the enrollment group information for the provided <paramref name="enrollmentGroupId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task DeleteAsync(string enrollmentGroupId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));

            return DeleteAsync(new EnrollmentGroup(enrollmentGroupId, null), cancellationToken);
        }

        /// <summary>
        /// Delete the enrollment group information.
        /// </summary>
        /// <remarks>
        /// This method will remove the enrollment group from the Device Provisioning Service using the
        /// provided <see cref="EnrollmentGroup"/> information. The Device Provisioning Service will care about the
        /// enrollmentGroupId and the eTag on the enrollmentGroup. If you want to delete the enrollment regardless the
        /// eTag, you can set the eTag="*" into the enrollmentGroup, or use the <see cref="DeleteAsync(string, CancellationToken)"/>.
        /// passing only the enrollmentGroupId.
        ///
        /// Note that delete the enrollment group will not remove the Devices itself from the IotHub.
        /// </remarks>
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
        /// Factory to create an enrollment group query.
        /// </summary>
        /// <remarks>
        /// This method will create a new enrollment group query on Device Provisioning Service and return it as
        /// a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        /// <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the Query will return a List of objects correspondent to the query result. The maximum
        /// number of items per iteration can be specified by the pageSize.
        /// </remarks>
        /// <param name="query">The <see cref="QuerySpecification"/> with the SQL query. It cannot be null.</param>
        /// <param name="pageSize">The int with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
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
        /// Retrieve the enrollment group attestation information.
        /// </summary>
        /// <param name="enrollmentGroupId">The <c>string</c> that identifies the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="AttestationMechanism"/> associated with the provided <paramref name="enrollmentGroupId"/>.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to retrieve the enrollment group attestation information for the provided <paramref name="enrollmentGroupId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public async Task<AttestationMechanism> GetAttestationAsync(string enrollmentGroupId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));

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

        private static Uri GetEnrollmentUri(string enrollmentGroupId)
        {
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
