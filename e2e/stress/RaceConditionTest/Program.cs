// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Run using the following:
//  dotnet run --framework netcoreapp2.1 --configuration Release
//  dotnet run --framework net47 --configuration Release

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Azure.Devices.Client.Exceptions;
using System.Threading;
using System.Text;
using System.IO;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class MessageRaceConditionTest
    {
        private string DeviceConnectionString;
        private const int NumberOfParallelDeviceClientCalls = 10000;

        public MessageRaceConditionTest(string deviceConnectionString)
        {
            DeviceConnectionString = deviceConnectionString;
        }

        public async Task<int> RunTest_SendTenThousandMessagesAsync(TransportType transportType)
        {
            const int MaxExecutionTimeMinutes = 10;
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, transportType);
            deviceClient.OperationTimeoutInMilliseconds = 5;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(MaxExecutionTimeMinutes));

#pragma warning disable CS0618 // Type or member is obsolete
            deviceClient.RetryPolicy = RetryPolicyType.No_Retry;
#pragma warning restore CS0618 // Type or member is obsolete

            List<Task> tasks = new List<Task>();

            try
            {
                for (int i = 0; i < NumberOfParallelDeviceClientCalls; i++)
                {
                    Message message = new Message(Encoding.ASCII.GetBytes("Test"));
                    tasks.Add(deviceClient.SendEventAsync(message, cancellationTokenSource.Token));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }

            Console.WriteLine("Test passed (" + transportType + ")");
            return 0;
        }

        public async Task<int> RunTest_SendThousandsOfMessagesAndCancelAsync(TransportType transportType)
        {
            const int MaxExecutionTimeInSeconds = 10;
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, transportType);
            deviceClient.OperationTimeoutInMilliseconds = 5;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(MaxExecutionTimeInSeconds));

#pragma warning disable CS0618 // Type or member is obsolete
            deviceClient.RetryPolicy = RetryPolicyType.No_Retry;
#pragma warning restore CS0618 // Type or member is obsolete

            List<Task> tasks = new List<Task>();

            try
            {
                for (int i = 0; i < NumberOfParallelDeviceClientCalls; i++)
                {
                    Message message = new Message(Encoding.ASCII.GetBytes("Test"));
                    tasks.Add(deviceClient.SendEventAsync(message, cancellationTokenSource.Token));
                }

                cancellationTokenSource.Cancel();

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }

            Console.WriteLine("Test passed (" + transportType + ")");
            return 0;
        }

        public static int Main(string[] args)
        {
            string deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONN_STRING") ?? throw new Exception("Device Connection string not provided");

            var messageRaceConditionTest = new MessageRaceConditionTest(deviceConnectionString);
            TransportType[] transportTests =
            {
                TransportType.Http1,
                TransportType.Amqp,
                TransportType.Mqtt
            };

            foreach(TransportType t in transportTests)
            {
                int returnValue;

                returnValue = messageRaceConditionTest.RunTest_SendTenThousandMessagesAsync(t).GetAwaiter().GetResult();

                if (returnValue != 0)
                {
                    Console.WriteLine("Failed RunTest_SendTenThousandMessagesAsync");
                    return returnValue;
                }

                returnValue = messageRaceConditionTest.RunTest_SendThousandsOfMessagesAndCancelAsync(t).GetAwaiter().GetResult();

                if (returnValue != 0)
                {
                    Console.WriteLine("Failed RunTest_SendThousandsOfMessagesAndCancelAsync");
                    return returnValue;
                }
            }

            return 0;
        }
    }
}
