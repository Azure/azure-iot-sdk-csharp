// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when IoT hub receives an invalid protocol version number.
    /// Note: This exception is currently not thrown by the client library.
    /// </summary>
    [Serializable]
    public class InvalidProtocolVersionException : IotHubException
    {
        /// <summary>
        /// Creates an instance of <see cref="InvalidProtocolVersionException"/> with the specified protocol version number
        /// and marks it as non-transient.
        /// </summary>
        /// <param name="version">The protocol version number.</param>
        public InvalidProtocolVersionException(string version)
            : base(!string.IsNullOrEmpty(version)
                  ? $"Invalid protocol version: {version}."
                  : "Protocol version is required. But, it was not provided.")
        {
            RequestedVersion = version;
        }


        /////<inheritdoc/>
        //public override void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    if (info == null)
        //    {
        //        throw new ArgumentException("SerializationInfo should not be null.", nameof(info));
        //    }

        //    info.AddValue("RequestedVersion", RequestedVersion);

        //    base.GetObjectData(info, context);
        //}

        internal InvalidProtocolVersionException()
            : base()
        {
        }

        internal InvalidProtocolVersionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// The protocol version number sent to IoT hub.
        /// </summary>
        public string RequestedVersion { get; private set; }
    }
}
