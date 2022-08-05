// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class ServiceClientE2ETests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(ServiceClientE2ETests)}_";

        [LoggedTestMethod]
        [ExpectedException(typeof(TimeoutException))]
        [TestCategory("Flaky")]
        public async Task Message_TimeOutReachedResponse()
        {
            await FastTimeout().ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_NoTimeoutPassed()
        {
            await DefaultTimeout().ConfigureAwait(false);
        }

        private async Task FastTimeout()
        {
            TimeSpan? timeout = TimeSpan.FromTicks(10).Negate();
            await TestTimeout(timeout).ConfigureAwait(false);
        }

        private async Task DefaultTimeout()
        {
            TimeSpan? timeout = null;
            await TestTimeout(timeout).ConfigureAwait(false);
        }

        private async Task TestTimeout(TimeSpan? timeout)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            using var sender = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);

            var sw = new Stopwatch();
            sw.Start();

            Logger.Trace($"Testing ServiceClient SendAsync() timeout in ticks={timeout?.Ticks}");
            try
            {
                using var testMessage = new Message(Encoding.ASCII.GetBytes("Test Message"));
                await sender.Messaging.SendAsync(testDevice.Id, testMessage, timeout).ConfigureAwait(false);
            }
            finally
            {
                sw.Stop();
                Logger.Trace($"Testing ServiceClient SendAsync(): exiting test after time={sw.Elapsed}; ticks={sw.ElapsedTicks}");
            }
        }

        [LoggedTestMethod]
        [DataRow(TransportType.Amqp)]
        [DataRow(TransportType.Amqp_WebSocket_Only)]
        public async Task ServiceClient_SendsMessage(TransportType transportType)
        {
            // arrange
            IotHubServiceClientOptions options = new IotHubServiceClientOptions
            {
                UseWebSocketOnly = transportType == TransportType.Amqp_WebSocket_Only
            };
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            using var sender = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString, options);
            string messageId = Guid.NewGuid().ToString();

            // act and expect no exception
            using var message = new Message
            {
                MessageId = messageId,
            };
            await sender.Messaging.SendAsync(testDevice.Id, message).ConfigureAwait(false);
        }

        // Unfortunately, the way AmqpServiceClient is implemented, it makes mocking the required amqp types difficult
        // (the amqp types are private members of the class, and cannot be set from any public/ internal API).
        // For this reason the following test is tested in the E2E flow, even though this is a unit test scenario.
        [LoggedTestMethod]
        public async Task MessageIdDefaultNotSet_SendEventDoesNotSetMessageId()
        {
            // arrange
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            using var sender = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);
            string messageId = Guid.NewGuid().ToString();

            // act
            using var messageWithoutId = new Message();
            using var messageWithId = new Message
            {
                MessageId = messageId,
            };
            await sender.Messaging.SendAsync(testDevice.Id, messageWithoutId).ConfigureAwait(false);
            await sender.Messaging.SendAsync(testDevice.Id, messageWithId).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().BeNull();
            messageWithId.MessageId.Should().Be(messageId);
        }

        // Unfortunately, the way AmqpServiceClient is implemented, it makes mocking the required amqp types difficult
        // (the amqp types are private members of the class, and cannot be set from any public/ internal API).
        // For this reason the following test is tested in the E2E flow, even though this is a unit test scenario.
        [LoggedTestMethod]
        public async Task MessageIdDefaultSetToNull_SendEventDoesNotSetMessageId()
        {
            // arrange
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            var options = new IotHubServiceClientOptions
            {
                SdkAssignsMessageId = SdkAssignsMessageId.Never,
            };
            using var sender = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString, options);
            string messageId = Guid.NewGuid().ToString();

            // act
            using var messageWithoutId = new Message();
            using var messageWithId = new Message
            {
                MessageId = messageId,
            };
            await sender.Messaging.SendAsync(testDevice.Id, messageWithoutId).ConfigureAwait(false);
            await sender.Messaging.SendAsync(testDevice.Id, messageWithId).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().BeNull();
            messageWithId.MessageId.Should().Be(messageId);
        }

        // Unfortunately, the way AmqpServiceClient is implemented, it makes mocking the required amqp types difficult
        // (the amqp types are private members of the class, and cannot be set from any public/ internal API).
        // For this reason the following test is tested in the E2E flow, even though this is a unit test scenario.
        [LoggedTestMethod]
        public async Task MessageIdDefaultSetToGuid_SendEventSetMessageIdIfNotSet()
        {
            // arrange
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            var options = new IotHubServiceClientOptions
            {
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
            };
            using var sender = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString, options);
            string messageId = Guid.NewGuid().ToString();

            // act
            using var messageWithoutId = new Message();
            using var messageWithId = new Message
            {
                MessageId = messageId,
            };
            await sender.Messaging.SendAsync(testDevice.Id, messageWithoutId).ConfigureAwait(false);
            await sender.Messaging.SendAsync(testDevice.Id, messageWithId).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().NotBeNullOrEmpty();
            messageWithId.MessageId.Should().Be(messageId);
        }
    }
}
