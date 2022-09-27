// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using CommandLine;
using Microsoft.Azure.Devices;

namespace RoleBasedAuthenticationSample
{
    public class Program
    {
        /// <summary>
        /// A sample to illustrate how to use Azure active directory for authentication to the IoT hub.
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

            // Initialize Azure active directory credentials.
            Console.WriteLine("Creating token credential.");

            // These environment variables are necessary for DefaultAzureCredential to use application Id and client secret to login.
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", parameters.ClientSecret);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", parameters.ClientId);
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", parameters.TenantId);

            // DefaultAzureCredential supports different authentication mechanisms and determines the appropriate credential type based of the environment it is executing in.
            // It attempts to use multiple credential types in an order until it finds a working credential.
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/azure.identity?view=azure-dotnet.
            TokenCredential tokenCredential = new DefaultAzureCredential();

            var options = new IotHubServiceClientOptions
            {
                Protocol = parameters.Protocol,
            };
            using var serviceClient = new IotHubServiceClient(parameters.HostName, tokenCredential, options);

            await RoleBasedAuthenticationSample.RunSampleAsync(serviceClient, parameters.DeviceId);
        }
    }
}
