// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.Iot.DigitalTwin.Device.Model
{
    /// <summary>
    /// Contains response of the property update request passed from the Digital Twin Client to Digital Twin
    /// Interface Client for further processing.
    /// </summary>
    public struct DigitalTwinPropertyResponse : IEquatable<DigitalTwinPropertyResponse>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinPropertyResponse"/> struct.
        /// </summary>
        /// <param name="respondVersion">The response version.</param>
        /// <param name="statusCode">The status code which maps to appropriate HTTP status code of the property updates.</param>
        /// <param name="statusDescription">Friendly description string of current status of update.</param>
        public DigitalTwinPropertyResponse(int respondVersion, int statusCode, string statusDescription)
        {
            this.RespondVersion = respondVersion;
            this.StatusCode = statusCode;
            this.StatusDescription = statusDescription;
        }

        /// <summary>
        /// Gets the version which is used for server to disambiguate calls for given property.
        /// </summary>
        public int RespondVersion { get; }

        /// <summary>
        /// Gets the status code associated with the respond.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Gets the status description associated with the respond.
        /// </summary>
        public string StatusDescription { get; }

        public static bool operator !=(DigitalTwinPropertyResponse obj1, DigitalTwinPropertyResponse obj2)
        {
            return !(obj1 == obj2);
        }

        public static bool operator ==(DigitalTwinPropertyResponse obj1, DigitalTwinPropertyResponse obj2)
        {
            return obj1.Equals(obj2);
        }

        /// <summary>
        /// Determines whether the specified DigitalTwinPropertyResponse is equal to the current.
        /// </summary>
        /// <param name="other">The DigitalTwinPropertyResponse to compare with the current.</param>
        /// <returns>True if the specified DigitalTwinPropertyResponse is equal to the current; otherwise, false.</returns>
        public bool Equals(DigitalTwinPropertyResponse other)
        {
            return
                this.RespondVersion.Equals(other.RespondVersion) &&
                this.StatusCode.Equals(other.StatusCode) &&
                string.Equals(this.StatusDescription, other.StatusDescription, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is DigitalTwinPropertyResponse && this.Equals((DigitalTwinPropertyResponse)obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.RespondVersion.GetHashCode();
                hashCode = (hashCode * 397) ^ this.StatusCode.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.StatusDescription != null ? this.StatusDescription.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
