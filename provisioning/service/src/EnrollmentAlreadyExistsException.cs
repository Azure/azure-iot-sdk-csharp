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
    public sealed class EnrollmentAlreadyExistsException : IotHubException
    {
        public EnrollmentAlreadyExistsException(string registrationId)
            : this(registrationId, string.Empty)
        {
            
        }

        public EnrollmentAlreadyExistsException(string registrationId, string trackingId)
            : base("Enrollment {0} already exists".FormatInvariant(registrationId), trackingId)
        {
        }

        public EnrollmentAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if !WINDOWS_UWP && !NETSTANDARD2_0
        public EnrollmentAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
