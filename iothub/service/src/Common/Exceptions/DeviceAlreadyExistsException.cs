// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    [Serializable]
    public sealed class DeviceAlreadyExistsException : IotHubException
    {
        public DeviceAlreadyExistsException(string deviceId)
            : this(deviceId, string.Empty)
        {
        }

        public DeviceAlreadyExistsException(string deviceId, string trackingId)
            : base("Device {0} already registered".FormatInvariant(deviceId), trackingId)
        {
        }

        public DeviceAlreadyExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public DeviceAlreadyExistsException(ErrorCode code, string message, Exception innerException = null)
            : base(code, message, innerException)
        {
        }

        public DeviceAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
