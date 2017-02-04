// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.Client.Samples
{
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

    class IoTClient
    {
        // String containing Hostname, Device Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";
        private const string DeviceConnectionString = "<replace>";

        DeviceClient deviceClient;
        public TransportType Protocol { get; private set; }

        Action<object> callMeLogger;
        Action<object> getDeviceNameLogger;
        Action<object> errorHandler;

        public IoTClient(TransportType protocol, Action<object> callMeLogger, Action<object> getDeviceNameLogger, Action<object> errorHandler)
        {
            this.Protocol = protocol;
            this.callMeLogger = callMeLogger;
            this.getDeviceNameLogger = getDeviceNameLogger;
            this.errorHandler = errorHandler;
        }

        public async Task Start()
        {
            try
            {
                this.deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, this.Protocol);

                // Set up callbacks:
                try
                {
                    await deviceClient.SetMethodHandlerAsync("CallMe", CallMe, null);
                    await deviceClient.SetMethodHandlerAsync("GetDeviceName", GetDeviceName, new DeviceData("Some UWP Device"));
                }
                catch
                {
                    errorHandler(string.Format("Methods are not supported for protocol {0}", this.Protocol));
                }

                Debug.WriteLine("Exited!\n");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        Task<MethodResponse> CallMe(MethodRequest methodRequest, object userContext)
        {
            this.callMeLogger(methodRequest.DataAsJson);

            return Task.FromResult(new MethodResponse(new byte[0], 200));
        }

        Task<MethodResponse> GetDeviceName(MethodRequest methodRequest, object userContext)
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

            this.getDeviceNameLogger(methodRequest.DataAsJson);

            return Task.FromResult(retValue);
        }

        public Task SendEvent(string message)
        {
            var dataBuffer = string.Format("Msg from UWP: '{0}'. Sent at: {1}. Protocol used: {2}.", message, DateTime.Now.ToLocalTime(), Protocol);
            Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
            Debug.WriteLine("Sending message: '{0}'", dataBuffer);
            return deviceClient.SendEventAsync(eventMessage);
        }

        public async Task<string> ReceiveCommand()
        {
            Debug.WriteLine("\nDevice waiting for commands from IoTHub...\n");
            Message receivedMessage;
            string messageData;

            while (true)
            {
                receivedMessage = await this.deviceClient.ReceiveAsync();

                if (receivedMessage != null)
                {
                    messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    Debug.WriteLine("\t{0}> Received message: {1}", DateTime.Now.ToLocalTime(), messageData);

                    await deviceClient.CompleteAsync(receivedMessage);

                    return messageData;
                }

                //  Note: In this sample, the polling interval is set to 
                //  10 seconds to enable you to see messages as they are sent.
                //  To enable an IoT solution to scale, you should extend this //  interval. For example, to scale to 1 million devices, set 
                //  the polling interval to 25 minutes.
                //  For further information, see
                //  https://azure.microsoft.com/documentation/articles/iot-hub-devguide/#messaging
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
    }
}