// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    public sealed class RegistrationNotFoundException : IotHubException
    {
        public RegistrationNotFoundException(string registrationId)
            : this(registrationId, null, null)
        {
        }

        public RegistrationNotFoundException(string registrationId, string drsName)
            : this(registrationId, drsName, null)
        {
        }

        public RegistrationNotFoundException(string registrationId, string drsName, string trackingId)
            : base(!string.IsNullOrEmpty(drsName) ? ApiResources.RegistrationNotFoundAtServiceName.FormatInvariant(registrationId, drsName)
                  : ApiResources.RegistrationNotFound.FormatInvariant(registrationId), trackingId)
        {
        }

        public RegistrationNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
