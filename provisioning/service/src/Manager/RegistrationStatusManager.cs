// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Service.Auth;
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
            /* SRS_REGISTRATION_STATUS_MANAGER_28_002: [The GetAsync shall sent the Get HTTP request to get the deviceRegistrationState information.] */
            ContractApiResponse contractApiResponse = await contractApiHttp.RequestAsync(
                HttpMethod.Get,
                GetDeviceRegistrationStatusUri(id),
                null,
                null,
                null,
                cancellationToken).ConfigureAwait(false);

            if (contractApiResponse.Body == null)
            {
                throw new ProvisioningServiceClientHttpException(contractApiResponse, true);
            }

            /* SRS_REGISTRATION_STATUS_MANAGER_28_003: [The GetAsync shall return a DeviceRegistrationState object created from the body of the HTTP response.] */
            return JsonConvert.DeserializeObject<DeviceRegistrationState>(contractApiResponse.Body);
        }

        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            DeviceRegistrationState deviceRegistrationState,
            CancellationToken cancellationToken)
        {
            /* SRS_REGISTRATION_STATUS_MANAGER_28_004: [The DeleteAsync shall throw ArgumentException if the provided deviceRegistrationState is null.] */
            if (deviceRegistrationState == null)
            {
                throw new ArgumentNullException(nameof(deviceRegistrationState));
            }

            /* SRS_REGISTRATION_STATUS_MANAGER_28_005: [The DeleteAsync shall sent the Delete HTTP request to remove the deviceRegistrationState.] */
            await contractApiHttp.RequestAsync(
                HttpMethod.Delete,
                GetDeviceRegistrationStatusUri(deviceRegistrationState.RegistrationId),
                null,
                null,
                deviceRegistrationState.ETag,
                cancellationToken).ConfigureAwait(false);
        }

        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            string id,
            CancellationToken cancellationToken,
            string eTag = null)
        {
            /* SRS_REGISTRATION_STATUS_MANAGER_28_007: [The DeleteAsync shall sent the Delete HTTP request to remove the deviceRegistrationState.] */
            await contractApiHttp.RequestAsync(
                HttpMethod.Delete,
                GetDeviceRegistrationStatusUri(id),
                null,
                null,
                eTag,
                cancellationToken).ConfigureAwait(false);
        }

        [SuppressMessage("Microsoft.Design", "CA1068",
            Justification = "Public API cannot change parameter order.")]
        internal static Query CreateEnrollmentGroupQuery(
            ServiceConnectionString provisioningConnectionString,
            QuerySpecification querySpecification,
            HttpTransportSettings httpTransportSettings,
            CancellationToken cancellationToken,
            string enrollmentGroupId,
            int pageSize = 0)
        {
            /* SRS_REGISTRATION_STATUS_MANAGER_28_008: [The CreateQuery shall throw ArgumentException if the provided querySpecification is null.] */
            if (querySpecification == null)
            {
                throw new ArgumentNullException(nameof(querySpecification));
            }

            if (pageSize < 0)
            {
                throw new ArgumentException($"{nameof(pageSize)} cannot be negative");
            }

            /* SRS_REGISTRATION_STATUS_MANAGER_28_010: [The CreateQuery shall return a new Query for DeviceRegistrationState.] */
            return new Query(
                provisioningConnectionString, 
                GetGetDeviceRegistrationStatus(enrollmentGroupId),
                querySpecification,
                httpTransportSettings,
                pageSize, 
                cancellationToken);
        }

        private static Uri GetDeviceRegistrationStatusUri(string id)
        {
            id = WebUtility.UrlEncode(id);
            return new Uri(DeviceRegistrationStatusUriFormat.FormatInvariant(
                ServiceName, id, SDKUtils.ApiVersionQueryString), UriKind.Relative);
        }

        private static string GetGetDeviceRegistrationStatus(string id)
        {
            return DeviceRegistrationStatusFormat.FormatInvariant(ServiceName, id);
        }
    }
}
