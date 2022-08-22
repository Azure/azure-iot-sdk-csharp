// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// UPLOAD TO BLOB ADVANCED SAMPLE
// This is a new api for upload to blob that allows for greater control over the blob upload calls.
// Instead of a single API call that wraps the Storage SDK, the user in this sample retrieves the linked
// Storage Account SAS Token from IoT Hub using a new API call, uses the Azure Storage Blob package to upload the local file to blob storage.
// Additionally - it exposes two new APIs:
//
// getBlobSharedAccessSignature
// > Using a HTTP POST, retrieve a SAS Token for the Storage Account linked to your IoT Hub.
//
// notifyBlobUploadStatus
// > Using HTTP POST, notify IoT Hub of the status of a finished file upload (success/failure).
//
// More information on Uploading Files with IoT Hub can be found here:
// https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-file-upload

// Choose a protocol by uncommenting one of these transports.
import { Mqtt as Protocol } from 'azure-iot-device-mqtt';
// import { Amqp as Protocol } from 'azure-iot-device-amqp';
// import { Http as Protocol } from 'azure-iot-device-Http';
// import { MqttWs as Protocol } from 'azure-iot-device-mqtt';
// import { AmqpWs as Protocol } from 'azure-iot-device-amqp';

import { Client } from 'azure-iot-device';
import { errors } from 'azure-iot-common';
import { AnonymousCredential, BlobUploadCommonResponse, BlockBlobClient, newPipeline, Pipeline } from '@azure/storage-blob';

// make sure you set these environment variables prior to running the sample.
const deviceConnectionString: string = process.env.IOTHUB_DEVICE_CONNECTION_STRING || '';
const filePath: string = process.env.PATH_TO_FILE || '';
const storageBlobName: string = 'testblob.txt';

// check for connection string
if (deviceConnectionString === '') {
  console.log('device connection string not set');
  process.exit(-1);
}

// check for file path
if (filePath === '') {
  console.log('file path is not set');
  process.exit(-1);
}

async function uploadToBlob(localFilePath: string, client: Client): Promise<void> {
  const blobInfo = await client.getBlobSharedAccessSignature(storageBlobName);
  if (!blobInfo) {
    throw new errors.ArgumentError('Invalid upload parameters');
  }

  const pipeline: Pipeline = newPipeline(new AnonymousCredential(), {
    retryOptions: { maxTries: 4 },
    // telemetry: { value: 'HighLevelSample V1.0.0' }, // Customized telemetry string
    keepAliveOptions: { enable: false }
  });

  // Construct the blob URL to construct the blob client for file uploads
  const { hostName, containerName, blobName, sasToken } = blobInfo;
  const blobUrl: string = `https://${hostName}/${containerName}/${blobName}${sasToken}`;

  // Create the BlockBlobClient for file upload to the Blob Storage Blob
  const blobClient = new BlockBlobClient(blobUrl, pipeline);

  // Setup blank status notification arguments to be filled in on success/failure
  let isSuccess: boolean;
  let statusCode: number;
  let statusDescription: string;

  try {
    const uploadStatus: BlobUploadCommonResponse = await blobClient.uploadFile(localFilePath);
    console.log('uploadStreamToBlockBlob success');

    // Save successful status notification arguments
    isSuccess = true;
    statusCode = uploadStatus._response.status;
    statusDescription = 'upload success';

    // Notify IoT Hub of upload to blob status (success)
    console.log('notifyBlobUploadStatus success');
  } catch (err: any) {
    isSuccess = false;
    statusCode = err.code;
    statusDescription = err.message;

    console.log('notifyBlobUploadStatus failed');
    console.log(err);
  }

  await client.notifyBlobUploadStatus(blobInfo.correlationId, isSuccess, statusCode, statusDescription);
}

// Create a client device from the connection string and upload the local file to blob storage.
const deviceClient = Client.fromConnectionString(deviceConnectionString, Protocol);
uploadToBlob(filePath, deviceClient)
  .catch((err) => {
    console.log(err);
  })
  .finally(() => {
    process.exit();
  });
