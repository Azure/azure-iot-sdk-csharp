// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    public sealed class EnrollmentGroupAlreadyExistsException : IotHubException
    {
        public EnrollmentGroupAlreadyExistsException(string enrollmentGroupId)
            : this(enrollmentGroupId, string.Empty)
        {
        }

        public EnrollmentGroupAlreadyExistsException(string enrollmentGroupId, string trackingId)
            : base(ApiResources.EnrollmentGroupAlreadyExists.FormatInvariant(enrollmentGroupId), trackingId)
        {
        }

        public EnrollmentGroupAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
