// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace X509DeviceCertWithChainSample
{
    /// <summary>
    /// A sample to illustrate authenticating with a device by passing in the device certificate and 
    /// full chain of certificates from the one used to sign the device certificate to the one uploaded to the service.
    /// AuthSetup.ps1 can be used to create the necessary certs and setup to run this sample.
    /// </summary>
    public class X509DeviceCertWithChainSample
    {
        private readonly DeviceClient _deviceClient;

        public X509DeviceCertWithChainSample(DeviceClient deviceClient)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
        }

        public async Task RunSampleAsync()
        {
            await _deviceClient.OpenAsync();
            Console.WriteLine("Device connection SUCCESS.");

            await SendTelemetry();
            Console.WriteLine("Send telemetry SUCCESS.");

            await _deviceClient.CloseAsync();
        }

        private async Task SendTelemetry()
        {
            string telemetryPayload = "{{ \"temperature\": 0d }}";
            using var message = new Message(Encoding.UTF8.GetBytes(telemetryPayload))
            {
                ContentEncoding = "utf-8",
                ContentType = "application/json",
            };

            await _deviceClient.SendEventAsync(message);
        }
    }
}
