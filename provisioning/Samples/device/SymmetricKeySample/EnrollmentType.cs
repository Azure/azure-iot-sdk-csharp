// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// The type of enrollment for a device in the provisioning service.
    /// </summary>
    public enum EnrollmentType
    {
        /// <summary>
        ///  Enrollment for a single device.
        /// </summary>
        Individual,

        /// <summary>
        /// Enrollment for a group of devices.
        /// </summary>
        Group,
    }
}
