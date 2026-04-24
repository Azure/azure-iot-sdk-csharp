// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a request is made against an IoT hub that does not exist.
    /// </summary>
    [Serializable]
    public class IotHubNotFoundException : IotHubException
    {
        /// <summary>
        /// Creates an instance of this class with a name of the IoT hub and marks it as non-transient.
        /// </summary>
        /// <param name="iotHubName">The name of the IoT hub.</param>
        public IotHubNotFoundException(string iotHubName)
            : base($"IoT hub not found: {iotHubName}.")
        {
        }

        /// <summary>
        /// Creates an instance of this class with a name of the IoT hub and tracking Id, and marks it as non-transient.
        /// </summary>
        /// <param name="iotHubName">The name of the IoT hub.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        public IotHubNotFoundException(string iotHubName, string trackingId)
            : base($"IoT hub not found: {iotHubName}.", trackingId)
        {
        }

        internal IotHubNotFoundException()
            : base()
        {
        }

        internal IotHubNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
