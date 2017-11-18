using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using System.Threading;

namespace multiplexingDotNetCore
{
    class Program
    {
        // String containing IotHub Hostname:
        //"hostname.azure-devices.net"
        private const string IoTHubHostName = "<iothubhostname>.azure-devices.net";

        //Number of devices for multiplexing
        private static int MUX_DEVICES = 4;
        private static DeviceClient[] deviceClients = new DeviceClient[MUX_DEVICES];

        //Array of device Ids (should have the same entries as defined on MUX_DEVICES):
        //{"device1", "device2", "device3", "device4"}
        private static String[] deviceIds = new String[] { "<device1>", "<device2>", "<device3>", "<device4>" };

        //Array of device PrimaryKeys (following the same order as above):
        //{"PrimaryKey device1", "PrimaryKey device2", "PrimaryKey device3", "PrimaryKey device4"}
        private static String[] devicePrimaryKeys = new String[] { "<PrimaryKey device1>",
                                                                    "<PrimaryKey device2>",
                                                                    "<PrimaryKey device3>",
                                                                    "<PrimaryKey device4>"    };

        private static int MESSAGE_COUNT = 5;
        private const int TEMPERATURE_THRESHOLD = 30;

        private static float temperature;
        private static float humidity;
        private static Random rnd = new Random();

        static void Main(string[] args)
        {

            try
            {
                MultiplexConnection().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }

            Console.WriteLine("Exited!\n");
        }

        static async Task MultiplexConnection()
        {
            for (int i = 0; i < 4; i++)
            {
                var auth = new DeviceAuthenticationWithRegistrySymmetricKey(deviceIds[i], devicePrimaryKeys[i]);
                deviceClients[i] = DeviceClient.Create(
                    IoTHubHostName,
                    auth,
                    new ITransportSettings[]
                    {
                        //Must specify Amqp_WebSocket_Only or Amqp_Tcp_Only
                        new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
                        {
                            //By default, connection pooling is turned off. It must be turned on explicitly using the below settings.
                            AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                            {
                                Pooling = true,
                                //If you would like to have only one TCP Connection set MaxPoolSize=1
                                MaxPoolSize = 2
                            }
                        }
                    });
                await deviceClients[i].OpenAsync();
            }

            SendEvent().Wait();
            ReceiveCommands().Wait();
        }

        static async Task SendEvent()
        {
            string dataBuffer;

            Console.WriteLine("Devices sending {0} messages to IoTHub...\n", MESSAGE_COUNT);

            for (int count = 0; count < MESSAGE_COUNT; count++)
            {
                for (int i = 0; i < MUX_DEVICES; i++)
                {
                    temperature = rnd.Next(20, 35);
                    humidity = rnd.Next(60, 80);
                    dataBuffer = string.Format("{{\"deviceId\":\"{0}\",\"messageId\":{1},\"temperature\":{2},\"humidity\":{3}}}", deviceIds[i], count, temperature, humidity);
                    Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                    eventMessage.Properties.Add("temperatureAlert", (temperature > TEMPERATURE_THRESHOLD) ? "true" : "false");
                    Console.WriteLine("\t{0}> Sending message: {1}, Data: [{2}]", DateTime.Now.ToLocalTime(), count, dataBuffer);
                    try
                    {
                        await deviceClients[i].SendEventAsync(eventMessage);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error in sending message: {0}", ex.Message);
                    }
                    Thread.Sleep(1000);
                }
            }
        }

        static async Task ReceiveCommands()
        {
            Console.WriteLine("\nDevices waiting for commands from IoTHub...\n");
            Message receivedMessage;
            string messageData;

            while (true)
            {
                for (int i = 0; i < MUX_DEVICES; i++)
                {
                    receivedMessage = await deviceClients[i].ReceiveAsync(TimeSpan.FromSeconds(1));
                    if (receivedMessage != null)
                    {
                        messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                        Console.WriteLine("\t{0}> {1} Received message: {2}", DateTime.Now.ToLocalTime(), deviceIds[i], messageData);

                        int propCount = 0;
                        foreach (var prop in receivedMessage.Properties)
                        {
                            Console.WriteLine("\t\tProperty[{0}> Key={1} : Value={2}", propCount++, prop.Key, prop.Value);
                        }

                        await deviceClients[i].CompleteAsync(receivedMessage);
                    }
                }
            }
        }
    }
}