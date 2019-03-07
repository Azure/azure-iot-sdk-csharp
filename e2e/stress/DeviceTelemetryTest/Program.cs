// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using System;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var writer = new ResultWriterFile("singleDeviceTelemetryPerf.csv");
            writer.WriteHeaderAsync().GetAwaiter().GetResult();

            int messageCount = 100000;
            int parallelCount = 1000;
            int messageSize = 64; // bytes

            TransportType[] transportTests =
            {
                TransportType.Amqp,
                TransportType.Http1,
                TransportType.Mqtt
            };

            foreach (TransportType t in transportTests)
            {
                var test = new DeviceTelemetryTest(
                    protocol: t,
                    messageCount: messageCount,
                    messageSize: messageSize,
                    parallelCount: parallelCount,
                    resultWriter: writer);

                test.RunTestAsync().GetAwaiter().GetResult();
            }
        }
    }
}
