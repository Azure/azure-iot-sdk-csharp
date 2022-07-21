// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    // TODO MQTT OrderedTwoPhaseWorkQueue disallow message feedback to be called mix order, enable this test once it's fixed
    public class MessageFeedbackE2eTests : E2EMsTestBase
    {
        private const int MessageCount = 3;

        private static readonly string s_devicePrefix = $"{nameof(MessageFeedbackE2eTests)}_";
        private static readonly TimeSpan s_oneMinute = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan s_fiveSeconds = TimeSpan.FromSeconds(5);

        [LoggedTestMethod]
        public async Task Message_CompleteMixOrder_Amqp()
        {
            // AMQP allows completing messages in any order received. Let's test that.
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
                Assert.Fail("Message completion out of order not supported outside of AMQP");
            }

            await serviceClient.OpenAsync().ConfigureAwait(false);

            var messages = new List<Client.Message>(MessageCount);
            for (int i = 0; i < MessageCount; i++)
            {
                (Message msg, string _, string _) = MessageReceiveE2ETests.ComposeC2dTestMessage(logger);
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
                msg.Dispose();

                using var cts = new CancellationTokenSource(s_oneMinute);
                Client.Message message = await deviceClient.ReceiveMessageAsync(cts.Token).ConfigureAwait(false);
                if (message == null)
                {
                    Assert.Fail("No message received.");
                }
                messages.Add(message);
            }

            var stopwatch = new Stopwatch();
            // process the messages in reverse order as received
            for (int i = MessageCount - 1; i >= 0; i--)
            {
                stopwatch.Reset();

                using var cts = new CancellationTokenSource(s_oneMinute);
                using Client.Message message = messages[i];
                await deviceClient.CompleteMessageAsync(message, cts.Token).ConfigureAwait(false);
                message.Dispose();

                stopwatch.Stop();
                stopwatch.Elapsed.Should().BeLessThan(s_oneMinute, $"CompleteMessageAsync exceeded the cancellation token source timeout.");
            }

            await deviceClient.CloseAsync().ConfigureAwait(false);
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }
    }
}
