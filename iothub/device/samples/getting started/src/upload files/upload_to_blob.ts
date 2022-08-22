// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS EXAMPLE IS DEPRECATED
// We recommend you follow the 'upload_to_blob_advanced.ts' sample.

import { Client } from 'azure-iot-device';
import { Mqtt as Protocol } from 'azure-iot-device-mqtt';
import * as fs from 'fs';

const deviceConnectionString: string =  process.env.IOTHUB_DEVICE_CONNECTION_STRING || '';
const filePath: string = process.env.PATH_TO_FILE || '';

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

const client: Client = Client.fromConnectionString(
  deviceConnectionString,
  Protocol
);

fs.stat(filePath, function (err: any, fileStats: fs.Stats): void {
  const fileStream = fs.createReadStream(filePath);

  if (err) {
    console.error('error with fs.stat: ' + err.message);
  }

  client.uploadToBlob('testblob.txt', fileStream, fileStats.size).catch((error: Error) => {
      console.log('error uploading file: ' + error.message);
    }).finally(() => {
      fileStream.destroy();
      process.exit();
    });
});
