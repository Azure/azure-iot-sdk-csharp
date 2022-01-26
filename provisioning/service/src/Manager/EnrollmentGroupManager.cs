// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Service.Auth;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal static class EnrollmentGroupManager
    {
        private const string ServiceName = "enrollmentGroups";
        private const string EnrollmentIdUriFormat = "{0}/{1}?{2}";
        private const string EnrollmentAttestationName = "attestationmechanism";
        private const string EnrollmentAttestationUriFormat = "{0}/{1}/{2}?{3}";

        internal static async Task<EnrollmentGroup> CreateOrUpdateAsync(
            IContractApiHttp contractApiHttp,
            EnrollmentGroup enrollmentGroup,
            CancellationToken cancellationToken)
        {
            /* SRS_ENROLLMENT_GROUP_MANAGER_28_001: [The CreateOrUpdateAsync shall throw ArgumentNullException if the provided enrollmentGroup is null.] */
            if (enrollmentGroup == null)
            {
                throw new ArgumentNullException(nameof(enrollmentGroup));
            }

            /* SRS_ENROLLMENT_GROUP_MANAGER_28_002: [The CreateOrUpdateAsync shall sent the Put HTTP request to create or update the enrollmentGroup.] */
            ContractApiResponse contractApiResponse = await contractApiHttp.RequestAsync(
                HttpMethod.Put,
                GetEnrollmentUri(enrollmentGroup.EnrollmentGroupId),
                null,
                JsonConvert.SerializeObject(enrollmentGroup),
                enrollmentGroup.ETag,
                cancellationToken).ConfigureAwait(false);

            if (contractApiResponse.Body == null)
            {
                throw new ProvisioningServiceClientHttpException(contractApiResponse, true);
            }

            /* SRS_ENROLLMENT_GROUP_MANAGER_28_003: [The CreateOrUpdateAsync shall return an enrollmentGroup object created from the body of the HTTP response.] */
            return JsonConvert.DeserializeObject<EnrollmentGroup>(contractApiResponse.Body);
        }

        internal static async Task<EnrollmentGroup> GetAsync(
            IContractApiHttp contractApiHttp,
            string enrollmentGroupId,
            CancellationToken cancellationToken)
        {
            /* SRS_ENROLLMENT_GROUP_MANAGER_28_008: [The GetAsync shall sent the Get HTTP request to get the enrollmentGroup information.] */
            ContractApiResponse contractApiResponse = await contractApiHttp.RequestAsync(
                HttpMethod.Get,
                GetEnrollmentUri(enrollmentGroupId),
                null,
                null,
                null,
                cancellationToken).ConfigureAwait(false);

            if (contractApiResponse.Body == null)
            {
                throw new ProvisioningServiceClientHttpException(contractApiResponse, true);
            }

            /* SRS_ENROLLMENT_GROUP_MANAGER_28_009: [The GetAsync shall return an EnrollmentGroup object created from the body of the HTTP response.] */
            return JsonConvert.DeserializeObject<EnrollmentGroup>(contractApiResponse.Body);
        }

        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            EnrollmentGroup enrollmentGroup,
            CancellationToken cancellationToken)
        {
            /* SRS_ENROLLMENT_GROUP_MANAGER_28_010: [The DeleteAsync shall throw ArgumentNullException if the provided enrollmentGroup is null.] */
            if (enrollmentGroup == null)
            {
                throw new ArgumentNullException(nameof(enrollmentGroup));
            }

            /* SRS_ENROLLMENT_GROUP_MANAGER_28_011: [The DeleteAsync shall sent the Delete HTTP request to remove the enrollmentGroup.] */
            await contractApiHttp.RequestAsync(
                HttpMethod.Delete,
                GetEnrollmentUri(enrollmentGroup.EnrollmentGroupId),
                null,
                null,
                enrollmentGroup.ETag,
                cancellationToken).ConfigureAwait(false);
        }

        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            string enrollmentGroupId,
            CancellationToken cancellationToken,
            string eTag = null)
        {
            /* SRS_ENROLLMENT_GROUP_MANAGER_28_013: [The DeleteAsync shall sent the Delete HTTP request to remove the EnrollmentGroup.] */
            await contractApiHttp.RequestAsync(
                HttpMethod.Delete,
                GetEnrollmentUri(enrollmentGroupId),
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
            /* SRS_ENROLLMENT_GROUP_MANAGER_28_014: [The CreateQuery shall throw ArgumentException if the provided querySpecification is null.] */
            if (querySpecification == null)
            {
                throw new ArgumentNullException(nameof(querySpecification));
            }

            if (pageSize < 0)
            {
                throw new ArgumentException($"{nameof(pageSize)} cannot be negative");
            }

            /* SRS_ENROLLMENT_GROUP_MANAGER_28_015: [The CreateQuery shall return a new Query for EnrollmentGroup.] */

            return new Query(hostName, headerProvider, ServiceName, querySpecification, httpTransportSettings, pageSize, cancellationToken);
        }

        private static Uri GetEnrollmentUri(string enrollmentGroupId)
        {
            enrollmentGroupId = WebUtility.UrlEncode(enrollmentGroupId);
            return new Uri(EnrollmentIdUriFormat.FormatInvariant(ServiceName, enrollmentGroupId, SDKUtils.ApiVersionQueryString), UriKind.Relative);
        }

        internal static async Task<AttestationMechanism> GetEnrollmentAttestationAsync(
                    IContractApiHttp contractApiHttp,
                    string enrollmentGroupId,
                    CancellationToken cancellationToken)
        {
            ContractApiResponse contractApiResponse = await contractApiHttp.RequestAsync(
                HttpMethod.Post,
                GetEnrollmentAttestationUri(enrollmentGroupId),
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
