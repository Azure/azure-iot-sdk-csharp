// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Model
{
    /// <summary>
    /// Contains response of the command passed from the Digital Twin Interface Client to Digital Twin Client
    /// for further processing (response to service).
    /// </summary>
    public struct DigitalTwinCommandResponse : IEquatable<DigitalTwinCommandResponse>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinCommandResponse"/> struct.
        /// </summary>
        /// <param name="status">The status of the executed command.</param>
        /// <param name="payload">The response data of command execution.</param>
        public DigitalTwinCommandResponse(int status, string payload)
        {
            this.Payload = payload;
            this.Status = status;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinCommandResponse"/> struct.
        /// </summary>
        /// <param name="status">The status of the executed command.</param>
        public DigitalTwinCommandResponse(int status)
        {
            this.Payload = null;
            this.Status = status;
        }

        /// <summary>
        /// Gets the serialized json representation of the payload in the response.
        /// </summary>
        public string Payload { get; }

        /// <summary>
        /// Gets the status of the response.
        /// </summary>
        public int Status { get; }

        public static bool operator ==(DigitalTwinCommandResponse left, DigitalTwinCommandResponse right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DigitalTwinCommandResponse left, DigitalTwinCommandResponse right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether the specified DigitalTwinCommandResponse is equal to the current.
        /// </summary>
        /// <param name="other">The DigitalTwinCommandResponse to compare with the current.</param>
        /// <returns>True if the specified DigitalTwinCommandResponse is equal to the current; otherwise, false.</returns>
        public bool Equals(DigitalTwinCommandResponse other)
        {
            return string.Equals(this.Payload, other.Payload, StringComparison.OrdinalIgnoreCase) && this.Status == other.Status;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is DigitalTwinCommandResponse && this.Equals((DigitalTwinCommandResponse)obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.Status.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.Payload != null ? this.Payload.GetHashCode() : 0);
                return hashCode;
            }
        }

    }
}
