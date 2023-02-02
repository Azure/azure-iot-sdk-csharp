// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

using System;

using System.Runtime.InteropServices;

namespace Microsoft.Azure.Devices.Samples
{
    /// <remarks>
    /// To get the connection string, log into https://azure.portal.com, go to Resources, open the IoT hub, open Shared Access Policies, open iothubowner, and copy a connection string.
    /// </remarks>
    internal class Parameters
    {
        [Option(
            'c',
            "HubConnectionString",
            HelpText = "The IoT hub connection string. "
                + "\nDefaults to environment variable 'IOTHUB_CONNECTION_STRING'.")]
        public string HubConnectionString { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");

        public bool Validate()
        {
            return !string.IsNullOrWhiteSpace(HubConnectionString);
        }
    }
}
