﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// Parameters for the application
    /// </summary>
    internal class Parameters
    {
        [Option(
            'r',
            "RegistrationId",
            Required = true,
            HelpText = "The desired registration Id of the device that will use this derived key.")]
        public string RegistrationId { get; set; }

        [Option(
            'p',
            "PrimaryKey",
            Required = true,
            HelpText = "The primary key of the group enrollment.")]
        public string PrimaryKey { get; set; }
    }
}
