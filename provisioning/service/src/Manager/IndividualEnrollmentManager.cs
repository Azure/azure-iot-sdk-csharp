// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Service.Auth;
using System.Net.Http;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// IndividualEnrollment Manager.
    /// </summary>
    /// <remarks>
    /// This is the inner class that implements the IndividualEnrollment APIs.
    /// For the public API, please see <see cref="ProvisioningServiceClient"/>.
    /// </remarks>
    internal static class IndividualEnrollmentManager
    {
        private const string ServiceName = "enrollments";
        private const string EnrollmentIdUriFormat = "{0}/{1}?{2}";
        private const string EnrollmentAttestationName = "attestationmechanism";
        private const string EnrollmentUriFormat = "{0}?{1}";
        private const string EnrollmentAttestationUriFormat = "{0}/{1}/{2}?{3}";

        internal static async Task<IndividualEnrollment> CreateOrUpdateAsync(
            IContractApiHttp contractApiHttp,
            IndividualEnrollment individualEnrollment,
            CancellationToken cancellationToken)
        {
            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_001: [The CreateOrUpdateAsync shall throw ArgumentException if the provided individualEnrollment is null.] */
            if (individualEnrollment == null)
            {
                throw new ArgumentNullException(nameof(individualEnrollment));
            }

            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_002: [The CreateOrUpdateAsync shall sent the Put HTTP request to create or update the individualEnrollment.] */
            ContractApiResponse contractApiResponse = await contractApiHttp.RequestAsync(
                HttpMethod.Put,
                GetEnrollmentUri(individualEnrollment.RegistrationId),
                null,
                JsonConvert.SerializeObject(individualEnrollment),
                individualEnrollment.ETag,
                cancellationToken).ConfigureAwait(false);

            if (contractApiResponse.Body == null)
            {
                throw new ProvisioningServiceClientHttpException(contractApiResponse, true);
            }

            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_003: [The CreateOrUpdateAsync shall return an IndividualEnrollment object created from the body of the HTTP response.] */
            return JsonConvert.DeserializeObject<IndividualEnrollment>(contractApiResponse.Body);
        }

        internal static async Task<BulkEnrollmentOperationResult> BulkOperationAsync(
            IContractApiHttp contractApiHttp,
            BulkOperationMode bulkOperationMode,
            IEnumerable<IndividualEnrollment> individualEnrollments,
            CancellationToken cancellationToken)
        {
            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_004: [The BulkOperationAsync shall throw ArgumentException if the provided
                                                    individualEnrollments is null or empty.] */
            if (!(individualEnrollments ?? throw new ArgumentNullException(nameof(individualEnrollments))).Any())
            {
                throw new ArgumentException($"{nameof(individualEnrollments)} cannot be empty");
            }

            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_005: [The BulkOperationAsync shall sent the Put HTTP request to run the bulk operation to the collection of the individualEnrollment.] */
            ContractApiResponse contractApiResponse = await contractApiHttp.RequestAsync(
                HttpMethod.Post,
                GetEnrollmentUri(),
                null,
                BulkEnrollmentOperation.ToJson(bulkOperationMode, individualEnrollments),
                null,
                cancellationToken).ConfigureAwait(false);

            if (contractApiResponse.Body == null)
            {
                throw new ProvisioningServiceClientHttpException(contractApiResponse, true);
            }

            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_006: [The BulkOperationAsync shall return an BulkEnrollmentOperationResult object created from the body of the HTTP response.] */
            return JsonConvert.DeserializeObject<BulkEnrollmentOperationResult>(contractApiResponse.Body);
        }

        internal static async Task<IndividualEnrollment> GetAsync(
            IContractApiHttp contractApiHttp,
            string registrationId,
            CancellationToken cancellationToken)
        {
            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_008: [The GetAsync shall sent the Get HTTP request to get the individualEnrollment information.] */
            ContractApiResponse contractApiResponse = await contractApiHttp.RequestAsync(
                HttpMethod.Get,
                GetEnrollmentUri(registrationId),
                null,
                null,
                null,
                cancellationToken).ConfigureAwait(false);

            if (contractApiResponse.Body == null)
            {
                throw new ProvisioningServiceClientHttpException(contractApiResponse, true);
            }

            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_009: [The GetAsync shall return an IndividualEnrollment object created from the body of the HTTP response.] */
            return JsonConvert.DeserializeObject<IndividualEnrollment>(contractApiResponse.Body);
        }

        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            IndividualEnrollment individualEnrollment,
            CancellationToken cancellationToken)
        {
            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_010: [The DeleteAsync shall throw ArgumentException if the provided individualEnrollment is null.] */
            if (individualEnrollment == null)
            {
                throw new ArgumentNullException(nameof(individualEnrollment));
            }

            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_011: [The DeleteAsync shall sent the Delete HTTP request to remove the individualEnrollment.] */
            await contractApiHttp.RequestAsync(
                HttpMethod.Delete,
                GetEnrollmentUri(individualEnrollment.RegistrationId),
                null,
                null,
                individualEnrollment.ETag,
                cancellationToken).ConfigureAwait(false);
        }

        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            string registrationId,
            CancellationToken cancellationToken,
            string eTag = null)
        {
            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_013: [The DeleteAsync shall sent the Delete HTTP request to remove the individualEnrollment.] */
            await contractApiHttp.RequestAsync(
                HttpMethod.Delete,
                GetEnrollmentUri(registrationId),
                null,
                null,
                eTag,
                cancellationToken).ConfigureAwait(false);
        }

        internal static Query CreateQuery(
            string hostName,
            IAuthorizationHeaderProvider headerProvider,
            QuerySpecification querySpecification,
            HttpTransportSettings httpTransportSettings,
            CancellationToken cancellationToken,
            int pageSize = 0)
        {
            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_014: [The CreateQuery shall throw ArgumentException if the provided querySpecification is null.] */
            if (querySpecification == null)
            {
                throw new ArgumentNullException(nameof(querySpecification));
            }

            if (pageSize < 0)
            {
                throw new ArgumentException($"{nameof(pageSize)} cannot be negative");
            }

            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_015: [The CreateQuery shall return a new Query for IndividualEnrollments.] */
            return new Query(hostName, headerProvider, ServiceName, querySpecification, httpTransportSettings, pageSize, cancellationToken);
        }

        private static Uri GetEnrollmentUri(string registrationId)
        {
            registrationId = WebUtility.UrlEncode(registrationId);
            return new Uri(EnrollmentIdUriFormat.FormatInvariant(ServiceName, registrationId, SDKUtils.ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetEnrollmentUri()
        {
            return new Uri(EnrollmentUriFormat.FormatInvariant(ServiceName, SDKUtils.ApiVersionQueryString), UriKind.Relative);
        }

        internal static async Task<AttestationMechanism> GetEnrollmentAttestationAsync(
             IContractApiHttp contractApiHttp,
             string registrationId,
             CancellationToken cancellationToken)
        {
            ContractApiResponse contractApiResponse = await contractApiHttp.RequestAsync(
                HttpMethod.Post,
                GetEnrollmentAttestationUri(registrationId),
                null,
                null,
                null,
                cancellationToken).ConfigureAwait(false);

            if (contractApiResponse.Body == null)
            {
                throw new ProvisioningServiceClientHttpException(contractApiResponse, true);
            }

            return JsonConvert.DeserializeObject<AttestationMechanism>(contractApiResponse.Body);
        }

        private static Uri GetEnrollmentAttestationUri(string enrollmentGroupId)
        {
            enrollmentGroupId = WebUtility.UrlEncode(enrollmentGroupId);
            return new Uri(EnrollmentAttestationUriFormat.FormatInvariant(ServiceName, enrollmentGroupId, EnrollmentAttestationName, SDKUtils.ApiVersionQueryString), UriKind.Relative);
        }
    }
}
