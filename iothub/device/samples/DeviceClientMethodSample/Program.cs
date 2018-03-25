// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Samples
{
    using System;
    using System.Text;
    using System.Threading.Tasks;

    class Program
    {
        // String containing Hostname, Device Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";
        // Either set the IOTHUB_DEVICE_CONN_STRING environment variable or within launchSettings.json:
        private static string DeviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING");

        class DeviceData
        {
            public DeviceData(string myName)
            {
                this.Name = myName;
            }

            public string Name
            {
                get; set;
            }
        }
        static void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            Console.WriteLine();
            Console.WriteLine("Connection Status Changed to {0}", status);
            Console.WriteLine("Connection Status Changed Reason is {0}", reason);
            Console.WriteLine();
        }

        static Task<MethodResponse> WriteToConsole(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine();
            Console.WriteLine("\t{0}", methodRequest.DataAsJson);
            Console.WriteLine();

            return Task.FromResult(new MethodResponse(new byte[0], 200));
        }

        static Task<MethodResponse> GetDeviceName(MethodRequest methodRequest, object userContext)
        {
            MethodResponse retValue;
            if (userContext == null)
            {
                retValue = new MethodResponse(new byte[0], 500);
            }
            else
            {
                var d = userContext as DeviceData;
                string result = "{\"name\":\"" + d.Name + "\"}";
                retValue = new MethodResponse(Encoding.UTF8.GetBytes(result), 200);
            }
            return Task.FromResult(retValue);
        }

        static void Main(string[] args)
        {
            TransportType transport = TransportType.Mqtt;

            if (args.Length == 1 && args[0].ToLower().Equals("amqp"))
            {
                transport = TransportType.Amqp;
            }

            DeviceClient deviceClient = null;
            try
            {
                deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, transport);

                deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);

                // Method Call processing will be enabled when the first method handler is added.
                // setup a callback for the 'WriteToConsole' method
                deviceClient.SetMethodHandlerAsync("WriteToConsole", WriteToConsole, null).Wait();

                // setup a calback for the 'GetDeviceName' method
                deviceClient.SetMethodHandlerAsync("GetDeviceName", GetDeviceName, new DeviceData("DeviceClientMethodSample")).Wait();
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error in sample: {0}", exception);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
            Console.WriteLine("Waiting for incoming subscribed Methods call.  Press enter to exit.");

            Console.ReadLine();
            Console.WriteLine("Exiting...");

            // remove the 'WriteToConsole' handler
            deviceClient?.SetMethodHandlerAsync("WriteToConsole", null, null).Wait();

            // remove the 'GetDeviceName' handler
            // Method Call processing will be disabled when the last method handler has been removed .
            deviceClient?.SetMethodHandlerAsync("GetDeviceName", null, null).Wait();

            deviceClient?.CloseAsync().Wait();
        }
    }
}
