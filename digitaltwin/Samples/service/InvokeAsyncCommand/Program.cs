using Microsoft.Azure.Devices.DigitalTwin.Service;
using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Sample
{
    /// <summary>
    /// Sample code that demonstrates how to invoke an async command on a digital twin and how to listen for async command updates
    /// </summary>
    class Program
    {
        private static string IOTHUB_CONNECTION_STRING = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
        private static string DEVICE_ID = Environment.GetEnvironmentVariable("DEVICE_ID");
        private static string INTERFACE_INSTANCE_NAME = Environment.GetEnvironmentVariable("INTERFACE_INSTANCE_NAME");
        private static string ASYNC_COMMAND_NAME = Environment.GetEnvironmentVariable("ASYNC_COMMAND_NAME");
        private static string EVENTHUB_CONNECTION_STRING = Environment.GetEnvironmentVariable("EVENTHUB_CONNECTION_STRING");

        //optional
        private static string PAYLOAD = Environment.GetEnvironmentVariable("PAYLOAD");

        private static string COMMAND_REQUEST_ID_PROPERTY_NAME = "iothub-command-request-id";

        public static int Main(string[] args)
        {
            verifyInputs();

            var digitalTwinServiceClient = new DigitalTwinServiceClient(IOTHUB_CONNECTION_STRING);

            Console.WriteLine("Invoking " + ASYNC_COMMAND_NAME + " on device " + DEVICE_ID + " with interface instance name " + INTERFACE_INSTANCE_NAME);

            var digitalTwinCommandResponse = digitalTwinServiceClient.InvokeCommand(DEVICE_ID, INTERFACE_INSTANCE_NAME, ASYNC_COMMAND_NAME, PAYLOAD).Value;

            Console.WriteLine("Command " + ASYNC_COMMAND_NAME + " invoked on the device successfully, the returned status was " + digitalTwinCommandResponse.Status + " and the request id was " + digitalTwinCommandResponse.RequestId);
            Console.WriteLine("The returned payload was ");
            Console.WriteLine(digitalTwinCommandResponse.Payload);
           
            // Status updates for this command will arrive as telemetry containing the returned request id
            listenForAsyncCommandUpdates(digitalTwinCommandResponse.RequestId);     

            return 0;
        }

        private static void listenForAsyncCommandUpdates(string requestId)
        {
            var connectionStringBuilder = new EventHubsConnectionStringBuilder(EVENTHUB_CONNECTION_STRING);

            var partitionReceivers = new List<PartitionReceiver>();
            var eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());
            var eventhubReceiverThreads = new List<Thread>();

            EventHubRuntimeInformation runtimeInformation = eventHubClient.GetRuntimeInformationAsync().Result;

            //spawn a thread per partition to receive over that partition
            Console.WriteLine("Setting up EventHub listeners for updates to the async method...");
            foreach (string partitionId in runtimeInformation.PartitionIds)
            {
                var partitionReceiver = eventHubClient.CreateReceiver(PartitionReceiver.DefaultConsumerGroupName, partitionId, EventPosition.FromEnqueuedTime(DateTime.Now));

                Thread partitionReceiverThread = new Thread(() => receive(partitionReceiver, requestId));
                eventhubReceiverThreads.Add(partitionReceiverThread);
                partitionReceiverThread.Start();
            }

            Console.WriteLine("EventHub listener now listening for updates containing the request id " + requestId);
            Console.WriteLine();

            //Wait for user to enter a key to exit the program
            Console.ReadLine();

            foreach (Thread partitionReceiverThread in eventhubReceiverThreads)
            {
                partitionReceiverThread.Interrupt();
            }
        }

        private static void receive(PartitionReceiver partitionReceiver, string requestId)
        {
            try
            {
                while (true)
                {
                    var receiveTask = partitionReceiver.ReceiveAsync(1, TimeSpan.FromSeconds(10));
                    receiveTask.Wait();
                    IEnumerable<EventData> receivedEvents = receiveTask.Result;

                    if (receivedEvents != null)
                    {
                        foreach (EventData eventData in receivedEvents)
                        {
                            if (eventData != null)
                            {
                                if (eventData.Properties != null && eventData.Properties.ContainsKey(COMMAND_REQUEST_ID_PROPERTY_NAME))
                                {
                                    string payload = Encoding.UTF8.GetString(eventData.Body);
                                    Console.WriteLine("Received an update on the async command:");
                                    Console.WriteLine();
                                    foreach (string propertyKey in eventData.Properties.Keys)
                                    {
                                        Console.WriteLine("    " + propertyKey + ":" + eventData.Properties[propertyKey]);
                                    }
                                    Console.WriteLine();
                                    Console.WriteLine("    " + "Update Payload: ");
                                    Console.WriteLine("        " + payload);
                                    Console.WriteLine();

                                    if (payload.Contains("100%"))
                                    {
                                        Console.WriteLine("Async command has finished, enter any key to finish\n");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (ThreadInterruptedException e)
            {
                //Thread was aborted, so allow it to end
                partitionReceiver.Close();
            }
        }

        private static void verifyInputs()
        {
            if (isNullOrEmpty(IOTHUB_CONNECTION_STRING) || 
                isNullOrEmpty(DEVICE_ID) || 
                isNullOrEmpty(INTERFACE_INSTANCE_NAME) || 
                isNullOrEmpty(ASYNC_COMMAND_NAME) || 
                isNullOrEmpty(EVENTHUB_CONNECTION_STRING))
            {
                Console.WriteLine(usage);
                Console.WriteLine("Enter any key to finish");
                Console.ReadLine();
                Environment.Exit(0);
            }

            if (string.IsNullOrEmpty(PAYLOAD))
            {
                PAYLOAD = null;
            }
        }

        private static bool isNullOrEmpty(String s)
        {
            return s == null || s.Length == 0;
        }

        private static String usage = "In order to run this sample, you must set environment variables for \n" +
                "IOTHUB_CONNECTION_STRING - Your IoT Hub's connection string\n" +
                "DEVICE_ID - The ID of the device to invoke the command onto\n" +
                "INTERFACE_INSTANCE_NAME - The interface the command belongs to\n" +
                "ASYNC_COMMAND_NAME - The name of the command to invoke on your digital twin\n" +
                "EVENTHUB_CONNECTION_STRING - The connection string to the EventHub associated to your IoT Hub\n" +
                "PAYLOAD - (optional) The json payload to include in the command";
    }
}
