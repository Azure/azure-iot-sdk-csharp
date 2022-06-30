﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Service.Auth;
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
            if (individualEnrollment == null)
            {
                throw new ArgumentNullException(nameof(individualEnrollment));
            }

            ContractApiResponse contractApiResponse = await contractApiHttp
                .RequestAsync(
                    HttpMethod.Put,
                    GetEnrollmentUri(individualEnrollment.RegistrationId),
                    null,
                    JsonConvert.SerializeObject(individualEnrollment),
                    individualEnrollment.ETag,
                    cancellationToken)
                .ConfigureAwait(false);

            if (contractApiResponse.Body == null)
            {
                throw new ProvisioningServiceClientHttpException(contractApiResponse, true);
            }

            return JsonConvert.DeserializeObject<IndividualEnrollment>(contractApiResponse.Body);
        }

        internal static async Task<BulkEnrollmentOperationResult> BulkOperationAsync(
            IContractApiHttp contractApiHttp,
            BulkOperationMode bulkOperationMode,
            IEnumerable<IndividualEnrollment> individualEnrollments,
            CancellationToken cancellationToken)
        {
            if (individualEnrollments == null)
            {
                throw new ArgumentNullException(nameof(individualEnrollments));
            }

            if (!individualEnrollments.Any())
            {
                throw new ArgumentException($"{nameof(individualEnrollments)} cannot be empty.");
            }

            ContractApiResponse contractApiResponse = await contractApiHttp
                .RequestAsync(
                    HttpMethod.Post,
                    GetEnrollmentUri(),
                    null,
                    BulkEnrollmentOperation.ToJson(bulkOperationMode, individualEnrollments),
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            if (contractApiResponse.Body == null)
            {
                throw new ProvisioningServiceClientHttpException(contractApiResponse, true);
            }

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

            if (contractApiResponse.Body == null)
            {
                throw new ProvisioningServiceClientHttpException(contractApiResponse, true);
            }

            return JsonConvert.DeserializeObject<IndividualEnrollment>(contractApiResponse.Body);
        }

        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            IndividualEnrollment individualEnrollment,
            CancellationToken cancellationToken)
        {
            if (individualEnrollment == null)
            {
                throw new ArgumentNullException(nameof(individualEnrollment));
            }

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
            QuerySpecification querySpecification,
            HttpTransportSettings httpTransportSettings,
            CancellationToken cancellationToken,
            int pageSize = 0)
        {
            if (querySpecification == null)
            {
                throw new ArgumentNullException(nameof(querySpecification));
            }

            if (pageSize < 0)
            {
                throw new ArgumentException($"{nameof(pageSize)} cannot be negative");
            }

            return new Query(provisioningConnectionString, ServiceName, querySpecification, httpTransportSettings, pageSize, cancellationToken);
        }

        private static Uri GetEnrollmentUri(string registrationId)
        {
            registrationId = WebUtility.UrlEncode(registrationId);
            return new Uri(EnrollmentIdUriFormat.FormatInvariant(ServiceName, registrationId, SdkUtils.ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetEnrollmentUri()
        {
            return new Uri(EnrollmentUriFormat.FormatInvariant(ServiceName, SdkUtils.ApiVersionQueryString), UriKind.Relative);
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

            if (contractApiResponse?.Body == null)
            {
                throw new ProvisioningServiceClientHttpException(contractApiResponse, true);
            }

            return JsonConvert.DeserializeObject<AttestationMechanism>(contractApiResponse.Body);
        }

        private static Uri GetEnrollmentAttestationUri(string enrollmentGroupId)
        {
            enrollmentGroupId = WebUtility.UrlEncode(enrollmentGroupId);
            return new Uri(
                EnrollmentAttestationUriFormat.FormatInvariant(ServiceName, enrollmentGroupId, EnrollmentAttestationName, SdkUtils.ApiVersionQueryString),
                UriKind.Relative);
        }
    }
}
