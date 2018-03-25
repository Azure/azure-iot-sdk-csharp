// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class Program
    {
        // String containing Hostname, Device Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";
        // Either set the IOTHUB_DEVICE_CONN_STRING environment variable or within launchSettings.json:
        private static string DeviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING");
        private const string FilePath = "TestPayload.txt";

        public static void Main(string[] args)
        {
            SendToBlobSample().Wait();
        }

        public static async Task SendToBlobSample()
        {
            var deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Http1);
            var fileStreamSource = new FileStream(FilePath, FileMode.Open);
            var fileName = Path.GetFileName(fileStreamSource.Name);

            Console.WriteLine("Uploading File: {0}", fileName);

            var watch = System.Diagnostics.Stopwatch.StartNew();
            await deviceClient.UploadToBlobAsync(fileName, fileStreamSource).ConfigureAwait(false);
            watch.Stop();

            Console.WriteLine("Time to upload file: {0}ms\n", watch.ElapsedMilliseconds);
        }
    }
}
