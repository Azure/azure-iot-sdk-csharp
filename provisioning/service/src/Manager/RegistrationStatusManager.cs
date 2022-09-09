// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal static class RegistrationStatusManager
    {
        private const string ServiceName = "registrations";
        private const string DeviceRegistrationStatusUriFormat = "{0}/{1}?{2}";
        private const string DeviceRegistrationStatusFormat = "{0}/{1}";

        internal static async Task<DeviceRegistrationState> GetAsync(
            IContractApiHttp contractApiHttp,
            string id,
            CancellationToken cancellationToken)
        {
            ContractApiResponse contractApiResponse = await contractApiHttp
                .RequestAsync(
                    HttpMethod.Get,
                    GetDeviceRegistrationStatusUri(id),
                    null,
                    null,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            if (contractApiResponse.Body == null)
            {
                throw new ProvisioningServiceClientHttpException(contractApiResponse, true);
            }

            return JsonConvert.DeserializeObject<DeviceRegistrationState>(contractApiResponse.Body);
        }

        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            DeviceRegistrationState deviceRegistrationState,
            CancellationToken cancellationToken)
        {
            if (deviceRegistrationState == null)
            {
                throw new ArgumentNullException(nameof(deviceRegistrationState));
            }

            await contractApiHttp
                .RequestAsync(
                    HttpMethod.Delete,
                    GetDeviceRegistrationStatusUri(deviceRegistrationState.RegistrationId),
                    null,
                    null,
                    deviceRegistrationState.ETag,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            string id,
            CancellationToken cancellationToken,
            string eTag = null)
        {
            await contractApiHttp
                .RequestAsync(
                    HttpMethod.Delete,
                    GetDeviceRegistrationStatusUri(id),
                    null,
                    null,
                    eTag,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        [SuppressMessage("Microsoft.Design", "CA1068",
            Justification = "Public API cannot change parameter order.")]
        internal static Query CreateEnrollmentGroupQuery(
            ServiceConnectionString provisioningConnectionString,
            string query,
            IContractApiHttp contractApiHttp,
            CancellationToken cancellationToken,
            string enrollmentGroupId,
            int pageSize = 0)
        {
            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));

            if (pageSize < 0)
            {
                throw new ArgumentException($"{nameof(pageSize)} cannot be negative");
            }

            return new Query(
                provisioningConnectionString,
                GetGetDeviceRegistrationStatus(enrollmentGroupId),
                query,
                contractApiHttp,
                pageSize,
                cancellationToken);
        }

        private static Uri GetDeviceRegistrationStatusUri(string id)
        {
            id = WebUtility.UrlEncode(id);
            return new Uri(
                string.Format(CultureInfo.InvariantCulture, DeviceRegistrationStatusUriFormat, ServiceName, id, SdkUtils.ApiVersionQueryString),
                UriKind.Relative);
        }

        private static string GetGetDeviceRegistrationStatus(string id)
        {
            return string.Format(CultureInfo.InvariantCulture, DeviceRegistrationStatusFormat, ServiceName, id);
        }
    }
}
