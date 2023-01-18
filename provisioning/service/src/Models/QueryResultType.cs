// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The Device Provisioning Service query result type
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum QueryResultType
    {
        /// <summary>
        /// Unknown result type.
        /// </summary>
        /// <remarks>
        /// The provisioning service cannot parse the information in the body.
        /// Cast the objects in the items using string and parser it depending on the query the sent.
        /// </remarks>
        Unknown,

        /// <summary>
        /// An individual enrollment.
        /// </summary>
        /// <remarks>
        /// The query result in a list of individual enrollments. Cast the objects in the items using <see cref="IndividualEnrollment"/>.
        /// </remarks>
        Enrollment,

        /// <summary>
        /// An enrollment group.
        /// </summary>
        /// <remarks>
        /// The query result in a list of enrollment groups. Cast the objects in the items using <see cref="Service.EnrollmentGroup"/>.
        /// </remarks>
        EnrollmentGroup,

        /// <summary>
        /// A device registration.
        /// </summary>
        /// <remarks>
        /// The query result in a list of device registrations. Cast the objects in the items using <see cref="DeviceRegistrationState"/>.
        /// </remarks>
        DeviceRegistration,
    }
}
