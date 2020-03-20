// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// An exception for configuration not found
    /// </summary>
    [Serializable]
#pragma warning disable CA1032 // Implement standard exception constructors
    public class ConfigurationNotFoundException : IotHubException
#pragma warning restore CA1032 // Implement standard exception constructors
    {
        /// <summary>
        /// Creates an instance by configuration Id
        /// </summary>
        /// <param name="configurationId">The configuration Id</param>
        public ConfigurationNotFoundException(string configurationId)
            : this(configurationId, null, null)
        {
        }

        /// <summary>
        /// Creates an instance by configuration Id and hub name
        /// </summary>
        /// <param name="configurationId">The configuration Id</param>
        /// <param name="iotHubName">The hub name</param>
        public ConfigurationNotFoundException(string configurationId, string iotHubName)
            : this(configurationId, iotHubName, null)
        {
        }

        /// <summary>
        /// Creates an instance by configuration Id, hub name, and tracking Id
        /// </summary>
        /// <param name="configurationId">The configuration Id</param>
        /// <param name="iotHubName">The hub name</param>
        /// <param name="trackingId">The tracking Id</param>
        public ConfigurationNotFoundException(string configurationId, string iotHubName, string trackingId)
            : base(!string.IsNullOrEmpty(iotHubName)
                  ? "Configuration {0} at IotHub {1} is not registered".FormatInvariant(configurationId, iotHubName)
                  : "Configuration {0} not registered".FormatInvariant(configurationId), trackingId)
        {
        }

        /// <summary>
        /// Creates an instance with a message and inner exception
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="innerException">An inner exception</param>
        public ConfigurationNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// For serialization purposes
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context</param>
#pragma warning disable CA2229 // Implement serialization constructors

        public ConfigurationNotFoundException(SerializationInfo info, StreamingContext context)
#pragma warning restore CA2229 // Implement serialization constructors
            : base(info, context)
        {
        }
    }
}
