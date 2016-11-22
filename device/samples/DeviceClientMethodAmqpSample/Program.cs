// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Samples
{
    using System;
    using System.Text;

    class Program
    {
        // String containing Hostname, Device Id & Device Key in one of the following formats:
        // "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        // "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";
        private const string DeviceConnectionString = "<replace>";

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

        static MethodCallbackReturn WriteToConsole(byte[] payload, object userContext)
        {
            Console.WriteLine();
            Console.WriteLine("\t{0}", Encoding.UTF8.GetString(payload));
            Console.WriteLine();

            return new MethodCallbackReturn(new byte[0], 200);
        }

        static MethodCallbackReturn GetDeviceName(byte[] payload, object userContext)
        {
            MethodCallbackReturn retValue;
            var userData = userContext as DeviceData;
            if (userData == null)
            {
                retValue = new MethodCallbackReturn(new byte[0], 500);
            }
            else
            {
                string result = "{\"name\":\"" + userData.Name + "\"}";
                retValue = new MethodCallbackReturn(Encoding.UTF8.GetBytes(result), 200);
            }
            return retValue;
        }

        static void Main(string[] args)
        {
            DeviceClient deviceClient = null;
            try
            {
                deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Amqp);

                deviceClient.OpenAsync().Wait();

                // Method Call processing will be enabled when the first method handler is added.
                // setup a callback for the 'WriteToConsole' method
                deviceClient.SetMethodHandler("WriteToConsole", WriteToConsole, null);

                // setup a callback for the 'GetDeviceName' method
                deviceClient.SetMethodHandler("GetDeviceName", GetDeviceName, new DeviceData("DeviceClientMethodAmqpSample"));
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
            deviceClient?.SetMethodHandler("WriteToConsole", null, null);

            // remove the 'GetDeviceName' handler
            // Method Call processing will be disabled when the last method handler has been removed .
            deviceClient?.SetMethodHandler("GetDeviceName", null, null);
        }
    }
}
