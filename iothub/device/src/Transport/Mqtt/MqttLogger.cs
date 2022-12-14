// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using MQTTnet.Diagnostics;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    internal class MqttLogger : IMqttNetLogger
    {
        public bool IsEnabled => Logging.IsEnabled;

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
        {
            var options = new EventSourceOptions
            {
                Level = ToEventLevel(logLevel)
            };

            string formattedMessage = message;
            if (parameters != null && parameters.Length > 0)
            {
                formattedMessage = string.Format(CultureInfo.CurrentCulture, message, parameters);
            }

            Logging.Log.Write(formattedMessage, options);
        }

        // MQTTNet has an enum to represent log level and our logging library has a separate enum. They have the
        // same values, though
        internal static EventLevel ToEventLevel(MqttNetLogLevel mqttNetLevel) =>
            mqttNetLevel switch
            {
                MqttNetLogLevel.Error => EventLevel.Error,
                MqttNetLogLevel.Warning => EventLevel.Warning,
                MqttNetLogLevel.Info => EventLevel.Informational,
                MqttNetLogLevel.Verbose => EventLevel.Verbose,
                _ => throw new ArgumentOutOfRangeException(nameof(mqttNetLevel), $"Unexpected level value: {mqttNetLevel}"),
            };
    }
}
