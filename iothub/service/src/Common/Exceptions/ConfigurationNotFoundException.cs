// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the queried configuration is not available on IoT hub.
    /// </summary>
    [Serializable]
    public class ConfigurationNotFoundException : IotHubException
    {
        /// <summary>
        /// Creates an instance of <see cref="ConfigurationNotFoundException"/> with the Id of the configuration and marks it as non-transient.
        /// </summary>
        /// <param name="configurationId">The Id of the configuration whose details are unavailable on IoT hub.</param>
        public ConfigurationNotFoundException(string configurationId)
            : this(configurationId, null, null)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ConfigurationNotFoundException"/> with the Id of the configuration
        /// and the name of the IoT hub, and marks it as non-transient.
        /// </summary>
        /// <param name="configurationId">The Id of the configuration whose details are unavailable on IoT hub.</param>
        /// <param name="iotHubName">The name of the IoT hub.</param>
        public ConfigurationNotFoundException(string configurationId, string iotHubName)
            : this(configurationId, iotHubName, null)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ConfigurationNotFoundException"/> with the Id of the configuration,
        /// the name of the IoT hub and the tracking Id, and marks it as non-transient.
        /// </summary>
        /// <param name="configurationId">The Id of the configuration whose details are unavailable on IoT hub.</param>
        /// <param name="iotHubName">The name of the IoT hub.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        public ConfigurationNotFoundException(string configurationId, string iotHubName, string trackingId)
            : base(!string.IsNullOrEmpty(iotHubName)
                  ? $"Configuration {configurationId} at IotHub {iotHubName} is not registered."
                  : $"Configuration {configurationId} not registered.", trackingId)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="ConfigurationNotFoundException"/> with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ConfigurationNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal ConfigurationNotFoundException()
            : base()
        {
        }
    }
}
