// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { clientFromConnectionString } from 'azure-iot-device-http';
import { Client, Message } from 'azure-iot-device';

// String containing Hostname, Device Id & Device Key in the following formats:
//  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
const deviceConnectionString: string = process.env.IOTHUB_DEVICE_CONNECTION_STRING || '';

if (deviceConnectionString === '') {
  console.log('device connection string not set');
  process.exit(-1);
}

const client: Client = clientFromConnectionString(deviceConnectionString);

// Create two messages and send them to the IoT hub as a batch.
const data: { id: number; message: string }[] = [
  { id: 1, message: 'hello' },
  { id: 2, message: 'world' },
];

const messages: any[] = [];

data.forEach(function (value: { id: number; message: string }): void {
  messages.push(new Message(JSON.stringify(value)));
});

console.log('sending ' + messages.length + ' events in a batch');

client.sendEventBatch(messages, printResultFor('send'));

function printResultFor(op: any): (err: any, res: any) => void {
  return function printResult(err: any, res: any): void {
    // console.log(res);
    if (err) console.log(op + ' error: ' + err.toString());
    if (res) console.log(op + ' status: ' + res.transportObj.statusCode + ' ' + res.transportObj.statusMessage);
  };
}
