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
    public sealed class EnrollmentGroupAlreadyExistsException : IotHubException
    {
        public EnrollmentGroupAlreadyExistsException(string enrollmentGroupId)
            : this(enrollmentGroupId, string.Empty)
        {
        }

        public EnrollmentGroupAlreadyExistsException(string enrollmentGroupId, string trackingId)
            : base("EnrollmentGroup {0} already exists".FormatInvariant(enrollmentGroupId), trackingId)
        {
        }

        public EnrollmentGroupAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if !WINDOWS_UWP && !NETSTANDARD2_0
        public EnrollmentGroupAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
