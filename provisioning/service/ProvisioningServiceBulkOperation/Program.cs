// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Provisioning.Service;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ProvisioningServiceBulkOperation
{
    class Program
    {
        /*
         * Details of the Provisioning.
         */
        private const string ProvisioningConnectionStringEnvVar = "PROVISIONING_CONNECTION_STRING";

        private const string SampleRegistrationId1 = "myvalid-registratioid-csharp-1";
        private const string SampleRegistrationId2 = "myvalid-registratioid-csharp-2";
        private const string SampleTpmEndorsementKey =
            "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj2gUS" +
            "cTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjYO7KPVt3d" +
            "yKhZS3dkcvfBisBhP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD6l4sGBwFCnKR" +
            "dln4XpM03zLpoHFao8zOwt8l/uP3qUIxmCYv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI6zQFOKF/rwsfBtFe" +
            "WlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7dLIVPnlgZcBhgy1SSDQMQ==";

        // Maximum number of elements per query.
        private const int QueryPageSize = 2;

        private static string _provisioningConnectionString;
        private static IDictionary<string, string> _registrationIds = new Dictionary<string, string>
        {
            { SampleRegistrationId1, SampleTpmEndorsementKey },
            { SampleRegistrationId2, SampleTpmEndorsementKey }
        };

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
                Console.WriteLine("\nCreating a new set of individualEnrollments...");
                List<IndividualEnrollment> individualEnrollments = new List<IndividualEnrollment>();
                foreach (var item in _registrationIds)
                {
                    Attestation attestation = new TpmAttestation(item.Value);
                    individualEnrollments.Add(new IndividualEnrollment(item.Key, attestation));
                }
                #endregion

                #region Create the individualEnrollment
                Console.WriteLine("\nRunning the bulk operation to create the individualEnrollments...");
                BulkEnrollmentOperationResult bulkEnrollmentOperationResult =
                    await provisioningServiceClient.RunBulkEnrollmentOperationAsync(BulkOperationMode.Create, individualEnrollments).ConfigureAwait(false);
                Console.WriteLine("\nResult of the Create bulk enrollment.");
                Console.WriteLine(bulkEnrollmentOperationResult);
                #endregion

                #region Get info of individualEnrollment
                foreach (IndividualEnrollment individualEnrollment in individualEnrollments)
                {
                    String registrationId = individualEnrollment.RegistrationId;
                    Console.WriteLine($"\nGetting the {nameof(individualEnrollment)} information for {registrationId}...");
                    IndividualEnrollment getResult = 
                        await provisioningServiceClient.GetIndividualEnrollmentAsync(registrationId).ConfigureAwait(false);
                    Console.WriteLine(getResult);
                }
                #endregion

                #region Query info of individualEnrollment
                Console.WriteLine("\nCreating a query for enrollments...");
                QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollments");
                using (Query query = provisioningServiceClient.CreateIndividualEnrollmentQuery(querySpecification, QueryPageSize))
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
                Console.WriteLine("\nDeleting the set of individualEnrollments...");
                bulkEnrollmentOperationResult =
                    await provisioningServiceClient.RunBulkEnrollmentOperationAsync(BulkOperationMode.Delete, individualEnrollments).ConfigureAwait(false);
                Console.WriteLine(bulkEnrollmentOperationResult);
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
                if (args.Length > 1)
                {
                    throw new ArgumentException("Too many arguments");
                }

                _provisioningConnectionString = args[0];
            }
            else
            {
                _provisioningConnectionString = Environment.GetEnvironmentVariable(ProvisioningConnectionStringEnvVar);
            }
        }
    }
}
