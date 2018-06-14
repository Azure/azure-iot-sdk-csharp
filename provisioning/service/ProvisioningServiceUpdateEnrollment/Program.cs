// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Provisioning.Service;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;

namespace ProvisioningServiceUpdateEnrollment
{
    /// <summary>
    /// Update the information in an individual enrollment on the Microsoft Azure IoT Hub Device Provisioning Service.
    /// </summary>
    /// <remarks>
    /// This sample will show how to update the information in a exited individualEnrollment. It will start creating
    ///     a new individualEnrolment. This enrollment contains an initialTwin state with the following information.
    /// <code>
    /// {
    ///     "Brand":"Contoso",
    ///     "Model":"SSC4",
    ///     "Color":"White",
    /// }
    /// </code>
    /// After that, the name of the color shall be updated to "Glace white".
    /// <b>Note:</b> If the device is already provisioned with the preview initialTwin state. Update the
    ///     individualEnrollment will not change the Twin state in the device.
    /// </remarks>
    class Program
    {
        /*
         * Details of the Provisioning.
         */
        private const string ProvisioningConnectionStringEnvVar = "PROVISIONING_CONNECTION_STRING";
        private const string TpmEndorsementKeyEnvVar = "TPM_ENDORSEMENT_KEY";
        private const string RegistrationIdEnvVar = "REGISTRATION_ID";

        private const string SampleRegistrationId = "myvalid-registratioid-csharp";
        private const string SampleTpmEndorsementKey =
            "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj2gUS" +
            "cTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjYO7KPVt3d" +
            "yKhZS3dkcvfBisBhP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD6l4sGBwFCnKR" +
            "dln4XpM03zLpoHFao8zOwt8l/uP3qUIxmCYv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI6zQFOKF/rwsfBtFe" +
            "WlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7dLIVPnlgZcBhgy1SSDQMQ==";

        private static string _provisioningConnectionString;
        private static string _registrationId;
        private static string _tpmEndorsementKey;

        public static void Main(string[] args)
        {
            try
            {
                ReadConfigurations(args);
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException(
                    "Test missing configuration:\n" +
                    "  This test requires a connection string, please create an environment variable " +
                    "PROVISIONING_CONNECTION_STRING with your provisioning connection string, or pass " +
                    "it as argument in the command line.\n", e);
            }

            RunSample().GetAwaiter().GetResult();
        }

        public static async Task RunSample()
        {
            Console.WriteLine("Starting sample...");

            TwinCollection desiredProperties = 
                new TwinCollection()
                {
                    ["Brand"] = "Contoso",
                    ["Model"] = "SSC4",
                    ["Color"] = "White",
                };

            using (ProvisioningServiceClient provisioningServiceClient =
                    ProvisioningServiceClient.CreateFromConnectionString(_provisioningConnectionString))
            {
                #region Create a new individualEnrollment
                Console.WriteLine("\nCreating a new individualEnrollment...");
                Attestation attestation = new TpmAttestation(_tpmEndorsementKey);
                IndividualEnrollment individualEnrollment =
                        new IndividualEnrollment(
                                SampleRegistrationId,
                                attestation);
                individualEnrollment.InitialTwinState = new TwinState(
                    null,
                    desiredProperties);

                IndividualEnrollment individualEnrollmentResult =
                    await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
                Console.WriteLine("\nIndividualEnrollment created with success...");
                Console.WriteLine(
                        "Note that there is a difference between the content of the individualEnrollment that you sent and\n" +
                        "  the individualEnrollmentResult that you received. The individualEnrollmentResult contains the eTag.");
                Console.WriteLine(
                        "\nindividualEnrollment:\n" + individualEnrollment);
                Console.WriteLine(
                        "\nindividualEnrollmentResult:\n" + individualEnrollmentResult);
                #endregion

                #region Update the info of individualEnrollment
                /*
                 * At this point, if you try to update your information in the provisioning service using the individualEnrollment
                 * that you created, it will fail because of the "precondition". It will happen because the individualEnrollment
                 * do not contains the eTag, and the provisioning service will not be able to check if the enrollment that you
                 * are updating is the correct one.
                 *
                 * So, to update the information you must use the individualEnrollmentResult that the provisioning service returned
                 * when you created the enrollment, another solution is get the latest enrollment from the provisioning service
                 * using the provisioningServiceClient.getIndividualEnrollment(), the result of this operation is an IndividualEnrollment
                 * object that contains the eTag.
                 */
                Console.WriteLine("\nUpdating the enrollment...");
                desiredProperties["Color"] = "Glace white";
                individualEnrollmentResult.InitialTwinState = new TwinState(null, desiredProperties);
                individualEnrollmentResult =
                    await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollmentResult).ConfigureAwait(false);
                Console.WriteLine("\nIndividualEnrollment updated with success...");
                Console.WriteLine(individualEnrollmentResult);
                #endregion

                #region Delete info of individualEnrollment
                Console.WriteLine("\nDeleting the individualEnrollment...");
                await provisioningServiceClient.DeleteIndividualEnrollmentAsync(_registrationId).ConfigureAwait(false);
                #endregion
            }
        }

        /// <summary>
        /// Read configurations for arguments or environment variables.
        /// </summary>
        /// <remarks>
        /// It will use the command line arguments, if it is not provided, it will read the test keys from the 
        ///     environment variable. If you are interested in the provisioning sample, you probably don't care 
        ///     about this method.
        /// </remarks>
        /// <param name="args">the list of arguments in command prompt.</param>
        public static void ReadConfigurations(string[] args)
        {
            if (args.Length > 0)
            {
                if (args.Length > 3)
                {
                    throw new ArgumentException("Too many arguments");
                }

                _provisioningConnectionString = args[0];
                if (args.Length > 1)
                {
                    _registrationId = args[1];
                }
                else
                {
                    _registrationId = SampleRegistrationId;
                }

                if (args.Length > 2)
                {
                    _tpmEndorsementKey = args[2];
                }
                else
                {
                    _tpmEndorsementKey = SampleTpmEndorsementKey;
                }
            }
            else
            {
                _provisioningConnectionString = Environment.GetEnvironmentVariable(ProvisioningConnectionStringEnvVar);
                try
                {
                    _registrationId = Environment.GetEnvironmentVariable(RegistrationIdEnvVar);
                    if (_registrationId == null)
                    {
                        _registrationId = SampleRegistrationId;
                    }
                }
                catch (ArgumentException)
                {
                    _registrationId = SampleRegistrationId;
                }
                try
                {
                    _tpmEndorsementKey = Environment.GetEnvironmentVariable(TpmEndorsementKeyEnvVar);
                    if (_tpmEndorsementKey == null)
                    {
                        _tpmEndorsementKey = SampleTpmEndorsementKey;
                    }
                }
                catch (ArgumentException)
                {
                    _tpmEndorsementKey = SampleTpmEndorsementKey;
                }
            }
        }
    }
}
