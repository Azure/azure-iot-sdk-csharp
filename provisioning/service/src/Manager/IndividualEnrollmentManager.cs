// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// IndividualEnrollment Manager.
    /// </summary>
    /// <remarks>
    /// This is the inner class that implements the IndividualEnrollment APIs.
    /// For the public API, please see <see cref="ProvisioningServiceClient"/>.
    /// </remarks>
    /// <see cref="https://docs.microsoft.com/en-us/azure/iot-dps/">Azure IoT Hub Device Provisioning Service</see>
    /// <see cref="https://docs.microsoft.com/en-us/rest/api/iot-dps/deviceenrollment">Device Enrollment</see>
    internal static class IndividualEnrollmentManager
    {
        private const string EnrollmentUriFormat = "enrollments/{0}?{1}";

        /// <summary>
        /// Create or update a device enrollment record.
        /// </summary>
        /// <see cref="ProvisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(IndividualEnrollment)"/>
        ///
        /// <param name="individualEnrollment">is an <see cref="IndividualEnrollment"/> that describes the enrollment that will be created of updated. It cannot be <code>null</code>.</param>
        /// <returns>An <see cref="IndividualEnrollment"/> with the result of the creation or update request.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to create or update the enrollment.</exception>
        internal static Task<IndividualEnrollment> CreateOrUpdateAsync(
            IContractApiHttp contractApiHttp, 
            IndividualEnrollment individualEnrollment,
            CancellationToken cancellationToken)
        {
            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_001: [The CreateOrUpdateAsync shall throw ArgumentException if the provided individualEnrollment is null.] */
            if (individualEnrollment == null)
            {
                throw new ArgumentException("individualEnrollment cannot be null.");
            }

            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_002: [The CreateOrUpdateAsync shall sent the Put Http request to create or update the individualEnrollment.] */
            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_003: [The CreateOrUpdateAsync shall return an IndividualEnrollment object created from the body of the Http response.] */
            return contractApiHttp.PutAsync(
                GetEnrollmentUri(individualEnrollment.RegistrationId), 
                individualEnrollment, 
                null,
                null,
                cancellationToken);
        }

        /// <summary>
        /// Rum a bulk individualEnrollment operation.
        /// </summary>
        /// <see cref="ProvisioningServiceClient.RunBulkEnrollmentOperationAsync(BulkOperationMode, IEnumerable{IndividualEnrollment})"/>
        ///
        /// <param name="bulkOperationMode">the <see cref="BulkOperationMode"/> that defines the single operation to do over the individualEnrollments. It cannot be <code>null</code>.</param>
        /// <param name="individualEnrollments">the collection of <see cref="IndividualEnrollment"/> that contains the description of each individualEnrollment. It cannot be <code>null</code> or empty.</param>
        /// <returns>An <see cref="BulkEnrollmentOperationResult"/> with the result of the bulk operation request.</returns>
        /// <exception cref="ArgumentException">if the provided parameters are not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the bulk operation.</exception>
        internal static Task<BulkEnrollmentOperationResult> BulkOperationAsync(
            IContractApiHttp contractApiHttp,
            BulkOperationMode bulkOperationMode, 
            IEnumerable<IndividualEnrollment> individualEnrollments,
            CancellationToken cancellationToken)
        {
            //TODO: Implement.

            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_004: [The BulkOperationAsync shall throw ArgumentException if the provided individualEnrollments is null.] */
            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_005: [The BulkOperationAsync shall sent the Put Http request to run the bulk operation to the collection of the individualEnrollment.] */
            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_006: [The BulkOperationAsync shall return an BulkEnrollmentOperationResult object created from the body of the Http response.] */

            throw new NotSupportedException("Bulk operation is not supported yet");
        }

        /// <summary>
        /// Get individualEnrollment information.
        /// </summary>
        /// <see cref="ProvisioningServiceClient.GetIndividualEnrollmentAsync(string)"/>
        ///
        /// <param name="registrationId">the <code>string</code> that identifies the individualEnrollment. It cannot be <code>null</code> or empty.</param>
        /// <returns>An <see cref="IndividualEnrollment"/> with the enrollment information.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the get operation.</exception>
        internal static Task<IndividualEnrollment> GetAsync(
            IContractApiHttp contractApiHttp,
            string registrationId,
            CancellationToken cancellationToken)
        {
            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_007: [The GetAsync shall throw ArgumentException if the provided registrationId is null or empty.] */
            ParserUtils.EnsureRegistrationId(registrationId);

            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_008: [The GetAsync shall sent the Get Http request to get the individualEnrollment information.] */
            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_009: [The GetAsync shall return an IndividualEnrollment object created from the body of the Http response.] */
            return contractApiHttp.GetAsync<IndividualEnrollment>(
                GetEnrollmentUri(registrationId),
                null,
                null,
                cancellationToken);
        }


        /// <summary>
        /// Delete individualEnrollment.
        /// </summary>
        /// <see cref="ProvisioningServiceClient.DeleteIndividualEnrollmentAsync(IndividualEnrollment)"/>
        ///
        /// <param name="individualEnrollment">is an <see cref="IndividualEnrollment"/> that describes the enrollment that will be deleted. It cannot be <code>null</code>.</param>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the delete operation.</exception>
        internal static Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            IndividualEnrollment individualEnrollment,
            CancellationToken cancellationToken)
        {
            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_010: [The DeleteAsync shall throw ArgumentException if the provided individualEnrollment is null.] */
            if (individualEnrollment == null)
            {
                throw new ArgumentException("individualEnrollment cannot be null.");
            }

            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_011: [The DeleteAsync shall sent the Delete Http request to remove the individualEnrollment.] */
            return contractApiHttp.DeleteAsync(
                GetEnrollmentUri(individualEnrollment.RegistrationId),
                individualEnrollment.ETag,
                null,
                null,
                cancellationToken);
        }

        /// <summary>
        /// Delete individualEnrollment.
        /// </summary>
        /// <see cref="ProvisioningServiceClient.DeleteIndividualEnrollmentAsync(string)"/>
        /// <see cref="ProvisioningServiceClient.DeleteIndividualEnrollmentAsync(string, string)"/>
        ///
        /// <param name="registrationId">is a <code>string</code> with the registrationId of the enrollment to delete. It cannot be <code>null</code> or empty.</param>
        /// <param name="eTag">is a <code>string</code> with the eTag of the enrollment to delete. It can be <code>null</code> or empty (ignored).</param>
        /// <exception cref="ArgumentException">if the provided registrationId is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the delete operation.</exception>
        internal static Task DeleteAsync(
            IContractApiHttp contractApiHttp,
            string registrationId,
            CancellationToken cancellationToken,
            string eTag = null)
        {
            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_012: [The DeleteAsync shall throw ArgumentException if the provided registrationId is null or empty.] */
            ParserUtils.EnsureRegistrationId(registrationId);

            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_013: [The DeleteAsync shall sent the Delete Http request to remove the individualEnrollment.] */
            return contractApiHttp.DeleteAsync(
                GetEnrollmentUri(registrationId),
                eTag,
                null,
                null,
                cancellationToken);
        }

        /// <summary>
        /// Create a new individualEnrollment query.
        /// </summary>
        /// <see cref="ProvisioningServiceClient.CreateIndividualEnrollmentQuery(QuerySpecification)"/>
        /// <see cref="ProvisioningServiceClient.CreateIndividualEnrollmentQuery(QuerySpecification, int)"/>
        ///
        /// <param name="querySpecification">is a <code>string</code> with the SQL query specification. It cannot be <code>null</code>.</param>
        /// <param name="pageSize">the <code>int</code> with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <returns>A <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        internal static Query CreateQuery(QuerySpecification querySpecification, int pageSize = 0)
        {
            //TODO: Implement.

            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_014: [The CreateQuery shall throw ArgumentException if the provided querySpecification is null.] */
            /* SRS_INDIVIDUAL_ENROLLMENT_MANAGER_21_015: [The CreateQuery shall return a new Query for IndividualEnrollments.] */

            throw new NotSupportedException("Query is not supported yet");
        }

        private static Uri GetEnrollmentUri(string registrationId)
        {
            registrationId = WebUtility.UrlEncode(registrationId);
            return new Uri(EnrollmentUriFormat.FormatInvariant(registrationId, SDKUtils.ApiVersionQueryString), UriKind.Relative);
        }
    }
}
