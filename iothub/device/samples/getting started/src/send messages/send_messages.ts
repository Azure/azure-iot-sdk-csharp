// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Choose a protocol by uncommenting one of these transports.
import { Mqtt as Protocol } from 'azure-iot-device-mqtt';
// import { Amqp as Protocol } from 'azure-iot-device-amqp';
// import { Http as Protocol } from 'azure-iot-device-Http';
// import { MqttWs as Protocol } from 'azure-iot-device-mqtt';
// import { AmqpWs as Protocol } from 'azure-iot-device-amqp';

import { Client, Message } from 'azure-iot-device';

const deviceConnectionString: string = process.env.IOTHUB_DEVICE_CONNECTION_STRING || '';
let sendInterval: NodeJS.Timeout;

if (deviceConnectionString === '') {
  console.log('device connection string not set');
  process.exit(-1);
}

const client: Client = Client.fromConnectionString(deviceConnectionString, Protocol);

async function asyncMain(): Promise<void> {
  client.on('connect', connectHandler);
  client.on('error', errorHandler);
  client.on('disconnect', disconnectHandler);
  client.on('message', messageHandler);

  client.open().catch((err) => {
    console.error('Could not connect: ' + err.message);
  });
}

function disconnectHandler(): void {
  clearInterval(sendInterval);

  client.open().catch((err) => {
    console.error(err.message);
  });
}

function connectHandler(): void {
  console.log('Client connected');
  // Create a message and send it to the IoT Hub every two seconds
  if (!sendInterval) {
    sendInterval = setInterval(() => {
      const message = generateMessage();
      console.log('Sending message: ' + message.getData());
      client.sendEvent(message, printResultFor('send'));
    }, 2000);
  }
}

function messageHandler(msg: any): void {
  console.log('Id: ' + msg.messageId + ' Body: ' + msg.data);
  client.complete(msg, printResultFor('completed'));
}

function errorHandler(err: any): void {
  console.error(err.message);
}

function printResultFor(op: any): (err: any, res: any) => void {
  return function printResult(err: any, res: any): void {
    if (err) console.log(op + ' error: ' + err.toString());
    if (res) console.log(op + ' status: ' + res.constructor.name);
  };
}

function generateMessage(): Message {
  const windSpeed: number = 10 + Math.random() * 4; // range: [10, 14]
  const temperature: number = 20 + Math.random() * 10; // range: [20, 30]
  const humidity: number = 60 + Math.random() * 20; // range: [60, 80]
  const data: string = JSON.stringify({ deviceId: 'myFirstDevice', windSpeed: windSpeed, temperature: temperature, humidity: humidity });
  const message: Message = new Message(data);

  message.properties.add('temperatureAlert', temperature > 28 ? 'true' : 'false' );

  return message;
}

asyncMain().catch((err: any): void => {
  console.log('error code: ', err.code);
  console.log('error message: ', err.message);
  console.log('error stack: ', err.stack);
});
