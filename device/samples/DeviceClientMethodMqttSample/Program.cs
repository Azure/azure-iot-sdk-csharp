// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Samples
{
    using System;
    using System.Text;

    class Program
    {
        // String containing Hostname, Device Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";
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
            Console.WriteLine("\t{0}", payload);
            Console.WriteLine();

            return MethodCallbackReturn.MethodCallbackReturnFactory(new byte[0], 200);
        }

        static MethodCallbackReturn GetDeviceName(byte[] payload, object userContext)
        {
            MethodCallbackReturn retValue;
            if (userContext == null)
            {
                retValue = MethodCallbackReturn.MethodCallbackReturnFactory(new byte[0], 500);
            }
            else
            {
                var d = userContext as DeviceData;
                string result = "{\"name\":\"" + d.Name + "\"}";
                retValue = MethodCallbackReturn.MethodCallbackReturnFactory(Encoding.UTF8.GetBytes(result), 200);
            }
            return retValue;
        }

        static void Main(string[] args)
        {
            try
            {
                DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Mqtt);

                deviceClient.OpenAsync().Wait();
                deviceClient.EnableMethodsAsync().Wait();

                deviceClient.SetMethodDelegate("WriteToConsole", WriteToConsole, null);
                deviceClient.SetMethodDelegate("GetDeviceName", GetDeviceName, new DeviceData("DeviceClientMethodMqttSample"));

                Console.WriteLine("Exited!");
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
            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }
    }
}
