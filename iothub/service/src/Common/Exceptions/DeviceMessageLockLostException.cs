// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    [Serializable]
    public class DeviceMessageLockLostException : IotHubException
    {
        public DeviceMessageLockLostException(string message)
            : base(message)
        {
        }

        public DeviceMessageLockLostException(ErrorCode code, string message)
            : base(code, message)
        {
        }

        public DeviceMessageLockLostException(string deviceId, Guid messageId)
            : this(deviceId, messageId, null)
        {
        }

        public DeviceMessageLockLostException(string deviceId, Guid messageId, string trackingId)
            : base("Message {0} lock was lost for Device {1}".FormatInvariant(messageId, deviceId), trackingId)
        {
        }

        private DeviceMessageLockLostException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
