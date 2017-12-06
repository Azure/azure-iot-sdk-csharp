// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.E2ETests
{
    using Message = Client.Message;
    using TransportType = Client.TransportType;

    [TestClass]
    public class MessageMemoryLeakTest
    {
        [TestMethod]
        public async Task Method_LongRunningLeakTest_Ok()
        {
            var proc = Process.GetCurrentProcess();

            DeviceClient deviceClient = DeviceClient.Create(
                "doesnotexist.domain",
                new DeviceAuthenticationWithRegistrySymmetricKey(
                    "fakedevice",
                    Convert.ToBase64String(new byte[] { 1, 2, 3 })),
                    TransportType.Mqtt);

            deviceClient.OperationTimeoutInMilliseconds = 5;

#pragma warning disable CS0618 // Type or member is obsolete
            deviceClient.RetryPolicy = RetryPolicyType.No_Retry;
#pragma warning restore CS0618 // Type or member is obsolete

            int i = 0;
            const int warmupIterations = 10;

            long lastMemoryUsed = 0;
            double lastCpuUsed = 0;

            while (true)
            {
                i++;

                try
                {
                    if ((i > warmupIterations) && (i % warmupIterations == 0))
                    {
                        long currentMemoryUsed = GC.GetTotalMemory(false);
                        double currentCpuUsed = proc.TotalProcessorTime.TotalSeconds;

                       

                        long deltaMemory = currentMemoryUsed - lastMemoryUsed;
                        double deltaCpu = currentCpuUsed - lastCpuUsed;

                        lastCpuUsed = currentCpuUsed;
                        lastMemoryUsed = currentMemoryUsed;
                    }

                    if (i == warmupIterations)
                    {
                        // Measure after initial warm-up.
                        lastMemoryUsed = GC.GetTotalMemory(false);
                        lastCpuUsed = proc.TotalProcessorTime.TotalSeconds;
                    }
                    
                    Console.WriteLine($"Send {i}");
                    var msg = CreateAzureMessage();
                    await deviceClient.SendEventBatchAsync(msg);

                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                }
            }
        }

        private static IEnumerable<Message> CreateAzureMessage()
        {
            var rnd = new Random();
            var list = new List<Message>();
            for (var i = 0; i < 5; i++)
            {
                var bytes = new byte[10];
                rnd.NextBytes(bytes);
                var msg = new Message(bytes);
                msg.Properties["dummytest"] = "yes";
                list.Add(msg);
            }
            return list;
        }
    }
}
