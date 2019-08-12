// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Xml;

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Model
{
    public class DigitalTwinValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinValue"/> class.
        /// </summary>
        /// <param name="value">The value of Digital Twin schema type long.</param>
        public static DigitalTwinValue CreateLong(long value)
        {
            return new DigitalTwinValue(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinValue"/> class.
        /// </summary>
        /// <param name="value">The value of Digital Twin schema type double.</param>
        public static DigitalTwinValue CreateDouble(double value)
        {
            return new DigitalTwinValue(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinValue"/> class.
        /// </summary>
        /// <param name="value">The value of PnP schema type boolean.</param>
        public static DigitalTwinValue CreateBoolean(bool value)
        {
            return new DigitalTwinValue(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinValue"/> class.
        /// </summary>
        /// <param name="value">The value of Digital Twin schema type string.</param>
        public static DigitalTwinValue CreateString(string value)
        {
            return new DigitalTwinValue(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinValue"/> class.
        /// </summary>
        /// <param name="value">The value when PnP schema type is Object.</param>
        public static DigitalTwinValue CreateObject(DataCollection value)
        {
            return new DigitalTwinValue(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinValue"/> class.
        /// </summary>
        /// <param name="value">The value of Digital Twin schema type date.</param>
        public static DigitalTwinValue CreateDate(DateTime value)
        {
            return new DigitalTwinValue(value.ToString("O", CultureInfo.InvariantCulture).Split('T')[0]);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinValue"/> class.
        /// </summary>
        /// <param name="value">The value of PnP schema type time.</param>
        public static DigitalTwinValue CreateTime(DateTime value)
        {
            return new DigitalTwinValue(value.ToString("O", CultureInfo.InvariantCulture).Split('T')[1]);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinValue"/> class.
        /// </summary>
        /// <param name="value">The property value of Digital Twin schema type duration.</param>
        public static DigitalTwinValue CreateDuration(TimeSpan value)
        {
            return new DigitalTwinValue(XmlConvert.ToString((TimeSpan)value));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinValue"/> class.
        /// </summary>
        /// <param name="value">The value of Digital Twin schema type datetime.</param>
        public static DigitalTwinValue CreateDateTime(DateTime value)
        {
            return new DigitalTwinValue(value.ToString("O", CultureInfo.InvariantCulture));
        }

        internal DigitalTwinValue(object value)
        {
            Value = value;
        }

        /// <summary>
        /// The property value.
        /// </summary>
        public object Value { get; private set; }
    }
}

