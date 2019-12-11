// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.IoT.DigitalTwin.E2ETests
{
    class Configuration
    {
        public static string IotHubConnectionString => GetValue("IOTHUB_CONN_STRING_CSHARP");

        public static string EventHubConnectionString => GetValue("IOTHUB_EVENTHUB_CONN_STRING_CSHARP");

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
