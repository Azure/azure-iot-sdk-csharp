// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace ArmReadWrite
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'c',
            "PrimaryConnectionString",
            Required = true,
            HelpText = "The primary connection string for the device to simulate.")]
        public string PrimaryConnectionString { get; set; }
    }
}
