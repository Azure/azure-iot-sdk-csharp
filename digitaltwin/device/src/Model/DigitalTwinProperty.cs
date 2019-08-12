// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.DigitalTwin.Client.Helper;

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Model
{
    /// <summary>
    /// The Digital Twin property.
    /// </summary>
    public class DigitalTwinProperty
    {
        /// <summary>
        ///  Initializes a new instance of the <see cref="DigitalTwinProperty"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The Digital Twin schema type value of the property.</param>
        public DigitalTwinProperty(string name, DigitalTwinValue value)
        {
            GuardHelper.ThrowIfNullOrWhiteSpace(name, nameof(name));
            Name = name;
            Value = value;
        }

        /// <summary>
        /// The property name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The Digital Twin property value.
        /// </summary>
        public DigitalTwinValue Value { get; private set; }

        /// <summary>
        /// The raw Digital Twin property value.
        /// </summary>
        public object RawValue
        {
            get { return Value?.Value; }
        }
    }
}
