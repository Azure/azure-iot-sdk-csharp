// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Provisioning.Service;
using System.Threading.Tasks;

namespace ProvisioningServiceEnrollment
{
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

        // Optional parameters
        private const string OptionalDeviceId = "myCSharpDevice";
        private const ProvisioningStatus OptionalProvisioningStatus = ProvisioningStatus.Enabled;

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

            using (ProvisioningServiceClient provisioningServiceClient =
                    ProvisioningServiceClient.CreateFromConnectionString(_provisioningConnectionString))
            {
                #region Create a new individualEnrollment config
                Console.WriteLine("\nCreating a new individualEnrollment...");
                Attestation attestation = new TpmAttestation(_tpmEndorsementKey);
                IndividualEnrollment individualEnrollment =
                        new IndividualEnrollment(
                                SampleRegistrationId,
                                attestation);

                // The following parameters are optional. Remove it if you don't need.
                individualEnrollment.DeviceId = OptionalDeviceId;
                individualEnrollment.ProvisioningStatus = OptionalProvisioningStatus;
                #endregion

                #region Create the individualEnrollment
                Console.WriteLine("\nAdding new individualEnrollment...");
                IndividualEnrollment individualEnrollmentResult = 
                    await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
                Console.WriteLine("\nIndividualEnrollment created with success.");
                Console.WriteLine(individualEnrollmentResult);
                #endregion

                #region Get info of individualEnrollment
                Console.WriteLine("\nGetting the individualEnrollment information...");
                IndividualEnrollment getResult = 
                    await provisioningServiceClient.GetIndividualEnrollmentAsync(SampleRegistrationId).ConfigureAwait(false);
                Console.WriteLine(getResult);
                #endregion

                #region Query info of individualEnrollment
                Console.WriteLine("\nCreating a query for enrollments...");
                QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollments");
                using (Query query = provisioningServiceClient.CreateIndividualEnrollmentQuery(querySpecification))
                {
                    while (query.HasNext())
                    {
                        Console.WriteLine("\nQuerying the next enrollments...");
                        QueryResult queryResult = await query.NextAsync().ConfigureAwait(false);
                        Console.WriteLine(queryResult);
                    }
                }
                #endregion

                #region Delete info of individualEnrollment
                Console.WriteLine("\nDeleting the individualEnrollment...");
                await provisioningServiceClient.DeleteIndividualEnrollmentAsync(getResult).ConfigureAwait(false);
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
                if(args.Length > 1)
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
                    if(_registrationId == null)
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
                    if(_tpmEndorsementKey == null)
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
