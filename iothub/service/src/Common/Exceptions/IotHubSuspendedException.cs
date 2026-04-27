// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// This exception is thrown when the IoT hub has been suspended. This is likely due to exceeding Azure
    /// spending limits. To resolve the error, check the Azure bill and ensure there are enough credits.
    /// </summary>
    [Serializable]
    public class IotHubSuspendedException : IotHubException
    {
        /// <summary>
        /// Creates an instance of this class with a name of the suspended IoT hub
        /// and marks it as non-transient.
        /// </summary>
        /// <param name="iotHubName">The name of the IoT hub that has been suspended.</param>
        public IotHubSuspendedException(string iotHubName)
            : base(Resources.IotHubSuspendedException.FormatInvariant(iotHubName))
        {
        }

        /// <summary>
        /// Creates an instance of this class with a name of the suspended IoT hub
        /// and tracking Id, and marks it as non-transient.
        /// </summary>
        /// <param name="iotHubName">The name of the IoT hub that has been suspended.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        public IotHubSuspendedException(string iotHubName, string trackingId)
            : base(Resources.IotHubSuspendedException.FormatInvariant(iotHubName), trackingId)
        {
        }

        internal IotHubSuspendedException()
            : base()
        {
        }

        internal IotHubSuspendedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
