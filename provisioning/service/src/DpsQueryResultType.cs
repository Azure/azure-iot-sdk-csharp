// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    using System.Runtime.Serialization;

    public enum DpsQueryResultType
    {
        [EnumMember(Value = "unknown")]
        Unknown = 0,

        [EnumMember(Value = "enrollment")]
        Enrollment = 1,

        [EnumMember(Value = "enrollmentGroup")]
        EnrollmentGroup = 2,

        [EnumMember(Value = "deviceRegistration")]
        DeviceRegistration = 3,
    }
}