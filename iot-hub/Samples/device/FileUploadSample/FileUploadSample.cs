// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Devices.Client.Transport;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class FileUploadSample
    {
        private readonly DeviceClient _deviceClient;

        public FileUploadSample(DeviceClient deviceClient)
        {
            _deviceClient = deviceClient;
        }

        public async Task RunSampleAsync()
        {
            const string filePath = "TestPayload.txt";

            using var fileStreamSource = new FileStream(filePath, FileMode.Open);
            var fileName = Path.GetFileName(fileStreamSource.Name);

            Console.WriteLine($"Uploading file {fileName}");

            var fileUploadTime = Stopwatch.StartNew();
            
            var fileUploadSasUriRequest = new FileUploadSasUriRequest
            {
                BlobName = fileName
            };

            // Note: GetFileUploadSasUriAsync and CompleteFileUploadAsync will use HTTPS as protocol regardless of the DeviceClient protocol selection.
            Console.WriteLine("Getting SAS URI from IoT Hub to use when uploading the file...");
            FileUploadSasUriResponse sasUri = await _deviceClient.GetFileUploadSasUriAsync(fileUploadSasUriRequest);
            Uri uploadUri = sasUri.GetBlobUri();

            Console.WriteLine($"Successfully got SAS URI ({uploadUri}) from IoT Hub");

            try
            {
                Console.WriteLine($"Uploading file {fileName} using the Azure Storage SDK and the retrieved SAS URI for authentication");

                // Note that other versions of the Azure Storage SDK can be used here. For the latest version, see
                // https://github.com/Azure/azure-sdk-for-net/tree/master/sdk/storage#azure-storage-libraries-for-net
                // NOTE: The UploadAsync operation overwrites the contents of the blob, creating a new block blob if none exists.
                // Overwriting an existing block blob replaces any existing metadata on the blob.
                // Set <see href="https://docs.microsoft.com/en-us/rest/api/storageservices/specifying-conditional-headers-for-blob-service-operations">
                // access conditions through BlobRequestConditions to avoid overwriting existing data.
                // See https://github.com/Azure/azure-sdk-for-net/blob/66d8ab97081a35ebc50c98278110dcac0e4d763e/sdk/storage/Azure.Storage.Blobs/src/BlockBlobClient.cs#L570 for more details.
                var blockBlobClient = new BlockBlobClient(uploadUri);
                await blockBlobClient.UploadAsync(fileStreamSource, new BlobUploadOptions());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to upload file to Azure Storage using the Azure Storage SDK due to {ex}");

                var failedFileUploadCompletionNotification = new FileUploadCompletionNotification
                {
                    // Mandatory. Must be the same value as the correlation id returned in the sas uri response
                    CorrelationId = sasUri.CorrelationId,

                    // Mandatory. Will be present when service client receives this file upload notification
                    IsSuccess = false,

                    // Optional, user-defined status code. Will be present when service client receives this file upload notification
                    StatusCode = 500,

                    // Optional, user defined status description. Will be present when service client receives this file upload notification
                    StatusDescription = ex.Message
                };

                // Note that this is done even when the file upload fails. IoT Hub has a fixed number of SAS URIs allowed active
                // at any given time. Once you are done with the file upload, you should free your SAS URI so that other
                // SAS URIs can be generated. If a SAS URI is not freed through this API, then it will free itself eventually
                // based on how long SAS URIs are configured to live on your IoT Hub.
                await _deviceClient.CompleteFileUploadAsync(failedFileUploadCompletionNotification);
                Console.WriteLine("Notified IoT Hub that the file upload failed and that the SAS URI can be freed");

                fileUploadTime.Stop();
                return;
            }

            Console.WriteLine("Successfully uploaded the file to Azure Storage");

            var successfulFileUploadCompletionNotification = new FileUploadCompletionNotification
            {
                // Mandatory. Must be the same value as the correlation id returned in the sas uri response
                CorrelationId = sasUri.CorrelationId,

                // Mandatory. Will be present when service client receives this file upload notification
                IsSuccess = true,

                // Optional, user defined status code. Will be present when service client receives this file upload notification
                StatusCode = 200,

                // Optional, user-defined status description. Will be present when service client receives this file upload notification
                StatusDescription = "Success"
            };

            await _deviceClient.CompleteFileUploadAsync(successfulFileUploadCompletionNotification);
            Console.WriteLine("Notified IoT Hub that the file upload succeeded and that the SAS URI can be freed.");

            fileUploadTime.Stop();

            Console.WriteLine($"Time to upload file: {fileUploadTime.Elapsed}.");
        }
    }
}
