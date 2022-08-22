// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

'use strict';

// !IMPORTANT! This sample only pertains to HTTPS Proxy, via the HTTP Transport, MQTTWS Transport, and AMQPWS Transport.

// Choose a protocol by uncommenting one of these transports.
const Protocol = require('azure-iot-device-mqtt').MqttWs;
// const Protocol = require('azure-iot-device-amqp').AmqpWs;
// const Protocol = require('azure-iot-device-http').Http;

const Client = require('azure-iot-device').Client;
const Message = require('azure-iot-device').Message;

// Using HTTPS Proxy Agent
const HttpsProxyAgent = require('https-proxy-agent');
const url = require('url');

// String containing Hostname, Device Id & Device Key in the following formats:
//  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
const deviceConnectionString = process.env.IOTHUB_DEVICE_CONNECTION_STRING;
let sendInterval;

function disconnectHandler () {
  clearInterval(sendInterval);
  sendInterval = null;
  client.removeAllListeners();
  client.open().catch((err) => {
    console.error(err.message);
  });
}

// The AMQP and HTTP transports have the notion of completing, rejecting or abandoning the message.
// For example, this is only functional in AMQP and HTTP:
// client.complete(msg, printResultFor('completed'));
// If using MQTT calls to complete, reject, or abandon are no-ops.
// When completing a message, the service that sent the C2D message is notified that the message has been processed.
// When rejecting a message, the service that sent the C2D message is notified that the message won't be processed by the device. the method to use is client.reject(msg, callback).
// When abandoning the message, IoT Hub will immediately try to resend it. The method to use is client.abandon(msg, callback).
// MQTT is simpler: it accepts the message by default, and doesn't support rejecting or abandoning a message.
function messageHandler (msg) {
  console.log('Id: ' + msg.messageId + ' Body: ' + msg.data);
  client.complete(msg, printResultFor('completed'));
}

function generateMessage () {
  const windSpeed = 10 + (Math.random() * 4); // range: [10, 14]
  const temperature = 20 + (Math.random() * 10); // range: [20, 30]
  const humidity = 60 + (Math.random() * 20); // range: [60, 80]
  const data = JSON.stringify({ deviceId: 'myFirstDevice', windSpeed: windSpeed, temperature: temperature, humidity: humidity });
  const message = new Message(data);
  message.properties.add('temperatureAlert', (temperature > 28) ? 'true' : 'false');
  return message;
}

function errorHandler (err) {
  console.error(err.message);
}

function connectHandler () {
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

// Create Proxy Agent
// TODO: You need to change this to match the endpoint of your proxy server.
const proxy = "http://localhost:8888/";
let options = url.parse(proxy);
var agent = new HttpsProxyAgent(options);

// fromConnectionString must specify a transport constructor, coming from any transport package.
let client = Client.fromConnectionString(deviceConnectionString, Protocol);
// MQTTWS (MQTT over Websocket)
client.setOptions({mqtt: {webSocketAgent: agent}});
// AMQPWS (AMQP over Websocket)
// client.setOptions({amqp: {agent: agent}});
// HTTP
// client.setOptions({http: {webSocketAgent: agent}});
client.on('connect', connectHandler);
client.on('error', errorHandler);
client.on('disconnect', disconnectHandler);
client.on('message', messageHandler);

client.open()
.catch(err => {
  console.error('Could not connect: ' + err.message);
});

// Helper function to print results in the console
function printResultFor(op) {
  return function printResult(err, res) {
    if (err) console.log(op + ' error: ' + err.toString());
    if (res) console.log(op + ' status: ' + res.constructor.name);
  };
}
