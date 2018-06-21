// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class FileUploadSample
    {
        // The file to upload.
        private const string FilePath = "TestPayload.txt";
        private DeviceClient _deviceClient;

        public FileUploadSample(DeviceClient deviceClient)
        {
            _deviceClient = deviceClient;
        }

        public async Task RunSampleAsync()
        {
            using (var fileStreamSource = new FileStream(FilePath, FileMode.Open))
            {
                var fileName = Path.GetFileName(fileStreamSource.Name);

                Console.WriteLine("Uploading File: {0}", fileName);

                var watch = System.Diagnostics.Stopwatch.StartNew();

                // Note: UploadToBlobAsync will use HTTPS as protocol, regardless of the DeviceClient protocol selection.
                await _deviceClient.UploadToBlobAsync(fileName, fileStreamSource).ConfigureAwait(false);
                watch.Stop();

                Console.WriteLine("Time to upload file: {0}ms\n", watch.ElapsedMilliseconds);
            }
        }
    }
}
