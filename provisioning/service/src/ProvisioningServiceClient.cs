// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Contains methods that services can use to perform create, remove, update and delete operations on devices.
    /// </summary>
    public abstract class ProvisioningServiceClient : IDisposable
    {
        /// <summary>
        /// Creates a DeviceRegistrationClient from the DRS connection string.
        /// </summary>
        /// <param name="connectionString"> The DRS connection string.</param>
        /// <returns> A DeviceRegistrationClient instance. </returns>
        public static ProvisioningServiceClient CreateFromConnectionString(string connectionString)
        {
            // TODO: create provisioningServiceConnectionString and provisioningServiceConnectionStringBuilder classes
            IotHubConnectionString provisioningServiceConnectionString = IotHubConnectionString.Parse(connectionString);
            return new HttpProvisioningServiceClient(provisioningServiceConnectionString);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing) { }

        /// <summary>
        /// Explicitly open the DeviceRegistrationClient instance.
        /// </summary>
        public abstract Task OpenAsync();

        /// <summary>
        /// Closes the DeviceRegistrationClient instance and disposes its resources.
        /// </summary>
        public abstract Task CloseAsync();

        /// <summary>
        /// Enrolls a new device.
        /// </summary>
        /// <param name="enrollment">Device enrollment object</param>
        /// <returns>echoes back the Enrollment object</returns>
        public abstract Task<Enrollment> AddEnrollmentAsync(Enrollment enrollment);

        /// <summary>
        /// Enrolls a new device.
        /// </summary>
        /// <param name="enrollment">Device enrollment object</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>echoes back the Enrollment object</returns>
        public abstract Task<Enrollment> AddEnrollmentAsync(Enrollment enrollment, CancellationToken cancellationToken);

        /// <summary>
        /// Bulk enroll devices.
        /// </summary>
        /// <param name="enrollments">Enumerable collection of <see cref="Enrollment"/> objects</param>
        /// <returns>returns a <see cref="BulkOperationResult"/> object</returns>
        public abstract Task<BulkOperationResult> AddEnrollmentsAsync(IEnumerable<Enrollment> enrollments);

        /// <summary>
        /// Bulk enroll devices
        /// </summary>
        /// <param name="enrollments">Enumerable collection of <see cref="Enrollment"/> objects</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>returns a <see cref="BulkOperationResult"/> object</returns>
        public abstract Task<BulkOperationResult> AddEnrollmentsAsync(IEnumerable<Enrollment> enrollments, CancellationToken cancellationToken);

        /// <summary>
        /// Updates a device enrollment.
        /// </summary>
        /// <param name="enrollment">Device enrollment object</param>
        /// <returns>echoes back the Enrollment object</returns>
        public abstract Task<Enrollment> UpdateEnrollmentAsync(Enrollment enrollment);

        /// <summary>
        /// Updates a device enrollment.
        /// </summary>
        /// <param name="enrollment">Device enrollment object</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>echoes back the Enrollment object</returns>
        public abstract Task<Enrollment> UpdateEnrollmentAsync(Enrollment enrollment, CancellationToken cancellationToken);

        /// <summary>
        /// Updates a device enrollment.
        /// </summary>
        /// <param name="enrollment">Device enrollment object</param>
        /// <param name="forceUpdate">Forces the <see cref="Enrollment"/> object to be updated even if it has changed since it was retrieved last time.</param>
        /// <returns>echoes back the Enrollment object</returns>
        public abstract Task<Enrollment> UpdateEnrollmentAsync(Enrollment enrollment, bool forceUpdate);

        /// <summary>
        /// Updates a device enrollment.
        /// </summary>
        /// <param name="enrollment">Device enrollment object</param>
        /// <param name="forceUpdate">Forces the <see cref="Enrollment"/> object to be updated even if it has changed since it was retrieved last time.</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>echoes back the Enrollment object</returns>
        public abstract Task<Enrollment> UpdateEnrollmentAsync(Enrollment enrollment, bool forceUpdate, CancellationToken cancellationToken);

        /// <summary>
        /// Updates device enrollments.
        /// </summary>
        /// <param name="enrollments">Enumerable collection of <see cref="Enrollment"/> objects</param>
        /// <returns>returns a <see cref="BulkOperationResult"/> object</returns>
        public abstract Task<BulkOperationResult> UpdateEnrollmentsAsync(IEnumerable<Enrollment> enrollments);

        /// <summary>
        /// Updates device enrollments.
        /// </summary>
        /// <param name="enrollments">Enumerable collection of <see cref="Enrollment"/> objects</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>returns a <see cref="BulkOperationResult"/> object</returns>
        public abstract Task<BulkOperationResult> UpdateEnrollmentsAsync(IEnumerable<Enrollment> enrollments, CancellationToken cancellationToken);

        /// <summary>
        /// Updates device enrollments.
        /// </summary>
        /// <param name="enrollments">Enumerable collection of <see cref="Enrollment"/> objects</param>
        /// <param name="forceUpdate">Forces the <see cref="Enrollment"/> objects to be updated even if they had changed since retrieved last time.</param>
        /// <returns>returns a <see cref="BulkOperationResult"/> object</returns>
        public abstract Task<BulkOperationResult> UpdateEnrollmentsAsync(IEnumerable<Enrollment> enrollments, bool forceUpdate);

        /// <summary>
        /// Updates device enrollments.
        /// </summary>
        /// <param name="enrollments">Enumerable collection of <see cref="Enrollment"/> objects</param>
        /// <param name="forceUpdate">Forces the <see cref="Enrollment"/> objects to be updated even if they had changed since retrieved last time.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>returns a <see cref="BulkOperationResult"/> object</returns>
        public abstract Task<BulkOperationResult> UpdateEnrollmentsAsync(IEnumerable<Enrollment> enrollments, bool forceUpdate, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the device enrollment record.
        /// </summary>
        /// <param name="registrationId">Registration id</param>
        /// <returns>Device enrollment object</returns>
        public abstract Task<Enrollment> GetEnrollmentAsync(string registrationId);

        /// <summary>
        /// Gets the device enrollment record.
        /// </summary>
        /// <param name="registrationId">Registration id</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Device enrollment object</returns>
        public abstract Task<Enrollment> GetEnrollmentAsync(string registrationId, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the enrollments query.
        /// </summary>
        /// <returns>Query object</returns>
        public abstract IProvisioningQuery CreateEnrollmentsQuery();

        /// <summary>
        /// Creates the enrollments query.
        /// </summary>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns>Query object</returns>
        public abstract IProvisioningQuery CreateEnrollmentsQuery(int? pageSize);

        /// <summary>
        /// Creates the enrollment groups query.
        /// </summary>
        /// <returns>Query object</returns>
        public abstract IProvisioningQuery CreateEnrollmentGroupsQuery();

        /// <summary>
        /// Creates the enrollment groups query.
        /// </summary>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns>Query object</returns>
        public abstract IProvisioningQuery CreateEnrollmentGroupsQuery(int? pageSize);

        /// <summary>
        /// Removes a device enrollment.
        /// </summary>
        /// <param name="enrollment">Device enrollment object</param>
        public abstract Task RemoveEnrollmentAsync(Enrollment enrollment);

        /// <summary>
        /// Removes a new device.
        /// </summary>
        /// <param name="enrollment">Device enrollment object</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        public abstract Task RemoveEnrollmentAsync(Enrollment enrollment, CancellationToken cancellationToken);

        /// <summary>
        /// Removes a device enrollment.
        /// </summary>
        /// <param name="registrationId">Registration ID</param>
        public abstract Task RemoveEnrollmentAsync(string registrationId);

        /// <summary>
        /// Removes a device enrollment.
        /// </summary>
        /// <param name="registrationId">Registration ID</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        public abstract Task RemoveEnrollmentAsync(string registrationId, CancellationToken cancellationToken);

        /// <summary>
        /// Removes device enrollments.
        /// </summary>
        /// <param name="enrollments">Enumerable collection of <see cref="Enrollment"/> objects</param>
        public abstract Task RemoveEnrollmentsAsync(IEnumerable<Enrollment> enrollments);

        /// <summary>
        /// Removes device enrollments.
        /// </summary>
        /// <param name="enrollments">Enumerable collection of <see cref="Enrollment"/> objects</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        public abstract Task RemoveEnrollmentsAsync(IEnumerable<Enrollment> enrollments, CancellationToken cancellationToken);

        /// <summary>
        /// Removes device enrollments.
        /// </summary>
        /// <param name="registrationIds">Registration IDs</param>
        public abstract Task RemoveEnrollmentsAsync(IEnumerable<string> registrationIds);

        /// <summary>
        /// Removes device enrollments.
        /// </summary>
        /// <param name="registrationIds">Registration IDs</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public abstract Task RemoveEnrollmentsAsync(IEnumerable<string> registrationIds, CancellationToken cancellationToken);

        /// <summary>
        /// Enrolls a device group.
        /// </summary>
        /// <param name="enrollmentGroup">Device enrollment group object</param>
        /// <returns>echoes back the EnrollmentGroup object</returns>
        public abstract Task<EnrollmentGroup> AddEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup);

        /// <summary>
        /// Enrolls a device group.
        /// </summary>
        /// <param name="enrollmentGroup">Device enrollment group object</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>echoes back the EnrollmentGroup object</returns>
        public abstract Task<EnrollmentGroup> AddEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup, CancellationToken cancellationToken);

        /// <summary>
        /// Updates a device enrollment group.
        /// </summary>
        /// <param name="enrollmentGroup">Device enrollment group object</param>
        /// <returns>echoes back the EnrollmentGroup object</returns>
        public abstract Task<EnrollmentGroup> UpdateEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup);

        /// <summary>
        /// Updates a device enrollment group.
        /// </summary>
        /// <param name="enrollmentGroup">Device enrollment group object</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>echoes back the EnrollmentGroup object</returns>
        public abstract Task<EnrollmentGroup> UpdateEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup, CancellationToken cancellationToken);

        /// <summary>
        /// Updates a device enrollment group.
        /// </summary>
        /// <param name="enrollmentGroup">Device enrollment group object</param>
        /// <param name="forceUpdate">Forces the <see cref="EnrollmentGroup"/> object to be updated even if it has changed since it was retrieved last time.</param>
        /// <returns>echoes back the EnrollmentGroup object</returns>
        public abstract Task<EnrollmentGroup> UpdateEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup, bool forceUpdate);

        /// <summary>
        /// Updates a device enrollment group.
        /// </summary>
        /// <param name="enrollmentGroup">Device enrollment group object</param>
        /// <param name="forceUpdate">Forces the <see cref="EnrollmentGroup"/> object to be updated even if it has changed since it was retrieved last time.</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>echoes back the EnrollmentGroup object</returns>
        public abstract Task<EnrollmentGroup> UpdateEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup, bool forceUpdate, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the device enrollment group.
        /// </summary>
        /// <param name="enrollmentGroupId">Enrollment group ID</param>
        /// <returns>Device enrollment group object</returns>
        public abstract Task<EnrollmentGroup> GetEnrollmentGroupAsync(string enrollmentGroupId);

        /// <summary>
        /// Gets the device enrollment group.
        /// </summary>
        /// <param name="enrollmentGroupId">Enrollment group ID</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Device enrollment group object</returns>
        public abstract Task<EnrollmentGroup> GetEnrollmentGroupAsync(string enrollmentGroupId, CancellationToken cancellationToken);

        /// <summary>
        /// Removes a device enrollment group.
        /// </summary>
        /// <param name="enrollmentGroup">Device enrollment group object</param>
        public abstract Task RemoveEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup);

        /// <summary>
        /// Removes a device enrollment group.
        /// </summary>
        /// <param name="enrollmentGroup">Device enrollment group object</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        public abstract Task RemoveEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup, CancellationToken cancellationToken);

        /// <summary>
        /// Removes a device enrollment group.
        /// </summary>
        /// <param name="enrollmentGroupId">Enrollment group ID</param>
        public abstract Task RemoveEnrollmentGroupAsync(string enrollmentGroupId);

        /// <summary>
        /// Removes a device enrollment group.
        /// </summary>
        /// <param name="enrollmentGroupId">Enrollment group ID</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        public abstract Task RemoveEnrollmentGroupAsync(string enrollmentGroupId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the device registration.
        /// </summary>
        /// <param name="registrationId">Registration id</param>
        /// <returns>Device registration status object</returns>
        public abstract Task<RegistrationStatus> GetDeviceRegistrationAsync(string registrationId);

        /// <summary>
        /// Gets the device registration.
        /// </summary>
        /// <param name="registrationId">Registration id</param>
        /// <param name="cancellationToken"> Cancellation Token</param>
        /// <returns>Device registration status object</returns>
        public abstract Task<RegistrationStatus> GetDeviceRegistrationAsync(string registrationId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the registration information of devices using this enrollment group
        /// </summary>
        /// <param name="enrollmentGroupId">enrollmentGroupId id</param>
        /// <returns>Query object</returns>
        public abstract IProvisioningQuery CreateDeviceRegistrationsQuery(string enrollmentGroupId);

        /// <summary>
        /// Gets the registration information of devices using this enrollment group
        /// </summary>
        /// <param name="enrollmentGroupId">enrollmentGroupId id</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns>Query object</returns>
        public abstract IProvisioningQuery CreateDeviceRegistrationsQuery(string enrollmentGroupId, int? pageSize);

        /// <summary>
        /// Removes a device registration.
        /// </summary>
        /// <param name="registrationStatus">Device registration status object</param>
        public abstract Task RemoveDeviceRegistrationAsync(RegistrationStatus registrationStatus);

        /// <summary>
        /// Removes a device registration.
        /// </summary>
        /// <param name="registrationStatus">Device registration status object</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        public abstract Task RemoveDeviceRegistrationAsync(RegistrationStatus registrationStatus, CancellationToken cancellationToken);

        /// <summary>
        /// Removes a device registration.
        /// </summary>
        /// <param name="registrationId">Registration ID</param>
        public abstract Task RemoveDeviceRegistrationAsync(string registrationId);

        /// <summary>
        /// Removes a device registration.
        /// </summary>
        /// <param name="registrationId">Registration ID</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        public abstract Task RemoveDeviceRegistrationAsync(string registrationId, CancellationToken cancellationToken);
    }
}
