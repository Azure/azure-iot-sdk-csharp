// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Common;

namespace Microsoft.Azure.Devices.Provisioning.Service.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.Azure.Devices.Common.Exceptions;

#if !WINDOWS_UWP
    [Serializable]
#endif
    public sealed class EnrollmentNotFoundException : IotHubException
    {
        public EnrollmentNotFoundException(string registrationId)
            : this(registrationId, null, null)
        {
        }

        public EnrollmentNotFoundException(string registrationId, string drsName)
            : this(registrationId, drsName, null)
        {
        }

        public EnrollmentNotFoundException(string registrationId, string drsName, string trackingId)
            : base(!string.IsNullOrEmpty(drsName) ? "Enrollment {0} at DRS {1} not registered".FormatInvariant(registrationId, drsName) : "Enrollment {0} not found".FormatInvariant(registrationId), trackingId)
        {
        }

        public EnrollmentNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if !WINDOWS_UWP && !NETSTANDARD2_0
        public EnrollmentNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
