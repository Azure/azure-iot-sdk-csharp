// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.IoT.Thief.Device
{
    public static class TestConfiguration
    {
        public static string DeviceConnectionString => GetValue("IOTHUB_LONG_HAUL_DEVICE_CONNECTION_STRING");

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
