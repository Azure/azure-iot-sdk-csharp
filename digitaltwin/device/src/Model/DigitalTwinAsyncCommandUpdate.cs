// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.Azure.Devices.DigitalTwin.Client.Helper;

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Model
{
    /// <summary>
    /// Contains information needed for updating an asynchronous command's status.
    /// </summary>
    public struct DigitalTwinAsyncCommandUpdate : IEquatable<DigitalTwinAsyncCommandUpdate>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinAsyncCommandUpdate"/> struct.
        /// </summary>
        /// <param name="name">The name of the command to be updated.</param>
        /// <param name="requestId">The request id of the command to be updated.</param>
        /// <param name="status">The status of the the command to be updated.</param>
        /// <param name="payload">The serialized payload of the the command to be updated.</param>
        public DigitalTwinAsyncCommandUpdate(string name, string requestId, int status, string payload = default)
        {
            this.Name = name;
            this.Payload = payload;
            this.RequestId = requestId;
            this.Status = status;
        }

        /// <summary>
        /// Gets the command name associated with this update.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the serialized payload associated with this update.
        /// </summary>
        public string Payload { get; }

        /// <summary>
        /// Gets the command request id associated with this update.
        /// </summary>
        public string RequestId { get; }

        /// <summary>
        /// Gets the status associated with this update.
        /// </summary>
        public int Status { get; }

        public static bool operator ==(DigitalTwinAsyncCommandUpdate left, DigitalTwinAsyncCommandUpdate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DigitalTwinAsyncCommandUpdate left, DigitalTwinAsyncCommandUpdate right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether the specified DigitalTwinAsyncCommandUpdate is equal to the current.
        /// </summary>
        /// <param name="other">The DigitalTwinAsyncCommandUpdate to compare with the current.</param>
        /// <returns>True if the specified DigitalTwinAsyncCommandUpdate is equal to the current; otherwise, false.</returns>
        public bool Equals(DigitalTwinAsyncCommandUpdate other)
        {
            return string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(this.Payload, other.Payload, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(this.RequestId, other.RequestId, StringComparison.OrdinalIgnoreCase) &&
                this.Status == other.Status;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is DigitalTwinAsyncCommandUpdate && this.Equals((DigitalTwinAsyncCommandUpdate)obj);
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
                hashCode = (hashCode * 397) ^ this.Status;
                return hashCode;
            }
        }

        /// <summary>
        /// Validate the struct contains valid data.
        /// </summary>
        public void Validate()
        {
            GuardHelper.ThrowIfNullOrWhiteSpace(this.Name, $"DigitalTwinAsyncCommandUpdate.{nameof(this.Name)}");
            GuardHelper.ThrowIfNullOrWhiteSpace(this.RequestId, $"DigitalTwinAsyncCommandUpdate.{nameof(this.RequestId)}");
        }
    }
}
