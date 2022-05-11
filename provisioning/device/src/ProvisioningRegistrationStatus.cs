// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The provisioning status type.
    /// </summary>
    public enum ProvisioningRegistrationStatusType
    {
        /// <summary>
        /// Device has not yet come online.
        /// </summary>
        Unassigned = 1,

        /// <summary>
        /// Device has connected to the DRS but IoT Hub Id has not yet been returned to the device.
        /// </summary>
        Assigning = 2,

        /// <summary>
        /// DRS successfully returned a device Id and connection string to the device.
        /// </summary>
        Assigned = 3,

        /// <summary>
        /// Device enrollment failed.
        /// </summary>
        Failed = 4,

        /// <summary>
        /// Device is disabled.
        /// </summary>
        Disabled = 5,
    }
}
