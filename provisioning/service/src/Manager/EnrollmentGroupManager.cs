// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal static class EnrollmentGroupManager
    {
        private const string EnrollmentIdUriFormat = "enrollmentGroups/{0}?{1}";
        private const string EnrollmentUriFormat = "enrollmentGroups";

        /// <summary>
        /// Create or update an enrollment group record.
        /// </summary>
        /// <see cref="ProvisioningServiceClient.CreateOrUpdateEnrollmentGroup(EnrollmentGroup)"/>
        ///
        /// <param name="enrollmentGroup">is an <see cref="EnrollmentGroup"/> that describes the enrollment that will be created of updated. It cannot be <code>null</code>.</param>
        /// <returns>An <see cref="EnrollmentGroup"/> with the result of the creation or update request.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to create or update the enrollment.</exception>
        internal static Task<EnrollmentGroup> CreateOrUpdateAsync(
            IContractApiHttp contractApiHttp,
            EnrollmentGroup enrollmentGroup,
            CancellationToken cancellationToken)
        {
            /* SRS_ENROLLMENT_GROUP_MANAGER_28_001: [The CreateOrUpdateAsync shall throw ArgumentException if the provided enrollmentGroup is null.] */
            if (enrollmentGroup == null)
            {
                throw new ArgumentException("enrollmentGroup cannot be null.");
            }

            /* SRS_ENROLLMENT_GROUP_MANAGER_28_002: [The CreateOrUpdateAsync shall sent the Put HTTP request to create or update the enrollmentGroup.] */
            /* SRS_ENROLLMENT_GROUP_MANAGER_28_003: [The CreateOrUpdateAsync shall return an enrollmentGroup object created from the body of the HTTP response.] */
            return contractApiHttp.PutAsync(
                GetEnrollmentUri(enrollmentGroup.EnrollmentGroupId),
                enrollmentGroup,
                null,
                null,
                cancellationToken);
        }

        /// <summary>
        /// Retrieve the enrollmentGroup information.
        /// </summary>
        /// <see cref="ProvisioningServiceClient.GetEnrollmentGroupAsync(string)"/>
        ///
        /// <param name="enrollmentGroupId">the <code>string</code> that identifies the enrollmentGroup. It cannot be <code>null</code> or empty.</param>
        /// <returns>An <see cref="EnrollmentGroup"/> with the enrollment information.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the get operation.</exception>
        internal static Task<EnrollmentGroup> GetAsync(
            IContractApiHttp contractApiHttp,
            string enrollmentGroupId,
            CancellationToken cancellationToken)
        {
            /* SRS_ENROLLMENT_GROUP_MANAGER_28_007: [The GetAsync shall throw ArgumentException if the provided enrollmentGroupId is null or empty.] */
            ParserUtils.EnsureValidId(enrollmentGroupId);

            /* SRS_ENROLLMENT_GROUP_MANAGER_28_008: [The GetAsync shall sent the Get HTTP request to get the enrollmentGroup information.] */
            /* SRS_ENROLLMENT_GROUP_MANAGER_28_009: [The GetAsync shall return an EnrollmentGroup object created from the body of the HTTP response.] */
            return contractApiHttp.GetAsync<EnrollmentGroup>(
                GetEnrollmentUri(enrollmentGroupId),
                null,
                null,
                cancellationToken);
        }

        /// <summary>
        /// Delete enrollmentGroup.
        /// </summary>
        /// <see cref="ProvisioningServiceClient.DeleteEnrollmentGroupAsync(EnrollmentGroup)"/>
        ///
        /// <param name="enrollmentGroup">is an <see cref="EnrollmentGroup"/> that describes the enrollment that will be deleted. It cannot be <code>null</code>.</param>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the delete operation.</exception>
        internal static Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            EnrollmentGroup enrollmentGroup,
            CancellationToken cancellationToken)
        {
            /* SRS_ENROLLMENT_GROUP_MANAGER_28_010: [The DeleteAsync shall throw ArgumentException if the provided enrollmentGroup is null.] */
            if (enrollmentGroup == null)
            {
                throw new ArgumentException("enrollmentGroup cannot be null.");
            }

            /* SRS_ENROLLMENT_GROUP_MANAGER_28_011: [The DeleteAsync shall sent the Delete HTTP request to remove the enrollmentGroup.] */
            return contractApiHttp.DeleteAsync(
                GetEnrollmentUri(enrollmentGroup.EnrollmentGroupId),
                enrollmentGroup.ETag,
                null,
                null,
                cancellationToken);
        }

        /// <summary>
        /// Delete enrollmentGroupId.
        /// </summary>
        /// <see cref="ProvisioningServiceClient.DeleteEnrollmentGroupAsync(string)"/>
        /// <see cref="ProvisioningServiceClient.DeleteEnrollmentGroupAsync(string, string)"/>
        ///
        /// <param name="enrollmentGroupId">is a <code>string</code> with the enrollmentGroupId to delete. It cannot be <code>null</code> or empty.</param>
        /// <param name="eTag">is a <code>string</code> with the eTag of the enrollment to delete. It can be <code>null</code> or empty (ignored).</param>
        /// <exception cref="ArgumentException">if the provided registrationId is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the delete operation.</exception>
        internal static Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            string enrollmentGroupId,
            CancellationToken cancellationToken,
            string eTag = null)
        {
            /* SRS_ENROLLMENT_GROUP_MANAGER_28_012: [The DeleteAsync shall throw ArgumentException if the provided enrollmentGroupId is null or empty.] */
            ParserUtils.EnsureValidId(enrollmentGroupId);

            /* SRS_ENROLLMENT_GROUP_MANAGER_28_013: [The DeleteAsync shall sent the Delete HTTP request to remove the EnrollmentGroup.] */
            return contractApiHttp.DeleteAsync(
                GetEnrollmentUri(enrollmentGroupId),
                eTag,
                null,
                null,
                cancellationToken);
        }

        /// <summary>
        /// Create a new enrollmentGroup query.
        /// </summary>
        /// <see cref="ProvisioningServiceClient.CreateEnrollmentGroupQuery(QuerySpecification)"/>
        /// <see cref="ProvisioningServiceClient.CreateEnrollmentGroupQuery(QuerySpecification, int)"/>
        ///
        /// <param name="querySpecification">is a <code>string</code> with the SQL query specification. It cannot be <code>null</code>.</param>
        /// <param name="pageSize">the <code>int</code> with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <returns>A <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        internal static Query CreateQuery(QuerySpecification querySpecification, int pageSize = 0)
        {
            //TODO: Implement.

            /* SRS_ENROLLMENT_GROUP_MANAGER_28_014: [The CreateQuery shall throw ArgumentException if the provided querySpecification is null.] */
            /* SRS_ENROLLMENT_GROUP_MANAGER_28_015: [The CreateQuery shall return a new Query for EnrollmentGroup.] */

            throw new NotSupportedException("Query is not supported yet");
        }

        private static Uri GetEnrollmentUri(string enrollmentGroupId)
        {
            enrollmentGroupId = WebUtility.UrlEncode(enrollmentGroupId);
            return new Uri(EnrollmentIdUriFormat.FormatInvariant(enrollmentGroupId, SDKUtils.ApiVersionQueryString), UriKind.Relative);
        }
    }
}
