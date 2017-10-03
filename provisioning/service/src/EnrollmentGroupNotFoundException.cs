// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
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
            : base(!string.IsNullOrEmpty(drsName) ? 
                  ApiResources.EnrollmentGroupNotFoundAtServiceName.FormatInvariant(enrollmentGroupId, drsName) : 
                  ApiResources.EnrollmentGroupNotFound.FormatInvariant(enrollmentGroupId), trackingId)
        {
        }

        public EnrollmentGroupNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
