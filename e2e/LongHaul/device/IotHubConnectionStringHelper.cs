﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.LongHaul.Device
{
    internal class IotHubConnectionStringHelper
    {
        public IotHubConnectionStringHelper(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            string[] fields = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (string field in fields)
            {
                string[] element = field.Split('=', StringSplitOptions.TrimEntries);

                if (element[0].Equals("HostName"))
                {
                    HostName = element[1];
                }
                else if (element[0].Equals("DeviceId"))
                {
                    DeviceId = element[1];
                }
            }
        }

        public string HostName { get; private set; }

        public string DeviceId { get; private set; }
    }
}
