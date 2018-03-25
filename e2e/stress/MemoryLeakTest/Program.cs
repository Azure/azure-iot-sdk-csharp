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
using System.Net.Sockets;
using Microsoft.Azure.Devices.Client.Exceptions;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class MessageMemoryLeakTest
    {
        private const double Megabyte = 1024 * 1024;

        // Assert constants. These need to be tuned to the CPU/Arch of the test machine and Release|Debug compilation flags.
        private const double MaximumAllowedMemoryMb = 30;
        private const double MaximumAllowedCpuDeltaMs = 5 * 1000;
        private const long ExecutionTimeMilliseconds = 10 * 60 * 1000;

        public async Task<int> Method_LongRunningLeakTest_WrongEndpoint_Ok(TransportType transportType)
        {
            var proc = Process.GetCurrentProcess();

            DeviceClient deviceClient = DeviceClient.Create(
                "doesnotexist.domain",
                new DeviceAuthenticationWithRegistrySymmetricKey(
                    "fakedevice",
                    Convert.ToBase64String(new byte[] { 1, 2, 3 })),
                    transportType);

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

                IEnumerable<Message> msg = CreateAzureMessage();

                try
                {
                    if ((i > warmupIterations) && (i % warmupIterations == 0))
                    {
                        GC.Collect(3, GCCollectionMode.Forced, true);

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
                            "Transport: {0} MemDeltaMB: {1:+00.000;-00.000} CpuDeltaMs: {2:0.000} [MemTotalMB: {3:00.000}, CpuTotalMs: {4:00.000} MemMaxMB: {5:00.000}, CpuDMaxMs: {6:0.000}]",
                            transportType,
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

                    await deviceClient.SendEventBatchAsync(msg).ConfigureAwait(false);

                    // The Send is expected to throw.
                    return 3;
                }
                catch (TimeoutException) { }
                catch (SocketException) { }
                catch (TaskCanceledException) { }
                catch (IotHubCommunicationException) { }
                finally
                {
                    foreach (Message m in msg)
                    {
                        m.Dispose();
                    }
                }
            }

            Console.WriteLine("Test passed.");
            return 0;
        }

        private static int s_messageNumber = 0;

        private static IEnumerable<Message> CreateAzureMessage()
        {
            Debug.WriteLine($"------ Message {s_messageNumber} -----------");

            var rnd = new Random();
            var list = new List<Message>();
            for (var i = 0; i < 10; i++)
            {
                var bytes = new byte[100];
                rnd.NextBytes(bytes);

                byte[] msgNumber = BitConverter.GetBytes(s_messageNumber);
                Buffer.BlockCopy(msgNumber, 0, bytes, 0, msgNumber.Length);

                var msg = new Message(bytes);
                msg.Properties["dummytest" + s_messageNumber] = "no" + s_messageNumber;
                list.Add(msg);

                s_messageNumber++;
            }

            return list;
        }

        public static int Main(string[] args)
        {
            var messageMemoryLeakTest = new MessageMemoryLeakTest();
            TransportType[] transportTests =
            {
                TransportType.Http1,
                TransportType.Amqp,
                TransportType.Mqtt
            };

            foreach(TransportType t in transportTests)
            {
                s_messageNumber = 0;

                int ret = messageMemoryLeakTest.Method_LongRunningLeakTest_WrongEndpoint_Ok(t).GetAwaiter().GetResult();
                if (ret != 0)
                {
                    return ret;
                }
            }

            return 0;
        }
    }
}
