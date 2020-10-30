// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Iothub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class ServiceClientE2ETests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"E2E_{nameof(ServiceClientE2ETests)}_";

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
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            using var sender = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            Logger.Trace($"Testing ServiceClient SendAsync() timeout in ticks={timeout?.Ticks}");
            try
            {
                await sender.SendAsync(testDevice.Id, new Message(Encoding.ASCII.GetBytes("Dummy Message")), timeout).ConfigureAwait(false);
            }
            finally
            {
                sw.Stop();
                Logger.Trace($"Testing ServiceClient SendAsync(): exiting test after time={sw.Elapsed}; ticks={sw.ElapsedTicks}");
            }
        }

        // Unfortunately, the way AmqpServiceClient is implemented, it makes mocking the required amqp types difficult
        // (the amqp types are private members of the class, and cannot be set from any public/ internal API).
        // For this reason the following test is tested in the E2E flow, even though this is a unit test scenario.
        [LoggedTestMethod]
        public async Task MessageIdDefaultNotSet_SendEventDoesNotSetMessageId()
        {
            // arrange
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            using var sender = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            string messageId = Guid.NewGuid().ToString();

            // act
            var messageWithoutId = new Message();
            var messageWithId = new Message
            {
                MessageId = messageId,
            };
            await sender.SendAsync(testDevice.Id, messageWithoutId).ConfigureAwait(false);
            await sender.SendAsync(testDevice.Id, messageWithId).ConfigureAwait(false);

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
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            var options = new ServiceClientOptions
            {
                SdkAssignsMessageId = Shared.SdkAssignsMessageId.Never,
            };
            using var sender = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, options);
            string messageId = Guid.NewGuid().ToString();

            // act
            var messageWithoutId = new Message();
            var messageWithId = new Message
            {
                MessageId = messageId,
            };
            await sender.SendAsync(testDevice.Id, messageWithoutId).ConfigureAwait(false);
            await sender.SendAsync(testDevice.Id, messageWithId).ConfigureAwait(false);

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
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            var options = new ServiceClientOptions
            {
                SdkAssignsMessageId = Shared.SdkAssignsMessageId.WhenUnset,
            };
            using var sender = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, options);
            string messageId = Guid.NewGuid().ToString();

            // act
            var messageWithoutId = new Message();
            var messageWithId = new Message
            {
                MessageId = messageId,
            };
            await sender.SendAsync(testDevice.Id, messageWithoutId).ConfigureAwait(false);
            await sender.SendAsync(testDevice.Id, messageWithId).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().NotBeNullOrEmpty();
            messageWithId.MessageId.Should().Be(messageId);
        }
    }
}
