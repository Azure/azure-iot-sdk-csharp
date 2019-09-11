// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Iot.DigitalTwin.Device.Helper;
using System;

namespace Azure.Iot.DigitalTwin.Device.Model
{
    public struct DigitalTwinPropertyReport : IEquatable<DigitalTwinPropertyReport>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public DigitalTwinPropertyReport(string name, string value) : this(name, value, DigitalTwinPropertyResponse.Empty)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="digitalTwinPropertyResponse"></param>
        public DigitalTwinPropertyReport(string name, string value, DigitalTwinPropertyResponse digitalTwinPropertyResponse)
        {
            GuardHelper.ThrowIfNullOrWhiteSpace(name, nameof(name));
            GuardHelper.ThrowIfNull(value, nameof(value));

            this.Name = name;
            this.Value = value;
            this.DigitalTwinPropertyResponse = digitalTwinPropertyResponse;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// 
        /// </summary>
        public DigitalTwinPropertyResponse DigitalTwinPropertyResponse { get; }

        public bool Equals(DigitalTwinPropertyReport other)
        {
            return
                string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(this.Value, other.Value, StringComparison.OrdinalIgnoreCase) &&
                this.DigitalTwinPropertyResponse.Equals(other.DigitalTwinPropertyResponse);
        }

        public override bool Equals(object obj)
        {
            return obj is DigitalTwinPropertyReport && Equals((DigitalTwinPropertyReport)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Name, this.Value, this.DigitalTwinPropertyResponse);
        }
    }
}
