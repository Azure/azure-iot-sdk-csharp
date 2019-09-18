// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Azure.Iot.DigitalTwin.Device.Helper;

namespace Azure.Iot.DigitalTwin.Device.Model
{
    /// <summary>
    /// Contains information of the invoked command passed from the Digital Twin Client to Digital Twin Interface Client
    /// for further processing.
    /// </summary>
    public struct DigitalTwinCommandRequest : IEquatable<DigitalTwinCommandRequest>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinCommandRequest"/> struct.
        /// </summary>
        /// <param name="name">The name of the command.</param>
        /// <param name="requestId"> The server generated identifier passed as part of the command.</param>
        /// <param name="payload"> The serialized json representation of the payload in the request.</param>
        internal DigitalTwinCommandRequest(string name, string requestId, string payload)
        {
            this.Name = name;
            this.RequestId = requestId;
            this.Payload = payload;
        }

        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the server generated identifier of the command.
        /// </summary>
        public string RequestId { get; }

        /// <summary>
        /// Gets serialized json representation of the payload in the request.
        /// </summary>
        public string Payload { get; }

        public static bool operator ==(DigitalTwinCommandRequest left, DigitalTwinCommandRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DigitalTwinCommandRequest left, DigitalTwinCommandRequest right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether the specified DigitalTwinCommandRequest is equal to the current.
        /// </summary>
        /// <param name="other">The DigitalTwinCommandRequest to compare with the current.</param>
        /// <returns>True if the specified DigitalTwinCommandRequest is equal to the current; otherwise, false.</returns>
        public bool Equals(DigitalTwinCommandRequest other)
        {
            return
                string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(this.RequestId, other.RequestId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(this.Payload, other.Payload, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is DigitalTwinCommandRequest && this.Equals((DigitalTwinCommandRequest)obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.Name.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.Payload != null ? this.Payload.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ this.RequestId.GetHashCode();
                return hashCode;
            }
        }
    }
}
