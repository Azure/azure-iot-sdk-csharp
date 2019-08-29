// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Iot.DigitalTwin.Device.Helper;

namespace Azure.Iot.DigitalTwin.Device.Model
{
    public struct DigitalTwinPropertyReport
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
    }
}
