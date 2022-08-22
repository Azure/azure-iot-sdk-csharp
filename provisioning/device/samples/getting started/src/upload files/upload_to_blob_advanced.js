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

'use strict';

// Choose a protocol by uncommenting one of these transports.
const Protocol = require('azure-iot-device-mqtt').Mqtt;
// const Protocol = require('azure-iot-device-amqp').Amqp;
// const Protocol = require('azure-iot-device-http').Http;
// const Protocol = require('azure-iot-device-mqtt').MqttWs;
// const Protocol = require('azure-iot-device-amqp').AmqpWs;

const Client = require('azure-iot-device').Client;
const errors = require('azure-iot-common').errors;

const {AnonymousCredential, BlockBlobClient, newPipeline } = require('@azure/storage-blob');

// make sure you set these environment variables prior to running the sample.
const deviceConnectionString = process.env.IOTHUB_DEVICE_CONNECTION_STRING;
const localFilePath = process.env.PATH_TO_FILE;
const storageBlobName = 'testblob.txt';

async function uploadToBlob(localFilePath, client) {
  const blobInfo = await client.getBlobSharedAccessSignature(storageBlobName);
  if (!blobInfo) {
    throw new errors.ArgumentError('Invalid upload parameters');
  }

  const pipeline = newPipeline(new AnonymousCredential(), {
    retryOptions: { maxTries: 4 },
    telemetry: { value: 'HighLevelSample V1.0.0' }, // Customized telemetry string
    keepAliveOptions: { enable: false }
  });

  // Construct the blob URL to construct the blob client for file uploads
  const { hostName, containerName, blobName, sasToken } = blobInfo;
  const blobUrl = `https://${hostName}/${containerName}/${blobName}${sasToken}`;

  // Create the BlockBlobClient for file upload to the Blob Storage Blob
  const blobClient = new BlockBlobClient(blobUrl, pipeline);

  // Setup blank status notification arguments to be filled in on success/failure
  let isSuccess;
  let statusCode;
  let statusDescription;

  try {
    const uploadStatus = await blobClient.uploadFile(localFilePath);
    console.log('uploadStreamToBlockBlob success');

    // Save successful status notification arguments
    isSuccess = true;
    statusCode = uploadStatus._response.status;
    statusDescription = uploadStatus._response.bodyAsText;

    // Notify IoT Hub of upload to blob status (success)
    console.log('notifyBlobUploadStatus success');
  }
  catch (err) {
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
uploadToBlob(localFilePath, deviceClient)
  .catch((err) => {
    console.log(err);
  })
  .finally(() => {
    process.exit();
  });
