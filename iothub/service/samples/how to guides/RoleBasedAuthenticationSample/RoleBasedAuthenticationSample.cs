// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Microsoft.Azure.Devices;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RoleBasedAuthenticationSample
{
    /// <summary>
    /// This sample connects to the IoT hub using Azure active directory credentials and sends a cloud-to-device message.
    /// For more information on setting up AAD for IoT hub, see <see href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-dev-guide-azure-ad-rbac"/>
    /// </summary>
    public class RoleBasedAuthenticationSample
    {
        public async Task RunSampleAsync(ServiceClient client, string deviceId)
        {
            Console.WriteLine("Connecting using token credential.");
            await client.OpenAsync();
            Console.WriteLine("Successfully opened connection.");

            Console.WriteLine("Sending a cloud-to-device message.");
            using var message = new Message(Encoding.ASCII.GetBytes("Hello, Cloud!"));
            await client.SendAsync(deviceId, message);
            Console.WriteLine("Successfully sent message.");
        }
    }
}
