// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Iot.DigitalTwin.Device.Helper;

namespace Azure.Iot.DigitalTwin.Device.Model
{
    public class DigitalTwinPropertyReport
    {
        public DigitalTwinPropertyReport(string name, string value) : this(name, value, null)
        {
        }

        public DigitalTwinPropertyReport(string name, string value, DigitalTwinPropertyResponse digitalTwinPropertyResponse)
        {
            GuardHelper.ThrowIfNullOrWhiteSpace(name, nameof(name));
            GuardHelper.ThrowIfNull(value, nameof(value));

            this.Name = name;
            this.Value = value;
            this.DigitalTwinPropertyResponse = digitalTwinPropertyResponse;
        }

        public string Name { get; }

        public string Value { get; }

        public DigitalTwinPropertyResponse DigitalTwinPropertyResponse { get; }
    }
}
