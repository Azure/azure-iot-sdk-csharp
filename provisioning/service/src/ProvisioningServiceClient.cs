// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Device Provisioning Service Client.
    /// </summary>
    /// <remarks>
    /// The IoT hub Device Provisioning Service is a helper service for IoT hub that enables automatic device
    /// provisioning to a specified IoT hub without requiring human intervention. You can use the Device Provisioning
    /// Service to provision millions of devices in a secure and scalable manner.
    ///
    /// This C# SDK provides an API to help developers to create and maintain Enrollments on the IoT hub Device
    /// Provisioning Service, it translate the rest API in C# Objects and Methods.
    ///
    /// To use the this SDK, you must include the follow package on your application.
    /// <code>
    /// // Include the following using to use the Device Provisioning Service APIs.
    /// using Microsoft.Azure.Devices.Provisioning.Service;
    /// </code>
    ///
    /// The main APIs are exposed by the <see cref="ProvisioningServiceClient"/>, it contains the public Methods that the
    /// application shall call to create and maintain the Enrollments. The Objects in the configs package shall
    /// be filled and passed as parameters of the public API, for example, to create a new enrollment, the application
    /// shall create the object <see cref="IndividualEnrollment"/> with the appropriate enrollment configurations, and call the
    /// <see cref="CreateOrUpdateIndividualEnrollmentAsync(IndividualEnrollment, CancellationToken)"/>.
    ///
    /// The IoT hub Device Provisioning Service supports SQL queries too. The application can create a new query using
    /// one of the queries factories, for instance <see cref="CreateIndividualEnrollmentQuery(string, CancellationToken)"/>, passing
    /// the <see cref="QuerySpecification"/>, with the SQL query. This factory returns a <see cref="Query"/> object, which is an
    /// active iterator.
    ///
    /// This C# SDK can be represented in the follow diagram, the first layer are the public APIs the your application
    /// shall use:
    ///
    /// <code>
    /// +===============+       +==========================================+                           +============+   +===+
    /// |    configs    |------>|         ProvisioningServiceClient        |                        +->|    Query   |   |   |
    /// +===============+       +==+=================+==================+==+                        |  +======+=====+   | e |
    ///                           /                  |                   \                          |         |         | x |
    ///                          /                   |                    \                         |         |         | c |
    /// +-----------------------+-----+  +-----------+------------+  +-----+---------------------+  |         |         | e |
    /// | IndividualEnrollmentManager |  | EnrollmentGroupManager |  | RegistrationStatusManager |  |         |         | p |
    /// +---------------+------+------+  +-----------+------+-----+  +-------------+-------+-----+  |         |         | t |
    ///                  \      \                    |       \                     |        \       |         |         | i |
    ///                   \      +----------------------------+------------------------------+------+         |         | o |
    ///                    \                         |                             |                          |         | n |
    ///  +--------+      +--+------------------------+-----------------------------+--------------------------+-----+   | s |
    ///  |  auth  |----->|                                    IContractApiHttp                                      |   |   |
    ///  +--------+      +-------------------------------------------+----------------------------------------------+   +===+
    ///                                                              |
    ///                                                              |
    ///                        +-------------------------------------+------------------------------------------+
    ///                        |                              System.Net.Http                                   |
    ///                        +--------------------------------------------------------------------------------+
    /// </code>
    /// </remarks>
    public class ProvisioningServiceClient : IDisposable
    {
        private readonly ServiceConnectionString _provisioningConnectionString;
        private readonly IContractApiHttp _contractApiHttp;

        /// <summary>
        /// Create a new instance of the ProvisioningServiceClient that exposes
        /// the API to the Device Provisioning Service.
        /// </summary>
        /// <remarks>
        /// The Device Provisioning Service Client is created based on a Provisioning Connection string.
        /// Once you create a Device Provisioning Service on Azure, you can get the connection string on the Azure portal.
        /// </remarks>
        /// <param name="connectionString">The connection string of the Device Provisioning Service.</param>
        /// <param name="options"> The options that allow configuration of the provisioning service client instance during initialization.</param>
        /// <returns>The ProvisioningServiceClient with the new instance of this object.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="connectionString"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="connectionString"/> is empty or white space.</exception>
        /// <exception cref="FormatException">If the provided <paramref name="connectionString"/> has incorrect value for host name.</exception>
        /// <exception cref="InvalidOperationException">If the provided <paramref name="connectionString"/> is missing host name,
        /// shared access key name or either shared access key or shared access signature.</exception>
        /// <exception cref="UnauthorizedAccessException">If the provided shared access signature is expired.</exception>
        public ProvisioningServiceClient(string connectionString, ProvisioningServiceClientOptions options = default)
        {
            ProvisioningServiceClientOptions clientOptions = options != null
                ? options.Clone()
                : new ();

            Argument.AssertNotNullOrWhiteSpace(connectionString, nameof(connectionString));

            _provisioningConnectionString = ServiceConnectionStringParser.Parse(connectionString);
            _contractApiHttp = new ContractApiHttp(
                _provisioningConnectionString.HttpsEndpoint,
                _provisioningConnectionString,
                clientOptions);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_contractApiHttp != null)
            {
                _contractApiHttp.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Creates or updates an individual enrollment.
        /// </summary>
        /// <param name="individualEnrollment">The individual enrollment object.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An individual enrollment</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="individualEnrollment"/> is null.</exception>
        /// <exception cref="DeviceProvisioningServiceException">If the service was not able to create or update the enrollment.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task<IndividualEnrollment> CreateOrUpdateIndividualEnrollmentAsync(IndividualEnrollment individualEnrollment, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(individualEnrollment, nameof(individualEnrollment));

            return IndividualEnrollmentManager.CreateOrUpdateAsync(_contractApiHttp, individualEnrollment, cancellationToken);
        }

        /// <summary>
        /// Create, update or delete a set of individual Device Enrollments.
        /// </summary>
        /// <remarks>
        /// This API provide the means to do a single operation over multiple individualEnrollments. A valid operation
        /// is determined by <see cref="BulkOperationMode"/>, and can be 'create', 'update', 'updateIfMatchETag', or 'delete'.
        /// </remarks>
        /// <param name="bulkOperationMode">The <see cref="BulkOperationMode"/> that defines the single operation to do over the individualEnrollments. It cannot be null.</param>
        /// <param name="individualEnrollments">The collection of <see cref="IndividualEnrollment"/> that contains the description of each individualEnrollment. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="BulkEnrollmentOperationResult"/> object with the result of operation for each enrollment.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="individualEnrollments"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="individualEnrollments"/> is an empty collection.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the client failed to send the request or service was not able to execute the bulk operation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task<BulkEnrollmentOperationResult> RunBulkEnrollmentOperationAsync(
            BulkOperationMode bulkOperationMode,
            IEnumerable<IndividualEnrollment> individualEnrollments,
            CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrEmpty(individualEnrollments, nameof(individualEnrollments));
            return IndividualEnrollmentManager.BulkOperationAsync(_contractApiHttp, bulkOperationMode, individualEnrollments, cancellationToken);
        }

        /// <summary>
        /// Gets the individual enrollment object.
        /// </summary>
        /// <param name="registrationId">The registration Id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The enrollment.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="registrationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="registrationId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">If the service was not able to get the enrollment.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task<IndividualEnrollment> GetIndividualEnrollmentAsync(string registrationId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));

            return IndividualEnrollmentManager.GetAsync(_contractApiHttp, registrationId, cancellationToken);
        }

        /// <summary>
        /// Deletes an individual enrollment.
        /// </summary>
        /// <param name="individualEnrollment">The individual enrollment.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="individualEnrollment"/> is null.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the client failed to send the request or service was not able to execute the operation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task DeleteIndividualEnrollmentAsync(IndividualEnrollment individualEnrollment, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(individualEnrollment, nameof(individualEnrollment));

            return IndividualEnrollmentManager.DeleteAsync(_contractApiHttp, individualEnrollment, cancellationToken);
        }

        /// <summary>
        /// Delete the individual enrollment information.
        /// </summary>
        /// <remarks>
        /// This method will remove the individualEnrollment from the Device Provisioning Service using the
        /// provided registrationId. It will delete the enrollment regardless the eTag. It means that this API
        /// correspond to the <see cref="DeleteIndividualEnrollmentAsync(string, string, CancellationToken)"/> with the eTag="*".
        ///
        /// Note that delete the enrollment will not remove the Device itself from the IotHub.
        /// </remarks>
        /// <param name="registrationId">The string that identifies the individualEnrollment. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="registrationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="registrationId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the client failed to send the request or service was not able to execute the operation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task DeleteIndividualEnrollmentAsync(string registrationId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));

            return IndividualEnrollmentManager.DeleteAsync(_contractApiHttp, registrationId, cancellationToken);
        }

        /// <summary>
        /// Deletes an individual enrollment if the eTag matches.
        /// </summary>
        /// <param name="registrationId">The registration id</param>
        /// <param name="eTag">The eTag.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="registrationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="registrationId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the client failed to send the request or service was not able to execute the operation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task DeleteIndividualEnrollmentAsync(string registrationId, string eTag, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));

            return IndividualEnrollmentManager.DeleteAsync(_contractApiHttp, registrationId, cancellationToken, eTag);
        }

        /// <summary>
        /// Factory to create a individualEnrollment query.
        /// </summary>
        /// <remarks>
        /// This method will create a new individualEnrollment query for Device Provisioning Service and return it
        /// as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        /// <c>"SELECT * FROM enrollments"</c>.
        /// </remarks>
        /// <param name="query">The SQL query. It cannot be null.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="query"/> is empty or white space.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Query CreateIndividualEnrollmentQuery(string query, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));

            return IndividualEnrollmentManager.CreateQuery(
                _provisioningConnectionString,
                query,
                _contractApiHttp,
                cancellationToken);
        }

        /// <summary>
        /// Factory to create a individualEnrollment query.
        /// </summary>
        /// <remarks>
        /// This method will create a new individualEnrollment query for Device Provisioning Service and return it
        /// as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        /// <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the Query will return a List of objects correspondent to the query result. The maximum
        /// number of items per iteration can be specified by the pageSize. It is optional, you can provide 0 for
        /// default pageSize or use the API <see cref="CreateIndividualEnrollmentQuery(string, CancellationToken)"/>.
        /// </remarks>
        /// <param name="query">The SQL query. It cannot be null.</param>
        /// <param name="pageSize">The int with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="query"/> is empty or white space.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the provided <paramref name="pageSize"/> is less than zero.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Query CreateIndividualEnrollmentQuery(string query, int pageSize, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));

            return IndividualEnrollmentManager.CreateQuery(
                _provisioningConnectionString,
                query,
                _contractApiHttp,
                cancellationToken,
                pageSize);
        }

        /// <summary>
        /// Create or update an enrollment group record.
        /// </summary>
        /// <remarks>
        /// This API creates a new enrollment group or update a existed one. All enrollment group in the Device
        /// Provisioning Service contains a unique identifier called enrollmentGroupId. If this API is called
        /// with an enrollmentGroupId that already exists, it will replace the existed enrollment group information
        /// by the new one. On the other hand, if the enrollmentGroupId does not exit, it will be created.
        ///
        /// To use the Device Provisioning Service API, you must include the follow package on your application.
        /// <code>
        /// // Include the following using to use the Device Provisioning Service APIs.
        /// using Microsoft.Azure.Devices.Provisioning.Service;
        /// </code>
        /// </remarks>
        /// <param name="enrollmentGroup">The <see cref="EnrollmentGroup"/> object that describes the individualEnrollment that will be created of updated.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An <see cref="EnrollmentGroup"/> object with the result of the create or update requested.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroup"/> is null.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to create or update the enrollment.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task<EnrollmentGroup> CreateOrUpdateEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(enrollmentGroup, nameof(enrollmentGroup));

            return EnrollmentGroupManager.CreateOrUpdateAsync(_contractApiHttp, enrollmentGroup, cancellationToken);
        }

        /// <summary>
        /// Retrieve the enrollment group information.
        /// </summary>
        /// <remarks>
        /// This method will return the enrollment group information for the provided enrollmentGroupId. It will retrieve
        /// the correspondent enrollment group from the Device Provisioning Service, and return it in the
        /// <see cref="EnrollmentGroup"/> object.
        /// </remarks>
        /// <param name="enrollmentGroupId">The string that identifies the enrollmentGroup. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="EnrollmentGroup"/> with the content of the enrollment group in the Provisioning Device Service.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to retrieve the enrollment group information for the provided <paramref name="enrollmentGroupId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task<EnrollmentGroup> GetEnrollmentGroupAsync(string enrollmentGroupId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));

            return EnrollmentGroupManager.GetAsync(_contractApiHttp, enrollmentGroupId, cancellationToken);
        }

        /// <summary>
        /// Delete the enrollment group information.
        /// </summary>
        /// <remarks>
        /// This method will remove the enrollment group from the Device Provisioning Service using the
        /// provided <see cref="EnrollmentGroup"/> information. The Device Provisioning Service will care about the
        /// enrollmentGroupId and the eTag on the enrollmentGroup. If you want to delete the enrollment regardless the
        /// eTag, you can set the eTag="*" into the enrollmentGroup, or use the <see cref="DeleteEnrollmentGroupAsync(string, CancellationToken)"/>.
        /// passing only the enrollmentGroupId.
        ///
        /// Note that delete the enrollment group will not remove the Devices itself from the IotHub.
        /// </remarks>
        /// <param name="enrollmentGroup">The <see cref="EnrollmentGroup"/> that identifies the enrollmentGroup. It cannot be null.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroup"/> is null.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to delete the enrollment group information for the provided <paramref name="enrollmentGroup"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task DeleteEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(enrollmentGroup, nameof(enrollmentGroup));

            return EnrollmentGroupManager.DeleteAsync(_contractApiHttp, enrollmentGroup, cancellationToken);
        }

        /// <summary>
        /// Delete the enrollment group information.
        /// </summary>
        /// <remarks>
        /// This method will remove the enrollment group from the Device Provisioning Service using the
        /// provided enrollmentGroupId. It will delete the enrollment group regardless the eTag. It means that this API
        /// correspond to the <see cref="DeleteEnrollmentGroupAsync(string, string, CancellationToken)"/> with the eTag="*".
        ///
        /// Note that delete the enrollment group will not remove the Devices itself from the IotHub.
        /// </remarks>
        /// <param name="enrollmentGroupId">The string that identifies the enrollmentGroup. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to delete the enrollment group information for the provided <paramref name="enrollmentGroupId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task DeleteEnrollmentGroupAsync(string enrollmentGroupId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));

            return EnrollmentGroupManager.DeleteAsync(_contractApiHttp, enrollmentGroupId, cancellationToken);
        }

        /// <summary>
        /// Delete the enrollment group information.
        /// </summary>
        /// <remarks>
        /// This method will remove the enrollment group from the Device Provisioning Service using the
        /// provided enrollmentGroupId and eTag. If you want to delete the enrollment group regardless the eTag, you can
        /// use <see cref="DeleteEnrollmentGroupAsync(string, CancellationToken)"/> or you can pass the eTag as null, empty, or "*".
        ///
        /// Note that delete the enrollment group will not remove the Device itself from the IotHub.
        /// </remarks>
        /// <param name="enrollmentGroupId">The string that identifies the enrollmentGroup. It cannot be null or empty.</param>
        /// <param name="eTag">The string with the enrollment group eTag. It can be null or empty.
        /// The Device Provisioning Service will ignore it in all of these cases.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to delete the enrollment group information for the provided <paramref name="enrollmentGroupId"/> and <paramref name="eTag"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task DeleteEnrollmentGroupAsync(string enrollmentGroupId, string eTag, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));

            return EnrollmentGroupManager.DeleteAsync(_contractApiHttp, enrollmentGroupId, cancellationToken, eTag);
        }

        /// <summary>
        /// Factory to create an enrollment group query.
        /// </summary>
        /// <remarks>
        /// This method will create a new enrollment group query on Device Provisioning Service and return it as
        /// a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        /// <c>"SELECT * FROM enrollments"</c>.
        /// </remarks>
        /// <param name="query">The SQL query. It cannot be null.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="query"/> is empty or white space.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Query CreateEnrollmentGroupQuery(string query, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));

            return EnrollmentGroupManager.CreateQuery(
                _provisioningConnectionString,
                query,
                _contractApiHttp,
                cancellationToken);
        }

        /// <summary>
        /// Factory to create an enrollment group query.
        /// </summary>
        /// <remarks>
        /// This method will create a new enrollment group query on Device Provisioning Service and return it as
        /// a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        /// <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the Query will return a List of objects correspondent to the query result. The maximum
        /// number of items per iteration can be specified by the pageSize. It is optional, you can provide 0 for
        /// default pageSize or use the API <see cref="CreateEnrollmentGroupQuery(string, CancellationToken)"/>.
        /// </remarks>
        /// <param name="query">The <see cref="QuerySpecification"/> with the SQL query. It cannot be null.</param>
        /// <param name="pageSize">The int with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="query"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="query"/> is empty or white space.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the provided <paramref name="pageSize"/> value is less than zero.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Query CreateEnrollmentGroupQuery(string query, int pageSize, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));

            return EnrollmentGroupManager.CreateQuery(
                _provisioningConnectionString,
                query,
                _contractApiHttp,
                cancellationToken,
                pageSize);
        }

        /// <summary>
        /// Retrieve the registration status information.
        /// </summary>
        /// <remarks>
        /// This method will return the DeviceRegistrationState for the provided id. It will retrieve
        /// the correspondent DeviceRegistrationState from the Device Provisioning Service, and return it in the
        /// <see cref="DeviceRegistrationState"/> object.
        /// </remarks>
        /// <param name="id">The string that identifies the DeviceRegistrationState. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="DeviceRegistrationState"/> with the content of the DeviceRegistrationState in the Provisioning Device Service.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="id"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="id"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to retrieve the registration state for the provided <paramref name="id"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task<DeviceRegistrationState> GetDeviceRegistrationStateAsync(string id, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(id, nameof(id));

            return RegistrationStatusManager.GetAsync(_contractApiHttp, id, cancellationToken);
        }

        /// <summary>
        /// Delete the registration status information.
        /// </summary>
        /// <remarks>
        /// This method will remove the DeviceRegistrationState from the Device Provisioning Service using the
        /// provided <see cref="DeviceRegistrationState"/> information. The Device Provisioning Service will care about the
        /// id and the eTag on the DeviceRegistrationState. If you want to delete the DeviceRegistrationState regardless the
        /// eTag, you can use the <see cref="DeleteDeviceRegistrationStateAsync(string, CancellationToken)"/> passing only the id.
        /// </remarks>
        /// <param name="deviceRegistrationState">The <see cref="DeviceRegistrationState"/> that identifies the DeviceRegistrationState.
        /// It cannot be null.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="deviceRegistrationState"/> is null.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// When the service wasn't able to delete the registration status.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task DeleteDeviceRegistrationStateAsync(DeviceRegistrationState deviceRegistrationState, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNull(deviceRegistrationState, nameof(deviceRegistrationState));

            return RegistrationStatusManager.DeleteAsync(_contractApiHttp, deviceRegistrationState, cancellationToken);
        }

        /// <summary>
        /// Delete the registration status information.
        /// </summary>
        /// <remarks>
        /// This method will remove the DeviceRegistrationState from the Device Provisioning Service using the
        /// provided id. It will delete the registration status regardless the eTag. It means that this API
        /// correspond to the <see cref="DeleteDeviceRegistrationStateAsync(string, string, CancellationToken)"/> with the <c>eTag="*"</c>.
        /// </remarks>
        /// <param name="id">The string that identifies the DeviceRegistrationState. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="id"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="id"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to delete the registration state for the provided <paramref name="id"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task DeleteDeviceRegistrationStateAsync(string id, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(id, nameof(id));

            return RegistrationStatusManager.DeleteAsync(_contractApiHttp, id, cancellationToken);
        }

        /// <summary>
        /// Delete the registration status information.
        /// </summary>
        /// <remarks>
        /// This method will remove the registration status from the Device Provisioning Service using the
        /// provided id and eTag. If you want to delete the registration status regardless the eTag, you can
        /// use <see cref="DeleteDeviceRegistrationStateAsync(string, CancellationToken)"/> or you can pass the eTag as <c>null</c>, empty, or
        /// <c>"*"</c>.
        /// </remarks>
        /// <param name="id">The string that identifies the DeviceRegistrationState. It cannot be null or empty.</param>
        /// <param name="eTag">The string with the DeviceRegistrationState eTag. It can be null or empty.
        /// The Device Provisioning Service will ignore it in all of these cases.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="id"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="id"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to delete the registration state for the provided <paramref name="id"/> and <paramref name="eTag"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task DeleteDeviceRegistrationStateAsync(string id, string eTag, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(id, nameof(id));

            return RegistrationStatusManager.DeleteAsync(_contractApiHttp, id, cancellationToken, eTag);
        }

        /// <summary>
        /// Factory to create a registration status query.
        /// </summary>
        /// <remarks>
        /// This method will create a new registration status query for a specific enrollment group on the Device
        /// Provisioning Service and return it as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        /// <c>"SELECT * FROM enrollments"</c>.
        /// </remarks>
        /// <param name="query">The <see cref="QuerySpecification"/> with the SQL query. It cannot be null.</param>
        /// <param name="enrollmentGroupId">The string that identifies the enrollmentGroup. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="query"/> or <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="query"/> or <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Query CreateEnrollmentGroupRegistrationStateQuery(
            string query,
            string enrollmentGroupId,
            CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));

            return RegistrationStatusManager.CreateEnrollmentGroupQuery(
                _provisioningConnectionString,
                query,
                _contractApiHttp,
                cancellationToken,
                enrollmentGroupId);
        }

        /// <summary>
        /// Factory to create a registration status query.
        /// </summary>
        /// <remarks>
        /// This method will create a new registration status query for a specific enrollment group on the Device
        /// Provisioning Service and return it as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        /// <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the Query will return a List of objects correspondent to the query result. The maximum
        /// number of items per iteration can be specified by the pageSize. It is optional, you can provide 0 for
        /// default pageSize or use the API <see cref="CreateIndividualEnrollmentQuery(string, CancellationToken)"/>.
        /// </remarks>
        /// <param name="query">The <see cref="QuerySpecification"/> with the SQL query. It cannot be null.</param>
        /// <param name="enrollmentGroupId">The string that identifies the enrollmentGroup. It cannot be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="pageSize">The int with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="query"/> or <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="query"/> or <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the provided <paramref name="pageSize"/> is less than zero.</exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Query CreateEnrollmentGroupRegistrationStateQuery(
            string query,
            string enrollmentGroupId,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(query, nameof(query));
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));

            return RegistrationStatusManager.CreateEnrollmentGroupQuery(
                _provisioningConnectionString,
                query,
                _contractApiHttp,
                cancellationToken,
                enrollmentGroupId,
                pageSize);
        }

        /// <summary>
        /// Retrieve the attestation information for an individual enrollment.
        /// </summary>
        /// <param name="registrationId">The registration Id of the individual enrollment to retrieve the attestation information of. This may not be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="AttestationMechanism"/> of the individual enrollment associated with the provided <paramref name="registrationId"/>.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="registrationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="registrationId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to retrieve the individual enrollment attestation information for the provided <paramref name="registrationId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task<AttestationMechanism> GetIndividualEnrollmentAttestationAsync(string registrationId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));

            return IndividualEnrollmentManager.GetEnrollmentAttestationAsync(_contractApiHttp, registrationId, cancellationToken);
        }

        /// <summary>
        /// Retrieve the enrollment group attestation information.
        /// </summary>
        /// <param name="enrollmentGroupId">The <c>string</c> that identifies the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="AttestationMechanism"/> associated with the provided <paramref name="enrollmentGroupId"/>.</returns>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        /// <exception cref="DeviceProvisioningServiceException">
        /// If the service was not able to retrieve the enrollment group attestation information for the provided <paramref name="enrollmentGroupId"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public Task<AttestationMechanism> GetEnrollmentGroupAttestationAsync(string enrollmentGroupId, CancellationToken cancellationToken = default)
        {
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));

            return EnrollmentGroupManager.GetEnrollmentAttestationAsync(_contractApiHttp, enrollmentGroupId, cancellationToken);
        }
    }
}
