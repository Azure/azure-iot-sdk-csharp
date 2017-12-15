// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Azure.Devices.Provisioning.Service;
using System.Threading.Tasks;

namespace ProvisioningServiceEnrollmentGroup
{
    class Program
    {
        /*
            * Details of the Provisioning.
            */
        private const string ProvisioningConnectionStringEnvVar = "PROVISIONING_CONNECTION_STRING";
        private const string X509RootCertPathVar = "X509_ROOT_CERT_PATH";
        private const string EnrollmentGroupIdEnvVar = "ENROLLMENTGROUP_ID";

        private const string SampleEnrollmentGroupId = "enrollmentgrouptest";

        private static string _provisioningConnectionString;
        private static string _enrollmentGroupId;
        private static string _x509RootCertPath;

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
                    "  This test requires a connection string and a certificate. Please create an environment variable " +
                    "PROVISIONING_CONNECTION_STRING with your provisioning connection string, and" +
                    "X509_ROOT_CERT_PATH with the path to your root certificate, or pass " +
                    "them as arguments in the command line.\n", e);
            }

            RunSample().GetAwaiter().GetResult();
        }

        public static async Task RunSample()
        {
            Console.WriteLine("Starting sample...");

            using (ProvisioningServiceClient provisioningServiceClient =
                    ProvisioningServiceClient.CreateFromConnectionString(_provisioningConnectionString))
            {
                #region Create a new enrollmentGroup config
                Console.WriteLine("\nCreating a new enrollmentGroup...");
                string certificatePassword = ReadCertificatePassword();
                var certificate = new X509Certificate2(_x509RootCertPath, certificatePassword);
                Attestation attestation = X509Attestation.CreateFromRootCertificates(certificate);
                EnrollmentGroup enrollmentGroup =
                        new EnrollmentGroup(
                                _enrollmentGroupId,
                                attestation);
                Console.WriteLine(enrollmentGroup);
                #endregion

                #region Create the enrollmentGroup
                Console.WriteLine("\nAdding new enrollmentGroup...");
                EnrollmentGroup enrollmentGroupResult = await provisioningServiceClient.CreateOrUpdateEnrollmentGroupAsync(enrollmentGroup);
                Console.WriteLine("\nEnrollmentGroup created with success.");
                Console.WriteLine(enrollmentGroupResult);
                #endregion

                #region Get info of enrollmentGroup
                Console.WriteLine("\nGetting the enrollmentGroup information...");
                EnrollmentGroup getResult = await provisioningServiceClient.GetEnrollmentGroupAsync(SampleEnrollmentGroupId);
                Console.WriteLine(getResult);
                #endregion

                #region Query info of enrollmentGroup doc
                //Console.WriteLine("\nCreating a query for enrollments...");
                //QuerySpecification querySpecification =
                //        new QuerySpecificationBuilder("*", QuerySpecificationBuilder.FromType.ENROLLMENTS)
                //                .createSqlQuery();
                //Query query = provisioningServiceClient.CreateEnrollmentGroupQuery(querySpecification);

                //while (query.hasNext())
                //{
                //    Console.WriteLine("\nQuerying the next enrollments...");
                //    QueryResult queryResult = query.next();
                //    Console.WriteLine(queryResult);
                //    Console.WriteLine(queryResult);
                //}
                #endregion

                #region Delete info of enrollmentGroup
                Console.WriteLine("\nDeleting the enrollmentGroup...");
                await provisioningServiceClient.DeleteEnrollmentGroupAsync(getResult);
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
            if (args.Length > 1)
            {
                if (args.Length > 3)
                {
                    throw new ArgumentException("Too many arguments");
                }

                _provisioningConnectionString = args[0];
                _x509RootCertPath = args[1];
                if (args.Length > 2)
                {
                    _enrollmentGroupId = args[2];
                }
                else
                {
                    _enrollmentGroupId = SampleEnrollmentGroupId;
                }
            }
            else
            {
                _provisioningConnectionString = Environment.GetEnvironmentVariable(ProvisioningConnectionStringEnvVar);

                _enrollmentGroupId = Environment.GetEnvironmentVariable(EnrollmentGroupIdEnvVar) ?? SampleEnrollmentGroupId;

                _x509RootCertPath = Environment.GetEnvironmentVariable(X509RootCertPathVar);
            }
        }

        private static string ReadCertificatePassword()
        {
            var password = new StringBuilder();
            Console.WriteLine($"Enter the PFX password for {_x509RootCertPath}:");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password.Remove(password.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else
                {
                    Console.Write('*');
                    password.Append(key.KeyChar);
                }
            }

            return password.ToString();
        }
    }
}
