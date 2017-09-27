// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Common;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.Azure.Devices.Common.Exceptions;

#if !WINDOWS_UWP
    [Serializable]
#endif
    public sealed class EnrollmentGroupNotFoundException : IotHubException
    {
        public EnrollmentGroupNotFoundException(string enrollmentGroupId)
            : this(enrollmentGroupId, null, null)
        {
        }

        public EnrollmentGroupNotFoundException(string enrollmentGroupId, string drsName)
            : this(enrollmentGroupId, drsName, null)
        {
        }

        public EnrollmentGroupNotFoundException(string enrollmentGroupId, string drsName, string trackingId)
            : base(!string.IsNullOrEmpty(drsName) ? "Enrollment group {0} at DRS {1} not found".FormatInvariant(enrollmentGroupId, drsName) : "Enrollment group {0} not found".FormatInvariant(enrollmentGroupId), trackingId)
        {
        }

        public EnrollmentGroupNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if !WINDOWS_UWP && !NETSTANDARD2_0
        public EnrollmentGroupNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
