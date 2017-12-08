// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Run using the following:
//  dotnet run --framework netcoreapp2.0 --configuration Release
//  dotnet run --framework net46 --configuration Release

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class MessageMemoryLeakTest
    {
        private const double Megabyte = 1024 * 1024;

        // Assert constants. These need to be tuned to the CPU/Arch of the test machine and Release|Debug compilation flags.
        private const double MaximumAllowedMemoryMb = 30;
        private const double MaximumAllowedCpuDeltaMs = 5 * 1000;
        private const long ExecutionTimeMilliseconds = 30 * 60 * 1000;

        public async Task<int> Method_LongRunningLeakTest_WrongEndpoint_Ok()
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
            long maxMemoryUsed = 0;
            double maxCpuDeltaUsed = 0;

            bool done = false;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (!done)
            {
                i++;
                done = sw.ElapsedMilliseconds > ExecutionTimeMilliseconds;

                try
                {
                    if ((i > warmupIterations) && (i % warmupIterations == 0))
                    {
                        long currentMemoryUsed = GC.GetTotalMemory(false);
                        double currentCpuUsed = proc.TotalProcessorTime.TotalMilliseconds;

                        long deltaMemory = currentMemoryUsed - lastMemoryUsed;
                        double deltaCpu = currentCpuUsed - lastCpuUsed;

                        if (maxCpuDeltaUsed < deltaCpu) maxCpuDeltaUsed = deltaCpu;
                        if (maxMemoryUsed < currentMemoryUsed)
                        {
                            maxMemoryUsed = currentMemoryUsed;
                            Console.ForegroundColor = ConsoleColor.Red;
                        }
                        else if (deltaMemory < 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                        }

                        Console.WriteLine(
                            "MemDeltaMB: {0:+00.000;-00.000} CpuDeltaMs: {1:0.000} [MemTotalMB: {2:00.000}, CpuTotalMs: {3:00.000} MemMaxMB: {4:00.000}, CpuDMaxMs: {5:0.000}]",
                            deltaMemory / Megabyte,
                            deltaCpu,
                            currentMemoryUsed / Megabyte,
                            currentCpuUsed,
                            maxMemoryUsed / Megabyte,
                            maxCpuDeltaUsed);

                        lastCpuUsed = currentCpuUsed;
                        lastMemoryUsed = currentMemoryUsed;

                        if (deltaCpu > MaximumAllowedCpuDeltaMs)
                        {
                            Console.WriteLine($"Test failed: Expected {nameof(deltaCpu)} < {MaximumAllowedCpuDeltaMs} but is {deltaCpu}");
                            return 1;
                        }

                        if (maxMemoryUsed / Megabyte > MaximumAllowedMemoryMb)
                        {
                            Console.WriteLine($"Test failed: Expected {nameof(maxMemoryUsed)} < {MaximumAllowedMemoryMb * Megabyte} but is {maxMemoryUsed}");
                            return 2;
                        }

                        Console.ForegroundColor = ConsoleColor.Gray;
                    }

                    if (i == warmupIterations)
                    {
                        // Measure after initial warm-up.
                        lastMemoryUsed = GC.GetTotalMemory(false);
                        lastCpuUsed = proc.TotalProcessorTime.TotalMilliseconds;
                    }

                    var msg = CreateAzureMessage();
                    await deviceClient.SendEventBatchAsync(msg).ConfigureAwait(false);

                    // The Send is expected to throw.
                    return 3;
                }
                catch (Exception)
                {
                }
            }

            Console.WriteLine("Test passed.");
            return 0;
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

        public static int Main(string[] args)
        {
            var messageMemoryLeakTest = new MessageMemoryLeakTest();
            return messageMemoryLeakTest.Method_LongRunningLeakTest_WrongEndpoint_Ok().GetAwaiter().GetResult();
        }
    }
}
