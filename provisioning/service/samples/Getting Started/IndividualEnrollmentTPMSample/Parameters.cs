// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CommandLine;

namespace IndividualEnrollmentTpmSample
{
    internal class Parameters
    {
        [Option(
            'c',
            "ProvisioningConnectionString",
            Required = false,
            HelpText = "The connection string of device provisioning service. Not required when the PROVISIONING_CONNECTION_STRING environment variable is set.")]
        public string ProvisioningConnectionString { get; set; } = Environment.GetEnvironmentVariable("PROVISIONING_CONNECTION_STRING");

        [Option(
            'd',
            "DeviceId",
            Required = false,
            HelpText = "The Id of device.")]
        public string DeviceId { get; set; } = $"my-device-{Guid.NewGuid()}";

        [Option(
            'r',
            "RegistrationId",
            Required = false,
            HelpText = "The Id of registration.")]
        public string RegistrationId { get; set; } = $"my-registration-{Guid.NewGuid()}";

        [Option(
            'e',
            "EndorsementKey",
            Required = false,
            HelpText = "The endorsement key.")]
        public string EndorsementKey { get; set; } = "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj2gUS" +
            "cTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjYO7KPVt3d" +
            "yKhZS3dkcvfBisBhP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD6l4sGBwFCnKR" +
            "dln4XpM03zLpoHFao8zOwt8l/uP3qUIxmCYv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI6zQFOKF/rwsfBtFe" +
            "WlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7dLIVPnlgZcBhgy1SSDQMQ==";

        public static void ValidateProvisioningConnectionString(string provisioningConnectionString)
        {
            if (string.IsNullOrWhiteSpace(provisioningConnectionString))
            {
                Console.WriteLine("A provisioning connection string needs to be specified, " +
                    "please set the environment variable \"PROVISIONING_CONNECTION_STRING\" " +
                    "or pass in \"-c | --ProvisioningConnectionString\" through command line.");
                Environment.Exit(1);
            }
        }
    }
}
