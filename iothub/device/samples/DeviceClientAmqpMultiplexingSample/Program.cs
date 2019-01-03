// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    class Program
    {
        // String containing Hostname, Device Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";
        // Either set the IOTHUB_DEVICE_CONN_STRING environment variable or within launchSettings.json:
        //private static string DeviceConnectionString = "HostName=z-test.azure-devices.net;DeviceId=z-001;SharedAccessKey=FCb7RGgBvXdHdL/X1fAG4Wink/lndApCCQdh7Dkzc1U=";

        private static int MESSAGE_COUNT = 5;
        private const  int TEMPERATURE_THRESHOLD = 30;
        private static String deviceId = "MyCSharpDevice";
        private static float temperature;
        private static float humidity;
        private static Random rnd = new Random();

        static async Task Main(string[] args)
        {
            var hostName = "z-test.azure-devices.net";

            var deviceId1 = "z-001";
            var primaryKey1 = "FCb7RGgBvXdHdL/X1fAG4Wink/lndApCCQdh7Dkzc1U=";

            //var deviceId2 = "z-002";
            //var primaryKey2 = "sLZ0GiDJL9ef0Nk9FUkSxsHPpnSKYdorVkiDD0tF6qc=";

            //var deviceId3 = "z-x509";
            //string envValue = Environment.GetEnvironmentVariable("IOTHUB_X509_PFX_CERTIFICATE");
            //string certBase64 = Environment.ExpandEnvironmentVariables(envValue);

            //if (certBase64 == null)
            //{
            //    Console.WriteLine("X509 ERROR");
            //    Environment.Exit(2);
            //}

            //Byte[] buff = Convert.FromBase64String(certBase64);

            //if (password == null)
            //{
            //    return new X509Certificate2(buff);
            //}
            //else
            //{
            //    return new X509Certificate2(buff, password);
            //}

            var auth1 = new DeviceAuthenticationWithRegistrySymmetricKey(deviceId1, primaryKey1);
            //var auth2 = new DeviceAuthenticationWithRegistrySymmetricKey(deviceId2, primaryKey2);
            //var auth3 = new DeviceAuthenticationWithX509Certificate(deviceId3, new X509Certificate2(buff));

            var transportSettings = new ITransportSettings[]
            {
                new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
                {
                    //AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                    //{
                    //    Pooling = true,
                    //    MaxPoolSize = 1
                    //}
                }
            };

            DeviceClient deviceClient1 = DeviceClient.Create(hostName, auth1, transportSettings);
            //DeviceClient deviceClient2 = DeviceClient.Create(hostName, auth2, transportSettings);
            //DeviceClient deviceClient3 = DeviceClient.Create(hostName, auth3, transportSettings);

            if (
                (deviceClient1 == null) 
                //||
                //(deviceClient2 == null) || 
                //(deviceClient3 == null)
                )
            {
                Console.WriteLine("Failed to create DeviceClient(s)!");
            }
            else
            {
                await deviceClient1.OpenAsync().ConfigureAwait(true);
                //await deviceClient2.OpenAsync().ConfigureAwait(true);
                //await deviceClient3.OpenAsync().ConfigureAwait(true);

                await SendEvent(deviceClient1).ConfigureAwait(false);
                //await SendEvent(deviceClient2).ConfigureAwait(false);
                //await SendEvent(deviceClient3).ConfigureAwait(false);

                Task task1 = new Task(delegate { ReceiveCommands(deviceClient1); });
                //Task task2 = new Task(delegate { ReceiveCommands(deviceClient2); });
                //Task task3 = new Task(delegate { ReceiveCommands(deviceClient3); });

                task1.Start();
                //task2.Start();
                //task3.Start();

                Console.ReadKey();
            }

            Console.WriteLine("Exited!\n");
        }

        static async Task SendEvent(DeviceClient deviceClient)
        {
            string dataBuffer;

            Console.WriteLine("Device sending {0} messages to IoTHub...\n", MESSAGE_COUNT);

            for (int count = 0; count < MESSAGE_COUNT; count++)
            {
                temperature = rnd.Next(20, 35);
                humidity = rnd.Next(60, 80);
                dataBuffer = string.Format("{{\"deviceId\":\"{0}\",\"messageId\":{1},\"temperature\":{2},\"humidity\":{3}}}", deviceId, count, temperature, humidity);
                Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                eventMessage.Properties.Add("temperatureAlert", (temperature > TEMPERATURE_THRESHOLD) ? "true" : "false");
                Console.WriteLine("\t{0}> Sending message: {1}, Data: [{2}]", DateTime.Now.ToLocalTime(), count, dataBuffer);

                await deviceClient.SendEventAsync(eventMessage).ConfigureAwait(false);
            }
        }

        static async void ReceiveCommands(DeviceClient deviceClient)
        {
            Console.WriteLine("\nDevice waiting for commands from IoTHub...\n");
            Message receivedMessage;
            string messageData;

            while (true)
            {
                receivedMessage = await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

                if (receivedMessage != null)
                {
                    messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    Console.WriteLine("\t{0}> Received message: {1}", DateTime.Now.ToLocalTime(), messageData);

                    int propCount = 0;
                    foreach (var prop in receivedMessage.Properties)
                    {
                        Console.WriteLine("\t\tProperty[{0}> Key={1} : Value={2}", propCount++, prop.Key, prop.Value);
                    }

                    await deviceClient.CompleteAsync(receivedMessage).ConfigureAwait(false);
                }
            }
        }
    }
}
