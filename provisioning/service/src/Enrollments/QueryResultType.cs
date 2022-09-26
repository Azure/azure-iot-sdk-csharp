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
        /// the provisioning service cannot parse the information in the body.
        /// You shall cast the Objects in the items using string and
        /// parser it depending on the query the you sent.
        /// </remarks>
        Unknown,

        /// <summary>
        /// Enrollment result type.
        /// </summary>
        /// <remarks>
        /// The query result in a list of individualEnrollment. You shall cast the
        /// Objects in the items using <see cref="IndividualEnrollment"/>.
        /// </remarks>
        Enrollment,

        /// <summary>
        /// Enrollment group result type.
        /// </summary>
        /// <remarks>
        /// The query result in a list of enrollmentGroup. You shall cast
        /// the Objects in the items using <see cref="Service.EnrollmentGroup"/>.
        /// </remarks>
        EnrollmentGroup,

        /// <summary>
        /// Device registration result type.
        /// </summary>
        /// <remarks>
        /// The query result in a list of device registration. You shall cast
        /// the Objects in the items using <see cref="DeviceRegistrationState"/>.
        /// </remarks>
        DeviceRegistration,
    }
}
