// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AzureSasCredentialAuthenticationSample
{
    /// <summary>
    /// This sample connects to the IoT hub using SAS token and sends a cloud-to-device message.
    /// For more information on setting up AAD for IoT hub, see <see href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-dev-guide-azure-ad-rbac"/>
    /// </summary>
    public class AzureSasCredentialAuthenticationSample
    {
        public static async Task RunSampleAsync(IotHubServiceClient client, string deviceId)
        {
            Console.WriteLine("Connecting using SAS credential.");
            await client.Messages.OpenAsync();
            Console.WriteLine("Successfully opened connection.");

            Console.WriteLine("Sending a cloud-to-device message.");
            var message = new OutgoingMessage(Encoding.ASCII.GetBytes("Hello, Cloud!"));
            await client.Messages.SendAsync(deviceId, message);
            Console.WriteLine("Successfully sent message.");
        }
    }
}
