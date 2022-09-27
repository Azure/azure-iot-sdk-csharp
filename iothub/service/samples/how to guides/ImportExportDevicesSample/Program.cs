// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommandLine;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// This program illustrates how to import and export large numbers of devices and configurations where otherwise
    /// using direct APIs would be time consuming and involve rate limiting other activity to the IoT hub.
    /// </summary>
    /// <remarks>
    /// Using import and export is ideal for backing up an IoT hub's device and configurations registry, or to migrate
    /// them to another IoT hub.
    /// <para>
    /// This application will do the following:
    /// <list type="number">
    /// <item>
    /// <term>Create new devices and add them to an IoT hub (for testing) -- you specify how many you want to add</term>
    /// <description>
    /// This has been tested up to 500,000 devices, but should work all the way up to the million devices allowed on a hub.
    /// </description>
    /// </item>
    /// <item>
    /// <term>Copy the items from one hub to another.</term>
    /// <description>This involves exporting from one into storage, and then importing from storage into the other.</description>
    /// </item>
    /// <item>
    /// <term>Delete the items from the IoT hub(s).</term>
    /// <description>This option is to clean up the hubs after the sample has finished.</description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// Be advised: The size of the hubs you are using should be able to manage the number of devices
    ///  you want to create and test with.
    /// </para>
    /// </remarks>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Parse application parameters
            Parameters parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(
                    parsedParams =>
                    {
                        parameters = parsedParams;
                    })
                .WithNotParsed(
                    errors =>
                    {
                        Environment.Exit(1);
                    });

            if (!parameters.Validate())
            {
                Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            try
            {
                // Instantiate the class and run the sample.
                var importExportDevicesSample = new ImportExportDevicesSample(
                    parameters.SourceIotHubConnectionString,
                    parameters.DestIotHubConnectionString,
                    parameters.StorageConnectionString,
                    parameters.ContainerName,
                    parameters.BlobNamePrefix);

                await importExportDevicesSample
                    .RunSampleAsync(
                        parameters.AddDevices,
                        parameters.IncludeConfigurations,
                        parameters.CopyDevices,
                        parameters.DeleteSourceDevices,
                        parameters.DeleteDestDevices);
            }
            catch (Exception ex)
            {
                Debug.Print($"Error. Description = {ex.Message}");
                Console.WriteLine($"Error. Description = {ex.Message}\n{ex.StackTrace}");
            }

            Console.WriteLine("Sample finished.");
        }
    }
}
