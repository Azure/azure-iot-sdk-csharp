// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class MethodSample
    {
        private DeviceClient _deviceClient;

        private class DeviceData
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

        public MethodSample(DeviceClient deviceClient)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
        }

        public async Task RunSampleAsync()
        {
            _deviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);

            // Method Call processing will be enabled when the first method handler is added.
            // Setup a callback for the 'WriteToConsole' method.
            await _deviceClient.SetMethodHandlerAsync(nameof(WriteToConsole), WriteToConsole, null).ConfigureAwait(false);

            // Setup a callback for the 'GetDeviceName' method.
            await _deviceClient.SetMethodHandlerAsync(nameof(GetDeviceName), GetDeviceName, new DeviceData("DeviceClientMethodSample")).ConfigureAwait(false);

            Console.WriteLine("Waiting 30 seconds for IoT Hub method calls ...");

            Console.WriteLine($"Use the IoT Hub Azure Portal to call methods {nameof(GetDeviceName)} or {nameof(WriteToConsole)} within this time.");
            await Task.Delay(30 * 1000).ConfigureAwait(false);
        }

        private void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            Console.WriteLine();
            Console.WriteLine("Connection Status Changed to {0}", status);
            Console.WriteLine("Connection Status Changed Reason is {0}", reason);
            Console.WriteLine();
        }

        private Task<MethodResponse> WriteToConsole(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t *** {nameof(WriteToConsole)} was called.");

            Console.WriteLine();
            Console.WriteLine("\t{0}", methodRequest.DataAsJson);
            Console.WriteLine();

            return Task.FromResult(new MethodResponse(new byte[0], 200));
        }

        private Task<MethodResponse> GetDeviceName(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t *** {nameof(GetDeviceName)} was called.");

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
    }
}
