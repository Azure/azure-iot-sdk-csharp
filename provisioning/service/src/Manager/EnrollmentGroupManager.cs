// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
            ContractApiResponse contractApiResponse = await contractApiHttp
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

        internal static async Task<EnrollmentGroup> GetAsync(
            IContractApiHttp contractApiHttp,
            string enrollmentGroupId,
            CancellationToken cancellationToken)
        {
            ContractApiResponse contractApiResponse = await contractApiHttp
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

        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            EnrollmentGroup enrollmentGroup,
            CancellationToken cancellationToken)
        {
            await contractApiHttp
                .RequestAsync(
                    HttpMethod.Delete,
                    GetEnrollmentUri(enrollmentGroup.EnrollmentGroupId),
                    null,
                    null,
                    enrollmentGroup.ETag,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            string enrollmentGroupId,
            CancellationToken cancellationToken,
            string eTag = null)
        {
            await contractApiHttp
                .RequestAsync(
                    HttpMethod.Delete,
                    GetEnrollmentUri(enrollmentGroupId),
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

        private static Uri GetEnrollmentUri(string enrollmentGroupId)
        {
            enrollmentGroupId = WebUtility.UrlEncode(enrollmentGroupId);
            return new Uri(string.Format(CultureInfo.InvariantCulture, EnrollmentIdUriFormat, ServiceName, enrollmentGroupId, SdkUtils.ApiVersionQueryString), UriKind.Relative);
        }

        internal static async Task<AttestationMechanism> GetEnrollmentAttestationAsync(
            IContractApiHttp contractApiHttp,
            string enrollmentGroupId,
            CancellationToken cancellationToken)
        {
            ContractApiResponse contractApiResponse = await contractApiHttp
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

        private static Uri GetEnrollmentAttestationUri(string enrollmentGroupId)
        {
            enrollmentGroupId = WebUtility.UrlEncode(enrollmentGroupId);
            return new Uri(
                string.Format(CultureInfo.InvariantCulture, EnrollmentAttestationUriFormat, ServiceName, enrollmentGroupId, EnrollmentAttestationName, SdkUtils.ApiVersionQueryString),
                UriKind.Relative);
        }
    }
}
