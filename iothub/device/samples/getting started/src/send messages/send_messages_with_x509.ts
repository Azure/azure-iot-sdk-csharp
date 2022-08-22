// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Choose a protocol by uncommenting one of these transports.
import { Mqtt as Protocol } from 'azure-iot-device-mqtt';
// import { Amqp as Protocol } from 'azure-iot-device-amqp';
// import { Http as Protocol } from 'azure-iot-device-Http';
// import { MqttWs as Protocol } from 'azure-iot-device-mqtt';
// import { AmqpWs as Protocol } from 'azure-iot-device-amqp';

import { Client, Message } from 'azure-iot-device';
import * as fs from 'fs';

// String containing Hostname and Device Id in the following format:
//  "HostName=<iothub_host_name>;DeviceId=<device_id>;x509=true"
const deviceConnectionString: string = process.env.IOTHUB_DEVICE_CONNECTION_STRING || '';
const certFile: string = process.env.PATH_TO_CERTIFICATE_FILE || '';
const keyFile: string = process.env.PATH_TO_KEY_FILE || '';
const passphrase: string = process.env.KEY_PASSPHRASE_OR_EMPTY || ''; // Key Passphrase if one exists.

if (deviceConnectionString === '') {
  console.log('device connection string not set');
  process.exit(-1);
}

if (certFile === '') {
  console.log('path certificate file is not set');
  process.exit(-1);
}

if (keyFile === '') {
  console.log('path to key file is not set');
  process.exit(-1);
}

if (passphrase === '') {
  console.log('Info: passphrase is not set');
  process.exit(-1);
}

// fromConnectionString must specify a transport constructor, coming from any transport package.
const client: Client = Client.fromConnectionString(
  deviceConnectionString,
  Protocol
);

let sendInterval: NodeJS.Timer;
const connectCallback = function (err: any): void {
  if (err) {
    console.error('Could not connect: ' + err.message);
  } else {
    console.log('Client connected');
    client.on('message', function (msg: Message): void {
      console.log('Id: ' + msg.messageId + ' Body: ' + msg.data);
      // When using MQTT the following line is a no-op.
      client.complete(msg, printResultFor('completed'));
      // The AMQP and HTTP transports also have the notion of completing, rejecting or abandoning the message.
      // When completing a message, the service that sent the C2D message is notified that the message has been processed.
      // When rejecting a message, the service that sent the C2D message is notified that the message won't be processed by the device. the method to use is client.reject(msg, callback).
      // When abandoning the message, IoT Hub will immediately try to resend it. The method to use is client.abandon(msg, callback).
      // MQTT is simpler: it accepts the message by default, and doesn't support rejecting or abandoning a message.
    });

    // Create a message and send it to the IoT Hub every second
    if (!sendInterval) {
      sendInterval = setInterval(function (): void {
        const windSpeed: number = 10 + Math.random() * 4; // range: [10, 14]
        const temperature: number = 20 + Math.random() * 10; // range: [20, 30]
        const humidity: number = 60 + Math.random() * 20; // range: [60, 80]
        const data: string = JSON.stringify({
          deviceId: 'myFirstDevice',
          windSpeed: windSpeed,
          temperature: temperature,
          humidity: humidity,
        });
        const message: Message = new Message(data);
        message.properties.add(
          'temperatureAlert',
          temperature > 28 ? 'true' : 'false'
        );
        console.log('Sending message: ' + message.getData());
        client.sendEvent(message, printResultFor('send'));
      }, 2000);
    }

    client.on('error', function (error: Error): void {
      console.error(error.message);
    });

    client.on('disconnect', function (): void {
      clearInterval(sendInterval);
      client.removeAllListeners();
      client.open(connectCallback);
    });
  }
};

const options: { cert: string; key: string; passphrase: string } = {
  cert: fs.readFileSync(certFile, 'utf-8').toString(),
  key: fs.readFileSync(keyFile, 'utf-8').toString(),
  passphrase: passphrase,
};

// Calling setOptions with the x509 certificate and key (and optionally, passphrase) will configure the client transport to use x509 when connecting to IoT Hub
client.setOptions(options);
client.open(connectCallback);

// Helper function to print results in the console
function printResultFor(op: any): (err: any, res: any) => void {
  return function printResult(err: any, res: any): void {
    if (err) console.log(op + ' error: ' + err.toString());
    if (res) console.log(op + ' status: ' + res.constructor.name);
  };
}
