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
    public sealed class DeviceRegistrationNotFoundException : IotHubException
    {
        public DeviceRegistrationNotFoundException(string registrationId)
            : this(registrationId, null, null)
        {
        }

        public DeviceRegistrationNotFoundException(string registrationId, string drsName)
            : this(registrationId, drsName, null)
        {
        }

        public DeviceRegistrationNotFoundException(string registrationId, string drsName, string trackingId)
            : base(!string.IsNullOrEmpty(drsName) ? "Device registration {0} at DRS {1} not found".FormatInvariant(registrationId, drsName) : "Device registration {0} not found".FormatInvariant(registrationId), trackingId)
        {
        }

        public DeviceRegistrationNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if !WINDOWS_UWP && !NETSTANDARD2_0
        public DeviceRegistrationNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
