// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The Device Provisioning Service query result type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum QueryResultType
    {
        /// <summary>
        /// Unknown result type.
        /// </summary>
        /// <remarks>
        /// The provisioning service cannot parse the information in the body.
        /// You shall cast the objects in the items using string and
        /// parser it depending on the query the you sent.
        /// </remarks>
        Unknown,

        /// <summary>
        /// An individual enrollment.
        /// </summary>
        /// <remarks>
        /// The query result in a list of individual enrollments. You shall cast the
        /// objects in the items using <see cref="Service.IndividualEnrollment"/>.
        /// </remarks>
        Enrollment,

        /// <summary>
        /// An enrollment group.
        /// </summary>
        /// <remarks>
        /// The query result in a list of enrollmentGroup. You shall cast
        /// the objects in the items using <see cref="Service.EnrollmentGroup"/>.
        /// </remarks>
        EnrollmentGroup,

        /// <summary>
        /// A device registration.
        /// </summary>
        /// <remarks>
        /// The query result in a list of device registration. You shall cast
        /// the objects in the items using <see cref="DeviceRegistrationState"/>.
        /// </remarks>
        DeviceRegistration,
    }
}
