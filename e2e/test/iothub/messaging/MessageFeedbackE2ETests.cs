// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class MessageFeedbackE2ETests : E2EMsTestBase
    {
        private const int MESSAGE_COUNT = 5;

        private static readonly string s_devicePrefix = $"{nameof(MessageFeedbackE2ETests)}_";
        private static readonly TimeSpan TIMESPAN_ONE_MINUTE = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan TIMESPAN_FIVE_SECONDS = TimeSpan.FromSeconds(5);

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_CompleteMixOrder_AMQP()
        {
            await CompleteMessageMixOrder(TestDeviceType.Sasl, Client.TransportType.Amqp_Tcp_Only, Logger).ConfigureAwait(false);
        }

        private static async Task CompleteMessageMixOrder(TestDeviceType type, Client.TransportType transport, MsTestLogger logger)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(logger, s_devicePrefix, type).ConfigureAwait(false);
            using (DeviceClient deviceClient = testDevice.CreateDeviceClient(transport))
            using (var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString))
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);

                if (transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                    await deviceClient.ReceiveAsync(TIMESPAN_FIVE_SECONDS).ConfigureAwait(false);
                }

                await serviceClient.OpenAsync().ConfigureAwait(false);

                var messages = new List<Client.Message>();
                for (int i = 0; i < MESSAGE_COUNT; i++)
                {
                    (Message msg, string payload, string p1Value) = MessageReceiveE2ETests.ComposeC2dTestMessage(logger);
                    await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);
                    Client.Message message = await deviceClient.ReceiveAsync(TIMESPAN_ONE_MINUTE).ConfigureAwait(false);
                    if (message == null)
                    {
                        Assert.Fail("No message received.");
                    }
                    messages.Add(message);
                }

                for (int i = 0; i < MESSAGE_COUNT; i++)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    await deviceClient.CompleteAsync(messages[MESSAGE_COUNT - 1 - i]).ConfigureAwait(false);
                    stopwatch.Stop();
                    Assert.IsFalse(stopwatch.ElapsedMilliseconds > deviceClient.OperationTimeoutInMilliseconds, $"CompleteAsync is over {deviceClient.OperationTimeoutInMilliseconds}");
                }

                await deviceClient.CloseAsync().ConfigureAwait(false);
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}
