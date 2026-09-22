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
            if (enrollmentGroup == null)
            {
                throw new ArgumentNullException(nameof(enrollmentGroup));
            }

            ContractApiResponse contractApiResponse = await contractApiHttp
                .RequestAsync(
                    HttpMethod.Put,
                    GetEnrollmentUri(enrollmentGroup.EnrollmentGroupId, contractApiHttp.ServiceVersion),
                    null,
                    JsonConvert.SerializeObject(enrollmentGroup, JsonSerializerSettingsInitializer.GetJsonSerializerSettings(contractApiHttp.ServiceVersion)),
                    enrollmentGroup.ETag,
                    cancellationToken)
                .ConfigureAwait(false);

            if (contractApiResponse.Body == null)
            {
                throw new ProvisioningServiceClientHttpException(contractApiResponse, true);
            }

            return JsonConvert.DeserializeObject<EnrollmentGroup>(contractApiResponse.Body, JsonSerializerSettingsInitializer.GetJsonSerializerSettings());
        }

        internal static async Task<EnrollmentGroup> GetAsync(
            IContractApiHttp contractApiHttp,
            string enrollmentGroupId,
            CancellationToken cancellationToken)
        {
            ContractApiResponse contractApiResponse = await contractApiHttp
                .RequestAsync(
                    HttpMethod.Get,
                    GetEnrollmentUri(enrollmentGroupId, contractApiHttp.ServiceVersion),
                    null,
                    null,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            if (contractApiResponse.Body == null)
            {
                throw new ProvisioningServiceClientHttpException(contractApiResponse, true);
            }

            return JsonConvert.DeserializeObject<EnrollmentGroup>(contractApiResponse.Body, JsonSerializerSettingsInitializer.GetJsonSerializerSettings());
        }

        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            EnrollmentGroup enrollmentGroup,
            CancellationToken cancellationToken)
        {
            if (enrollmentGroup == null)
            {
                throw new ArgumentNullException(nameof(enrollmentGroup));
            }

            await contractApiHttp
                .RequestAsync(
                    HttpMethod.Delete,
                    GetEnrollmentUri(enrollmentGroup.EnrollmentGroupId, contractApiHttp.ServiceVersion),
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
                    GetEnrollmentUri(enrollmentGroupId, contractApiHttp.ServiceVersion),
                    null,
                    null,
                    eTag,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        internal static Query CreateQuery(
            ServiceConnectionString provisioningConnectionString,
            QuerySpecification querySpecification,
            HttpTransportSettings httpTransportSettings,
            CancellationToken cancellationToken,
            int pageSize = 0,
            ServiceVersion serviceVersion = ServiceVersion.V2019_03_31)
        {
            if (querySpecification == null)
            {
                throw new ArgumentNullException(nameof(querySpecification));
            }

            if (pageSize < 0)
            {
                throw new ArgumentException($"{nameof(pageSize)} cannot be negative");
            }

            return new Query(provisioningConnectionString, ServiceName, querySpecification, httpTransportSettings, pageSize, cancellationToken, serviceVersion);
        }

        private static Uri GetEnrollmentUri(string enrollmentGroupId, ServiceVersion serviceVersion)
        {
            enrollmentGroupId = WebUtility.UrlEncode(enrollmentGroupId);
            return new Uri(EnrollmentIdUriFormat.FormatInvariant(ServiceName, enrollmentGroupId, SdkUtils.GetApiVersionQueryString(serviceVersion)), UriKind.Relative);
        }

        internal static async Task<AttestationMechanism> GetEnrollmentAttestationAsync(
            IContractApiHttp contractApiHttp,
            string enrollmentGroupId,
            CancellationToken cancellationToken)
        {
            ContractApiResponse contractApiResponse = await contractApiHttp
                .RequestAsync(
                    HttpMethod.Post,
                    GetEnrollmentAttestationUri(enrollmentGroupId, contractApiHttp.ServiceVersion),
                    null,
                    null,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            if (contractApiResponse.Body == null)
            {
                throw new ProvisioningServiceClientHttpException(contractApiResponse, true);
            }

            return JsonConvert.DeserializeObject<AttestationMechanism>(contractApiResponse.Body, JsonSerializerSettingsInitializer.GetJsonSerializerSettings());
        }

        private static Uri GetEnrollmentAttestationUri(string enrollmentGroupId, ServiceVersion serviceVersion)
        {
            enrollmentGroupId = WebUtility.UrlEncode(enrollmentGroupId);
            return new Uri(
                EnrollmentAttestationUriFormat.FormatInvariant(ServiceName, enrollmentGroupId, EnrollmentAttestationName, SdkUtils.GetApiVersionQueryString(serviceVersion)),
                UriKind.Relative);
        }
    }
}
