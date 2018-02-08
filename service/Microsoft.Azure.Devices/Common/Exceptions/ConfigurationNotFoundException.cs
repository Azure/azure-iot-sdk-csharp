// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if ENABLE_MODULES_SDK
namespace Microsoft.Azure.Devices.Common.Exceptions
{
    using System;
    using System.Runtime.Serialization;

#if !WINDOWS_UWP
    [Serializable]
#endif
    public class ConfigurationNotFoundException : IotHubException
    {
        public ConfigurationNotFoundException(string configurationId)
            : this(configurationId, null, null)
        {
        }

        public ConfigurationNotFoundException(string configurationId, string iotHubName)
            : this(configurationId, iotHubName, null)
        {
        }

        public ConfigurationNotFoundException(string configurationId, string iotHubName, string trackingId)
            : base(!string.IsNullOrEmpty(iotHubName)
                  ? "Configuration {0} at IotHub {1} is not registered".FormatInvariant(configurationId, iotHubName)
                  : "Configuration {0} not registered".FormatInvariant(configurationId), trackingId)
        {
        }

        public ConfigurationNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if !WINDOWS_UWP && !NETSTANDARD1_3
        public ConfigurationNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
#endif