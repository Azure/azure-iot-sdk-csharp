// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    // TODO MQTT OrderedTwoPhaseWorkQueue disallow message feedback to be called mix order, enable this test once it's fixed
    public class MessageFeedbackE2ETests : E2EMsTestBase
    {
        private const int MessageCount = 5;

        private static readonly string s_devicePrefix = $"{nameof(MessageFeedbackE2ETests)}_";
        private static readonly TimeSpan s_oneMinute = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan s_fiveSeconds = TimeSpan.FromSeconds(5);

        [LoggedTestMethod]
        public async Task Message_CompleteMixOrder_AMQP()
        {
            await CompleteMessageMixOrder(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only, Logger).ConfigureAwait(false);
        }

        private static async Task CompleteMessageMixOrder(TestDeviceType type, Client.TransportType transport, MsTestLogger logger)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(logger, s_devicePrefix, type).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(new ClientOptions { TransportType = transport });
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            await deviceClient.OpenAsync().ConfigureAwait(false);

            if (transport == Client.TransportType.Mqtt_Tcp_Only
                || transport == Client.TransportType.Mqtt_WebSocket_Only)
            {
                // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                using var cts = new CancellationTokenSource(s_fiveSeconds);
                await deviceClient.ReceiveMessageAsync(cts.Token).ConfigureAwait(false);
            }

            await serviceClient.OpenAsync().ConfigureAwait(false);

            var messages = new List<Client.Message>(MessageCount);
            for (int i = 0; i < MessageCount; i++)
            {
                (Message msg, string payload, string p1Value) = MessageReceiveE2ETests.ComposeC2dTestMessage(logger);
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
                using var cts = new CancellationTokenSource(s_oneMinute);
                Client.Message message = await deviceClient.ReceiveMessageAsync(cts.Token).ConfigureAwait(false);
                if (message == null)
                {
                    Assert.Fail("No message received.");
                }
                messages.Add(message);
            }

            for (int i = 0; i < MessageCount; i++)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                using var cts = new CancellationTokenSource(s_oneMinute);
                await deviceClient.CompleteMessageAsync(messages[MessageCount - 1 - i], cts.Token).ConfigureAwait(false);
                stopwatch.Stop();
                Assert.IsFalse(stopwatch.Elapsed > s_oneMinute, $"CompleteMessageAsync is over {s_oneMinute}");
            }

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }
    }
}
