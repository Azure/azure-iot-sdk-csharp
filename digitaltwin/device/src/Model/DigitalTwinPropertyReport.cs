// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Azure.Iot.DigitalTwin.Device.Helper;

namespace Azure.Iot.DigitalTwin.Device.Model
{
    /// <summary>
    /// Contains key and value pair for digital twin properties to be reported.
    /// </summary>
    public struct DigitalTwinPropertyReport : IEquatable<DigitalTwinPropertyReport>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinPropertyReport"/> struct.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The property value.</param>
        /// <param name="digitalTwinPropertyResponse">The reported property response.</param>
        public DigitalTwinPropertyReport(string name, string value, DigitalTwinPropertyResponse digitalTwinPropertyResponse = default)
        {
            this.Name = name;
            this.Value = value;
            this.DigitalTwinPropertyResponse = digitalTwinPropertyResponse;
        }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the value of the property.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the digital twin property response.
        /// </summary>
        public DigitalTwinPropertyResponse DigitalTwinPropertyResponse { get; }

        public static bool operator ==(DigitalTwinPropertyReport left, DigitalTwinPropertyReport right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DigitalTwinPropertyReport left, DigitalTwinPropertyReport right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether the specified DigitalTwinPropertyReport is equal to the current.
        /// </summary>
        /// <param name="other">The DigitalTwinPropertyReport to compare with the current.</param>
        /// <returns>True if the specified DigitalTwinPropertyReport is equal to the current; otherwise, false.</returns>
        public bool Equals(DigitalTwinPropertyReport other)
        {
            return
                string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(this.Value, other.Value, StringComparison.OrdinalIgnoreCase) &&
                this.DigitalTwinPropertyResponse.Equals(other.DigitalTwinPropertyResponse);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is DigitalTwinPropertyReport && this.Equals((DigitalTwinPropertyReport)obj);
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
                hashCode = (hashCode * 397) ^ this.Value.GetHashCode();
                hashCode = (hashCode * 397) ^ this.DigitalTwinPropertyResponse.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Validate the struct contains valid data.
        /// </summary>
        public void Validate()
        {
            GuardHelper.ThrowIfNullOrWhiteSpace(this.Name, $"DigitalTwinPropertyReport.{nameof(this.Name)}");
        }
    }
}
