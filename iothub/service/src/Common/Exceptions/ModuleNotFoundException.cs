﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class ModuleNotFoundException : IotHubException
    {
        public ModuleNotFoundException(string deviceId, string moduleId)
            : this(deviceId, moduleId, null, null)
        {
        }

        public ModuleNotFoundException(string deviceId, string moduleId, string iotHubName)
            : this(deviceId, moduleId, iotHubName, null)
        {
        }

        public ModuleNotFoundException(string deviceId, string moduleId, string iotHubName, string trackingId)
            : base(!string.IsNullOrEmpty(iotHubName) ? "Module {0} on Device {1} at IotHub {2} not registered".FormatInvariant(moduleId, deviceId, iotHubName) : "Module {0} on Device {0} not registered".FormatInvariant(moduleId, deviceId), trackingId)
        {
        }

        public ModuleNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ModuleNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
