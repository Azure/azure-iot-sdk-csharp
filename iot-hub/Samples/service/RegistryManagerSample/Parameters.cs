﻿using CommandLine;

namespace Microsoft.Azure.Devices.Samples
{
    internal class Parameters
    {
        [Option(
            'c',
            "ConnectionString",
            Required = true,
            HelpText = "The service connection string with permissions to manage devices.")]
        public string ConnectionString { get; set; }

        [Option(
            'p',
            "PrimaryThumbprint",
            Required = true,
            HelpText = "Primary IoT hub PFX X509 thumbprint.")]
        public string PrimaryThumbprint { get; set; }

        [Option(
            's',
            "SecondaryThumbprint",
            Required = false,
            HelpText = "Secondary IoT hub PFX X509 thumbprint.")]
        public string SecondaryThumbprint { get; set; }

        [Option(
            'd',
            "DevicePrefix",
            Required = false,
            Default = "RegistryManagerSample-",
            HelpText = "The prefix to use when creating devices.")]
        public string DevicePrefix { get; set; }
    }
}
