// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Device Provisioning Service Client.
    /// </summary>
    /// <remarks>
    /// The IoT hub Device Provisioning Service is a helper service for IoT hub that enables automatic device
    /// provisioning to a specified IoT hub without requiring human intervention. You can use the Device Provisioning
    /// Service to provision millions of devices in a secure and scalable manner.
    /// </remarks>
    public class ProvisioningServiceClient : IDisposable
    {
        private readonly ServiceConnectionString _provisioningConnectionString;
        private readonly IContractApiHttp _contractApiHttp;
        private readonly IProvisioningServiceRetryPolicy _retryPolicy;
        private readonly RetryHandler _retryHandler;

        /// <summary>
        /// Create a new instance of this client.
        /// </summary>
        /// <remarks>
        /// This client is created using the connection string for your Device Provisioning Service instance.
        /// </remarks>
        /// <param name="connectionString">The connection string of the Device Provisioning Service.</param>
        /// <param name="options">The optional client settings.</param>
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
                : new();

            Argument.AssertNotNullOrWhiteSpace(connectionString, nameof(connectionString));

            _provisioningConnectionString = ServiceConnectionStringParser.Parse(connectionString);
            _contractApiHttp = new ContractApiHttp(
                _provisioningConnectionString.HttpsEndpoint,
                _provisioningConnectionString,
                clientOptions);

            _retryPolicy = clientOptions.RetryPolicy ?? new ProvisioningServiceNoRetry();
            _retryHandler = new RetryHandler(_retryPolicy);

            // Subclients
            IndividualEnrollments = new IndividualEnrollmentsClient(_provisioningConnectionString, _contractApiHttp, _retryHandler);
            EnrollmentGroups = new EnrollmentGroupsClient(_provisioningConnectionString, _contractApiHttp, _retryHandler);
            DeviceRegistrationStates = new DeviceRegistrationStatesClient(_provisioningConnectionString, _contractApiHttp, _retryHandler);
        }

        /// <summary>
        /// Subclient of <see cref="ProvisioningServiceClient"/> that handles all individual enrollment operations including
        /// getting/creating/setting/deleting individual enrollments, querying individual enrollments, and getting attestation mechanisms
        /// for particular individual enrollments.
        /// </summary>
        public IndividualEnrollmentsClient IndividualEnrollments { get; protected private set; }

        /// <summary>
        /// Subclient of <see cref="ProvisioningServiceClient"/> that handles all enrollment group operations including
        /// getting/creating/setting/deleting enrollment groups, querying enrollment groups, and getting attestation mechanisms
        /// for particular enrollment groups.
        /// </summary>
        public EnrollmentGroupsClient EnrollmentGroups { get; protected private set; }

        /// <summary>
        /// Subclient of <see cref="ProvisioningServiceClient"/> that handles all device registration state operations including
        /// getting a device registration state, deleting a device registration state, and querying device registration states.
        /// </summary>
        public DeviceRegistrationStatesClient DeviceRegistrationStates { get; protected private set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_contractApiHttp != null)
            {
                _contractApiHttp.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
