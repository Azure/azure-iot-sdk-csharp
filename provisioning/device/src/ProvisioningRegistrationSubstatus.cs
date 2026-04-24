// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The provisioning substatus type.
    /// </summary>
    public enum ProvisioningRegistrationSubstatusType
    {
        /// <summary>
        /// Device has been assigned to an IoT hub for the first time.
        /// </summary>
        InitialAssignment = 1,

        /// <summary>
        /// Device has been assigned to a different IoT hub and its device data was migrated from the previously assigned IoT hub.
        /// Device data was removed from the previously assigned IoT hub.
        /// </summary>
        DeviceDataMigrated = 2,

        /// <summary>
        /// Device has been assigned to a different IoT hub and its device data was populated from the initial state stored in the enrollment.
        /// Device data was removed from the previously assigned IoT hub.
        /// </summary>
        DeviceDataReset = 3,
    }
}