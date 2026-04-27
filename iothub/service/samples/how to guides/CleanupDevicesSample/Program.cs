// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Storage.Blobs;
using CommandLine;
using Microsoft.Azure.Devices.Client.Samples;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
    {
        /// <summary>
        /// A sample to illustrate bulk devices deletion.
        /// </summary>
        /// <param name="args">
        /// Run with `--help` to see a list of required and optional parameters.
        /// </param>
        public static async Task<int> Main(string[] args)
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

            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(parameters.HubConnectionString);
            var saveDevicesWithPrefix = new List<string> { "Save_" };

            var blobServiceClient = new BlobServiceClient(parameters.StorageAccountConnectionString);
            string blobContainerName = $"cleanupdevice{Guid.NewGuid()}";
            BlobContainerClient blobContainerClient = await blobServiceClient.CreateBlobContainerAsync(blobContainerName);

            var sample = new CleanupDevicesSample(registryManager, blobContainerClient, saveDevicesWithPrefix);
            await sample.RunCleanUpAsync();

            Console.WriteLine("Done.");
            return 0;
        }
    }
}
