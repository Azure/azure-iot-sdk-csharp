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
        private const string PROVISIONING_CONNECTION_STRING_ENV_VAR = "PROVISIONING_CONNECTION_STRING";
        private const string TPM_ENDORSEMENT_KEY_ENV_VAR = "TPM_ENDORSEMENT_KEY";
        private const string REGISTRATION_ID_ENV_VAR = "REGISTRATION_ID";

        private const string SAMPLE_REGISTRATION_ID = "myvalid-registratioid-csharp";
        private const string SAMPLE_INVALID_TPM_ENDORSEMENT_KEY = 
            "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj2gUS" +
            "cTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjYO7KPVt3d" +
            "yKhZS3dkcvfBisBhP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD6l4sGBwFCnKR" +
            "dln4XpM03zLpoHFao8zOwt8l/uP3qUIxmCYv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI6zQFOKF/rwsfBtFe" +
            "WlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7dLIVPnlgZcBhgy1SSDQMQ==";

        // Optional parameters
        private const string OPTIONAL_DEVICE_ID = "myCSharpDevice";
        private const ProvisioningStatus OPTIONAL_PROVISIONING_STATUS = ProvisioningStatus.Enabled;

        private static string _provisioningConnectionString;
        private static string _registrationId;
        private static string _tpmEndorsementKey;

        static void Main(string[] args)
        {
            try
            {
                ReadConfigurations(args);
            }
            catch (Exception)
            {
                Console.WriteLine("Test missing configuration:");
                Console.WriteLine("  This test requires a connection string, please create an environment variable " +
                    "PROVISIONING_CONNECTION_STRING with your provisioning connection string, or pass it as argument " +
                    "in the command line.");
                throw;
            }

            RunSample().GetAwaiter().GetResult();
        }

        public static async Task RunSample()
        {
            Console.WriteLine("Starting sample...");

            // *********************************** Create a Provisioning Service Client ************************************
            ProvisioningServiceClient provisioningServiceClient =
                    ProvisioningServiceClient.CreateFromConnectionString(_provisioningConnectionString);

            // ******************************** Create a new individualEnrollment config **********************************
            Console.WriteLine("\nCreate a new individualEnrollment...");
            Attestation attestation = new TpmAttestation(_tpmEndorsementKey);
            IndividualEnrollment individualEnrollment =
                    new IndividualEnrollment(
                            SAMPLE_REGISTRATION_ID,
                            attestation);

            // The following parameters are optional. Remove it if you don't need.
            individualEnrollment.DeviceId = OPTIONAL_DEVICE_ID;
            individualEnrollment.ProvisioningStatus = OPTIONAL_PROVISIONING_STATUS;

            // ************************************ Create the individualEnrollment *************************************
            Console.WriteLine("\nAdd new individualEnrollment...");
            IndividualEnrollment individualEnrollmentResult = await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment);
            Console.WriteLine("\nIndividualEnrollment created with success...");
            Console.WriteLine(individualEnrollmentResult);

            // ************************************* Get info of individualEnrollment *************************************
            Console.WriteLine("\nGet the individualEnrollment information...");
            IndividualEnrollment getResult = await provisioningServiceClient.GetIndividualEnrollmentAsync(SAMPLE_REGISTRATION_ID);
            Console.WriteLine(getResult);

            //// ************************************ Query info of individualEnrollment ************************************
            //Console.WriteLine("\nCreate a query for enrollments...");
            //QuerySpecification querySpecification =
            //        new QuerySpecificationBuilder("*", QuerySpecificationBuilder.FromType.ENROLLMENTS)
            //                .createSqlQuery();
            //Query query = provisioningServiceClient.createIndividualEnrollmentQuery(querySpecification);

            //while (query.hasNext())
            //{
            //    Console.WriteLine("\nQuery the next enrollments...");
            //    QueryResult queryResult = query.next();
            //    Console.WriteLine(queryResult);
            //}

            // *********************************** Delete info of individualEnrollment ************************************
            Console.WriteLine("\nDelete the individualEnrollment...");
            await provisioningServiceClient.DeleteIndividualEnrollmentAsync(getResult);

            // ********************************* Destroy the Provisioning Service Client ***********************************
            provisioningServiceClient.Dispose();
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
                    throw new ArgumentException("Too much arguments");
                }

                _provisioningConnectionString = args[0];
                if(args.Length > 1)
                {
                    _registrationId = args[1];
                }
                else
                {
                    _registrationId = SAMPLE_REGISTRATION_ID;
                }

                if (args.Length > 2)
                {
                    _tpmEndorsementKey = args[2];
                }
                else
                {
                    _tpmEndorsementKey = SAMPLE_INVALID_TPM_ENDORSEMENT_KEY;
                }
            }
            else
            {
                _provisioningConnectionString = Environment.GetEnvironmentVariable(PROVISIONING_CONNECTION_STRING_ENV_VAR);
                try
                {
                    _registrationId = Environment.GetEnvironmentVariable(REGISTRATION_ID_ENV_VAR);
                }
                catch (ArgumentException)
                {
                    _registrationId = SAMPLE_REGISTRATION_ID;
                }
                try
                {
                    _tpmEndorsementKey = Environment.GetEnvironmentVariable(TPM_ENDORSEMENT_KEY_ENV_VAR);
                }
                catch (ArgumentException)
                {
                    _tpmEndorsementKey = SAMPLE_INVALID_TPM_ENDORSEMENT_KEY;
                }
            }
        }
    }
}
