// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using System;
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
        //private const string DeviceConnectionString1 = "HostName=iot-sdks-test.azure-devices.net;DeviceId=TamerRolling;SharedAccessKey=IKbDlm5v1k8DsZL4wej2gcc0hU52qiP6Rg3QPtUV9EY=";
        private const string DeviceConnectionString1 = "<replace_with_connection_string_based_on_primary_key>";
        private const string DeviceConnectionString2 = "<replace_with_connection_string_based_on_secondary_key>";

        private static string DeviceConnectionString = DeviceConnectionString1;

        private static int MESSAGE_COUNT = 5;

        private static DeviceClient deviceClient;
        private static bool isRollOverInvoked = false;

        static void Main(string[] args)
        {
            try
            {
                MainAsync().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        static async Task MainAsync()
        {
            try
            {
                deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString);

                //await SendEvent(deviceClient);
                await ReceiveCommands(deviceClient);
            }
            catch (UnauthorizedException ex)
            {
                Console.WriteLine("UnauthorizedExpception:\n" + ex.Message);
                if (!isRollOverInvoked)
                {
                    isRollOverInvoked = true;
                    DeviceConnectionString = DeviceConnectionString2;
                    await MainAsync();
                }
                else
                {
                    throw ex;
                }
            }
        }

        static async Task SendEvent(DeviceClient deviceClient)
        {
            try
            {
                string dataBuffer;

                Console.WriteLine("Device sending {0} messages to IoTHub...\n", MESSAGE_COUNT);

                for (int count = 0; count < MESSAGE_COUNT; count++)
                {
                    dataBuffer = Guid.NewGuid().ToString();
                    Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                    Console.WriteLine("\t{0}> Sending message: {1}, Data: [{2}]", DateTime.Now.ToLocalTime(), count, dataBuffer);

                    await deviceClient.SendEventAsync(eventMessage);
                }

            }
            catch (UnauthorizedException)
            {
                throw;
            }
        }

        static async Task ReceiveCommands(DeviceClient deviceClient)
        {
            try
            {
                Console.WriteLine("\nDevice waiting for commands from IoTHub...\n");
                Message receivedMessage;
                string messageData;

                while (true)
                {
                    receivedMessage = await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(1));

                    if (receivedMessage != null)
                    {
                        messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                        Console.WriteLine("\t{0}> Received message: {1}", DateTime.Now.ToLocalTime(), messageData);

                        int propCount = 0;
                        foreach (var prop in receivedMessage.Properties)
                        {
                            Console.WriteLine("\t\tProperty[{0}> Key={1} : Value={2}", propCount++, prop.Key, prop.Value);
                        }

                        await deviceClient.CompleteAsync(receivedMessage);
                    }
                }
            }
            catch (UnauthorizedException)
            {
                throw;
            }
        }
    }
}
