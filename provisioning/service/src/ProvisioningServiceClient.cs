﻿// Copyright (c) Microsoft. All rights reserved.
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
                : new();

            Argument.AssertNotNullOrWhiteSpace(connectionString, nameof(connectionString));

            _provisioningConnectionString = ServiceConnectionStringParser.Parse(connectionString);
            _contractApiHttp = new ContractApiHttp(
                _provisioningConnectionString.HttpsEndpoint,
                _provisioningConnectionString,
                clientOptions);

            IndividualEnrollments = new IndividualEnrollmentsClient(_provisioningConnectionString, _contractApiHttp);
            EnrollmentGroups = new EnrollmentGroupsClient(_provisioningConnectionString, _contractApiHttp);
            DeviceRegistrationStates = new DeviceRegistrationStatesClient(_provisioningConnectionString, _contractApiHttp);
        }

        /// <summary>
        /// Subclient of <see cref="ProvisioningServiceClient"/> that handles all individual enrollment operations including
        /// getting/creating/setting/deleting individual enrollments, querying individual enrollments, and getting attestation mechanisms
        /// for particular individual enrollments.
        /// </summary>
        public IndividualEnrollmentsClient IndividualEnrollments { get; protected set; }

        /// <summary>
        /// Subclient of <see cref="ProvisioningServiceClient"/> that handles all enrollment group operations including
        /// getting/creating/setting/deleting enrollment groups, querying enrollment groups, and getting attestation mechanisms
        /// for particular enrollment groups.
        /// </summary>
        public EnrollmentGroupsClient EnrollmentGroups { get; protected set; }

        /// <summary>
        /// Subclient of <see cref="ProvisioningServiceClient"/> that handles all device registration state operations including
        /// getting a device registration state, deleting a device registration state, and querying device registration states.
        /// </summary>
        public DeviceRegistrationStatesClient DeviceRegistrationStates { get; protected set; }

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
