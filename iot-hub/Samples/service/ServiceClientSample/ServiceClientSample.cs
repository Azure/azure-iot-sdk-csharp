// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class ServiceClientSample
    {
        private readonly ServiceClient _serviceClient;

        public ServiceClientSample(ServiceClient serviceClient)
        {
            _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
        }

        public async Task RunSampleAsync(string deviceId)
        {
            var str = "Hello, Cloud!";
            var message = new Message(Encoding.ASCII.GetBytes(str));

            Console.Write($"\tSending C2D message to {deviceId} . . . ");
            await _serviceClient.SendAsync(deviceId, message).ConfigureAwait(false);
            Console.WriteLine("DONE");
        }
    }
}
