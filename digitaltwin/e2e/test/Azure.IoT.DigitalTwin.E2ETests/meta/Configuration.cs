// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.IoT.DigitalTwin.E2ETests
{
    class Configuration
    {
        public static string IotHubConnectionString => GetValue("IOTHUB_CONNECTION_STRING");

        public static string EventHubConnectionString => GetValue("EVENTHUB_CONNECTION_STRING");

        public static string EventhubName => GetValue("EVENTHUB_NAME");

        private static string GetValue(string envName, string defaultValue = null)
        {
            string envValue = Environment.GetEnvironmentVariable(envName);

            if (string.IsNullOrWhiteSpace(envValue))
            {
                return defaultValue ?? throw new InvalidOperationException($"Configuration missing: {envName}");
            }

            return Environment.ExpandEnvironmentVariables(envValue);
        }
    }
}
