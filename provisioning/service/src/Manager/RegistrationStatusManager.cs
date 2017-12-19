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
    internal static class RegistrationStatusManager
    {
        private const string ServiceName = "registrations";
        private const string DeviceRegistrationStatusUriFormat = "{0}/{1}?{2}";
        private const string DeviceRegistrationStatusFormat = "{0}/{1}";

        /// <summary>
        /// Get registration status information.
        /// </summary>
        /// <see cref="ProvisioningServiceClient.GetDeviceRegistrationStateAsync(string)"/>
        ///
        /// <param name="id">the <code>string</code> that identifies the deviceRegistrationState. It cannot be <code>null</code> or empty.</param>
        /// <returns>An <see cref="DeviceRegistrationState"/> with the device registration information.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the get operation.</exception>
        internal static async Task<DeviceRegistrationState> GetAsync(
            IContractApiHttp contractApiHttp,
            string id,
            CancellationToken cancellationToken)
        {
            /* SRS_REGISTRATION_STATUS_MANAGER_28_001: [The GetAsync shall throw ArgumentException if the provided ID is null or empty.] */
            ParserUtils.EnsureRegistrationId(id);

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

        /// <summary>
        /// Delete deviceRegistrationState.
        /// </summary>
        /// <see cref="ProvisioningServiceClient.DeleteDeviceRegistrationStatusAsync(string)"/>
        ///
        /// <param name="deviceRegistrationState">is an <see cref="DeviceRegistrationState"/> that describes device registration status which will be deleted. It cannot be <code>null</code>.</param>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the delete operation.</exception>
        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            DeviceRegistrationState deviceRegistrationState,
            CancellationToken cancellationToken)
        {
            /* SRS_REGISTRATION_STATUS_MANAGER_28_004: [The DeleteAsync shall throw ArgumentException if the provided deviceRegistrationState is null.] */
            if (deviceRegistrationState == null)
            {
                throw new ArgumentException(nameof(deviceRegistrationState));
            }

            /* SRS_REGISTRATION_STATUS_MANAGER_28_005: [The DeleteAsync shall sent the Delete HTTP request to remove the deviceRegistrationState.] */
            await contractApiHttp.RequestAsync(
                HttpMethod.Delete,
                GetDeviceRegistrationStatusUri(deviceRegistrationState.RegistrationId),
                null,
                null,
                null,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete deviceRegistrationState.
        /// </summary>
        /// <see cref="ProvisioningServiceClient.DeleteDeviceRegistrationStatusAsync(string)"/>
        /// <see cref="ProvisioningServiceClient.DeleteDeviceRegistrationStatusAsync(string, string)"/>
        ///
        /// <param name="id">is a <code>string</code> with the id of the registrationStatus to delete. It cannot be <code>null</code> or empty.</param>
        /// <param name="eTag">is a <code>string</code> with the eTag of the registrationStatus to delete. It can be <code>null</code> or empty (ignored).</param>
        /// <exception cref="ArgumentException">if the provided registrationId is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the delete operation.</exception>
        internal static async Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            string id,
            CancellationToken cancellationToken,
            string eTag = null)
        {
            /* SRS_REGISTRATION_STATUS_MANAGER_28_006: [The DeleteAsync shall throw ArgumentException if the provided id is null or empty.] */
            ParserUtils.EnsureRegistrationId(id);

            /* SRS_REGISTRATION_STATUS_MANAGER_28_007: [The DeleteAsync shall sent the Delete HTTP request to remove the deviceRegistrationState.] */
            await contractApiHttp.RequestAsync(
                HttpMethod.Delete,
                GetDeviceRegistrationStatusUri(id),
                null,
                null,
                eTag,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new deviceRegistrationState query.
        /// </summary>
        /// <see cref="ProvisioningServiceClient.CreateEnrollmentGroupRegistrationStatusQuery(QuerySpecification, string)"/>
        /// <see cref="ProvisioningServiceClient.CreateEnrollmentGroupRegistrationStatusQuery(QuerySpecification, string, int)"/>
        ///
        /// <param name="querySpecification">is a <code>string</code> with the SQL query specification. It cannot be <code>null</code>.</param>
        /// <param name="enrollmentGroupId">is a <code>string</code> with the id which the query run against. It cannot be <code>null</code>.</param>
        /// <param name="pageSize">the <code>int</code> with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <returns>A <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        internal static Query CreateEnrollmentGroupQuery(
            ServiceConnectionString provisioningConnectionString,
            QuerySpecification querySpecification, 
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

            /* SRS_REGISTRATION_STATUS_MANAGER_28_009: [The CreateQuery shall throw ArgumentException if the provided enrollmentGroupId is not valid.]] */
            ParserUtils.EnsureRegistrationId(enrollmentGroupId);

            /* SRS_REGISTRATION_STATUS_MANAGER_28_010: [The CreateQuery shall return a new Query for DeviceRegistrationState.] */
            return new Query(
                provisioningConnectionString, 
                GetGetDeviceRegistrationStatus(enrollmentGroupId), 
                querySpecification, 
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
