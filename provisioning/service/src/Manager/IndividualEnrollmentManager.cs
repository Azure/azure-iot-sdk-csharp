// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Individual enrollment manager.
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
            ContractApiResponse contractApiResponse = await contractApiHttp
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

        internal static async Task<BulkEnrollmentOperationResult> BulkOperationAsync(
            IContractApiHttp contractApiHttp,
            BulkOperationMode bulkOperationMode,
            IEnumerable<IndividualEnrollment> individualEnrollments,
            CancellationToken cancellationToken)
        {
            Debug.Assert(individualEnrollments != null);
            Debug.Assert(individualEnrollments.Any(), $"{nameof(individualEnrollments)} cannot be empty.");

            ContractApiResponse contractApiResponse = await contractApiHttp
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

        internal static async Task<IndividualEnrollment> GetAsync(
            IContractApiHttp contractApiHttp,
            string registrationId,
            CancellationToken cancellationToken)
        {
            ContractApiResponse contractApiResponse = await contractApiHttp
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

        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            IndividualEnrollment individualEnrollment,
            CancellationToken cancellationToken)
        {
            await contractApiHttp
                .RequestAsync(
                    HttpMethod.Delete,
                    GetEnrollmentUri(individualEnrollment.RegistrationId),
                    null,
                    null,
                    individualEnrollment.ETag,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            string registrationId,
            CancellationToken cancellationToken,
            string eTag = null)
        {
            await contractApiHttp
                .RequestAsync(
                    HttpMethod.Delete,
                    GetEnrollmentUri(registrationId),
                    null,
                    null,
                    eTag,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        internal static Query CreateQuery(
            ServiceConnectionString provisioningConnectionString,
            string query,
            IContractApiHttp contractApiHttp,
            CancellationToken cancellationToken,
            int pageSize = 0)
        {
            return new Query(provisioningConnectionString, ServiceName, query, contractApiHttp, pageSize, cancellationToken);
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

        internal static async Task<AttestationMechanism> GetEnrollmentAttestationAsync(
             IContractApiHttp contractApiHttp,
             string registrationId,
             CancellationToken cancellationToken)
        {
            ContractApiResponse contractApiResponse = await contractApiHttp
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

        private static Uri GetEnrollmentAttestationUri(string enrollmentGroupId)
        {
            enrollmentGroupId = WebUtility.UrlEncode(enrollmentGroupId);
            return new Uri(
                string.Format(CultureInfo.InvariantCulture, EnrollmentAttestationUriFormat, ServiceName, enrollmentGroupId, EnrollmentAttestationName, SdkUtils.ApiVersionQueryString),
                UriKind.Relative);
        }
    }
}
