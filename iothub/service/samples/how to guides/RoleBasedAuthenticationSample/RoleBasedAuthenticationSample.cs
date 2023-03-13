// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace RoleBasedAuthenticationSample
{
    /// <summary>
    /// This sample connects to the IoT hub using Azure active directory credentials and sends a cloud-to-device message.
    /// For more information on setting up AAD for IoT hub, see <see href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-dev-guide-azure-ad-rbac"/>
    /// </summary>
    public class RoleBasedAuthenticationSample
    {
        public static async Task RunSampleAsync(IotHubServiceClient client, string deviceId)
        {
            Console.WriteLine("Connecting using token credential.");
            await client.Messages.OpenAsync();
            Console.WriteLine("Successfully opened connection.");

            Console.WriteLine("Sending a cloud-to-device message.");
            var message = new OutgoingMessage(Encoding.ASCII.GetBytes("Hello from the cloud!"));
            await client.Messages.SendAsync(deviceId, message);
            Console.WriteLine("Successfully sent message.");
        }
    }
}
