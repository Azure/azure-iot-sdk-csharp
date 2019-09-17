// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.Iot.DigitalTwin.Device.Model
{
    /// <summary>
    /// Contains information of the property update request passed from the Digital Twin Client to Digital Twin Interface Client
    /// for further processing.
    /// </summary>
    public struct DigitalTwinPropertyUpdate : IEquatable<DigitalTwinPropertyUpdate>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinPropertyUpdate"/> struct.
        /// </summary>
        /// <param name="respondVersion">The response version.</param>
        /// <param name="statusCode">The status code which maps to appropriate HTTP status code of the property updates.</param>
        /// <param name="statusDescription">Friendly description string of current status of update.</param>
        internal DigitalTwinPropertyUpdate(string propertyName, int desiredVersion, string propertyDesired, string propertyReported)
        {
            this.PropertyName = propertyName;
            this.DesiredVersion = desiredVersion;
            this.PropertyDesired = propertyDesired;
            this.PropertyReported = propertyReported;
        }

        /// <summary>
        /// Gets the name of the property being update.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the value which the device application had previously reported for this property.
        /// This value may be NULL if the application never reported the property.  It will also
        /// be NULL when an update arrives to the given property after the initial callback.
        /// </summary>
        public string PropertyReported { get; }

        /// <summary>
        /// Gets the value of the service requests the given property to be set to.
        /// </summary>
        public string PropertyDesired { get; }

        /// <summary>
        /// Gets the version of this property.
        /// </summary>
        public int DesiredVersion { get; }

        /// <summary>
        /// Determines whether the specified DigitalTwinPropertyUpdate is equal to the current.
        /// </summary>
        /// <param name="other">The DigitalTwinPropertyUpdate to compare with the current.</param>
        /// <returns>True if the specified DigitalTwinPropertyUpdate is equal to the current; otherwise, false.</returns>
        public bool Equals(DigitalTwinPropertyUpdate other)
        {
            return
                string.Equals(this.PropertyName, other.PropertyName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(this.PropertyReported, other.PropertyReported, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(this.PropertyDesired, other.PropertyDesired, StringComparison.OrdinalIgnoreCase) &&
                this.DesiredVersion == other.DesiredVersion;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is DigitalTwinPropertyUpdate && this.Equals((DigitalTwinPropertyUpdate)obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(this.PropertyName, this.PropertyReported, this.PropertyDesired, this.DesiredVersion);
        }
    }
}
