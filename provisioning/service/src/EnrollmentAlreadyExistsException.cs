// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    public sealed class EnrollmentAlreadyExistsException : IotHubException
    {
        public EnrollmentAlreadyExistsException(string registrationId)
            : this(registrationId, string.Empty)
        {
            
        }

        public EnrollmentAlreadyExistsException(string registrationId, string trackingId)
            : base(ApiResources.EnrollmentAlreadyExists.FormatInvariant(registrationId), trackingId)
        {
        }

        public EnrollmentAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
