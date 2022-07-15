// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Azure;
using Azure.Core;
using CommandLine;
using Microsoft.Azure.Devices;
using System;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AzureSasCredentialAuthenticationSample
{
    public class Program
    {
        /// <summary>
        /// A sample to illustrate how to use SAS token for authentication to the IoT hub.
        /// <param name="args">Run with `--help` to see a list of required and optional parameters.</param>
        /// For more information on setting up AAD for IoT hub, see <see href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-dev-guide-azure-ad-rbac"/>
        /// </summary>
        public static async Task Main(string[] args)
        {            
            // Parse application parameters
            Parameters parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            // Initialize SAS token credentials.
            Console.WriteLine("Creating sas credential.");

            TimeSpan timeToLive = TimeSpan.FromHours(1);
            DateTime expiresOn = DateTime.UtcNow.Add(timeToLive);
            string sasToken = GenerateSasToken(parameters.ResourceUri, parameters.SharedAccessKey, parameters.SharedAccessKeyName, expiresOn);
            // Note: Pass the generated sasToken and not just the shared access signature when creating the AzureSasCredential.
            AzureSasCredential sasCredential = new AzureSasCredential(sasToken);

            // This is how the credential can be updated in the AzureSasCredential object whenever necessary.
            // This sample just shows how to perform the update but it is not necessary to update the token
            // until the token is close to its expiry.
            DateTime newExpiresOn = DateTime.UtcNow.Add(timeToLive);
            string updatedSasToken = GenerateSasToken(parameters.ResourceUri, parameters.SharedAccessKey, parameters.SharedAccessKeyName, newExpiresOn);
            sasCredential.Update(updatedSasToken);

            // There are constructors for all the other clients where you can pass SAS credentials - JobClient, RegistryManager, DigitalTwinClient
            var hostName = parameters.ResourceUri.Split('/')[0];
            using var serviceClient = ServiceClient.Create(hostName, sasCredential, parameters.TransportType);

            var sample = new AzureSasCredentialAuthenticationSample();
            await sample.RunSampleAsync(serviceClient, parameters.DeviceId);
        }

        private static string GenerateSasToken(string resourceUri, string sharedAccessKey, string policyName, DateTime expiresOn)
        {
            DateTime epochTime = new DateTime(1970, 1, 1);
            TimeSpan secondsFromEpochTime = expiresOn.Subtract(epochTime);
            long seconds = Convert.ToInt64(secondsFromEpochTime.TotalSeconds, CultureInfo.InvariantCulture);
            string expiry = Convert.ToString(seconds, CultureInfo.InvariantCulture);

            string stringToSign = WebUtility.UrlEncode(resourceUri) + "\n" + expiry;

            HMACSHA256 hmac = new HMACSHA256(Convert.FromBase64String(sharedAccessKey));
            string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

            // SharedAccessSignature sr=ENCODED(dh://myiothub.azure-devices.net/a/b/c?myvalue1=a)&sig=<Signature>&se=<ExpiresOnValue>[&skn=<KeyName>]
            string token = string.Format(
                CultureInfo.InvariantCulture,
                "SharedAccessSignature sr={0}&sig={1}&se={2}",
                WebUtility.UrlEncode(resourceUri),
                WebUtility.UrlEncode(signature),
                expiry);

            if (!string.IsNullOrWhiteSpace(policyName))
            {
                token += "&skn=" + policyName;
            }

            return token;
        }
    }
}
