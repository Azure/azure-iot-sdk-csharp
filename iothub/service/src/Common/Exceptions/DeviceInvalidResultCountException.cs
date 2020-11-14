// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the count of device results exceeds the specified maximum value.
    /// Note: This exception is currently not thrown by the client library.
    /// </summary>
    [Serializable]
    public sealed class DeviceInvalidResultCountException : IotHubException
    {
        /// <summary>
        /// Creates an instance of <see cref="DeviceInvalidResultCountException"/> with the value of the
        /// maximum allowable count of device results and marks it as non-transient.
        /// </summary>
        /// <param name="maximumResultCount">The maximum count of device results allowed.</param>
        public DeviceInvalidResultCountException(int maximumResultCount)
            : base($"Number of device results must be between 0 and {maximumResultCount}")
        {
            MaximumResultCount = maximumResultCount;
        }

        internal DeviceInvalidResultCountException()
            : base()
        {
        }

        internal DeviceInvalidResultCountException(string message)
            : base(message)
        {
        }

        internal DeviceInvalidResultCountException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private DeviceInvalidResultCountException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            MaximumResultCount = info.GetInt32("MaximumResultCount");
        }

        internal int MaximumResultCount { get; private set; }

        /// <inheritdoc />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("MaximumResultCount", MaximumResultCount);
        }
    }
}
