// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class MessagingClientE2ETests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(MessagingClientE2ETests)}_";

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [ExpectedException(typeof(OperationCanceledException))]
        [TestCategory("Flaky")]
        public async Task Message_TimeOutReachedResponse()
        {
            await FastTimeout().ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_NoTimeoutPassed()
        {
            await DefaultTimeout().ConfigureAwait(false);
        }

        private async Task FastTimeout()
        {
            // pre-cancelled cancellation token
            var cancellationToken = new CancellationToken(true);
            await TestTimeout(cancellationToken).ConfigureAwait(false);
        }

        private async Task DefaultTimeout()
        {
            await TestTimeout(CancellationToken.None).ConfigureAwait(false);
        }

        private async Task TestTimeout(CancellationToken cancellationToken)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            using var sender = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            // don't pass in cancellation token here. This test is for seeing how SendAsync reacts with an valid or expired token.
            await sender.Messages.OpenAsync(CancellationToken.None).ConfigureAwait(false);

            var sw = new Stopwatch();
            sw.Start();

            Logger.Trace($"Testing ServiceClient SendAsync() cancellation.");
            try
            {
                var testMessage = new Message(Encoding.ASCII.GetBytes("Test Message"));
                await sender.Messages.SendAsync(testDevice.Id, testMessage, cancellationToken).ConfigureAwait(false);

                // Pass in the cancellation token to see how the operation reacts to it.
                await sender.Messages.SendAsync(testDevice.Id, testMessage, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await sender.Messages.CloseAsync(CancellationToken.None).ConfigureAwait(false);
                sw.Stop();
                Logger.Trace($"Testing ServiceClient SendAsync(): exiting test after time={sw.Elapsed}; ticks={sw.ElapsedTicks}");
            }
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task MessagingClient_SendsMessage(IotHubTransportProtocol protocol)
        {
            // arrange
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            using var sender = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);
            await sender.Messages.OpenAsync().ConfigureAwait(false);

            var message = new Message(new byte[10]); // arbitrary payload since it shouldn't matter

            await sender.Messages.SendAsync(testDevice.Id, message).ConfigureAwait(false);
            await sender.Messages.CloseAsync().ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task MessagingClient_CanReopenClosedClient(IotHubTransportProtocol protocol)
        {
            // arrange
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            using var sender = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);

            // Open, close, then re-open the client
            await sender.Messages.OpenAsync().ConfigureAwait(false);
            await sender.Messages.CloseAsync().ConfigureAwait(false);
            await sender.Messages.OpenAsync().ConfigureAwait(false);

            // Client should still be usable after closing and re-opening
            var message = new Message(new byte[10]); // arbitrary payload since it shouldn't matter
            await sender.Messages.SendAsync(testDevice.Id, message).ConfigureAwait(false);
            await sender.Messages.CloseAsync().ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task MessagingClient_CanSendMultipleMessagesInOneConnection(IotHubTransportProtocol protocol)
        {
            // arrange
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            using var sender = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);
            await sender.Messages.OpenAsync().ConfigureAwait(false);

            // Client should be able to send more than one message on an open connection
            for (int i = 0; i < 2; i++)
            {
                var message = new Message(new byte[10]); // arbitrary payload since it shouldn't matter
                await sender.Messages.SendAsync(testDevice.Id, message).ConfigureAwait(false);
            }

            await sender.Messages.CloseAsync().ConfigureAwait(false);
        }

        // Unfortunately, the way AmqpServiceClient is implemented, it makes mocking the required amqp types difficult
        // (the amqp types are private members of the class, and cannot be set from any public/ internal API).
        // For this reason the following test is tested in the E2E flow, even though this is a unit test scenario.
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task MessageIdDefaultNotSet_SendEventDoesNotSetMessageId()
        {
            // arrange
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            using var sender = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            await sender.Messages.OpenAsync().ConfigureAwait(false);
            string messageId = Guid.NewGuid().ToString();

            // act
            var messageWithoutId = new Message();
            var messageWithId = new Message
            {
                MessageId = messageId,
            };
            await sender.Messages.SendAsync(testDevice.Id, messageWithoutId).ConfigureAwait(false);
            await sender.Messages.SendAsync(testDevice.Id, messageWithId).ConfigureAwait(false);

            await sender.Messages.CloseAsync().ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().BeNull();
            messageWithId.MessageId.Should().Be(messageId);
        }

        // Unfortunately, the way AmqpServiceClient is implemented, it makes mocking the required amqp types difficult
        // (the amqp types are private members of the class, and cannot be set from any public/ internal API).
        // For this reason the following test is tested in the E2E flow, even though this is a unit test scenario.
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task MessageIdDefaultSetToNull_SendEventDoesNotSetMessageId()
        {
            // arrange
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            var options = new IotHubServiceClientOptions
            {
                SdkAssignsMessageId = SdkAssignsMessageId.Never,
            };
            using var sender = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);
            await sender.Messages.OpenAsync().ConfigureAwait(false);
            string messageId = Guid.NewGuid().ToString();

            // act
            var messageWithoutId = new Message();
            var messageWithId = new Message
            {
                MessageId = messageId,
            };
            await sender.Messages.SendAsync(testDevice.Id, messageWithoutId).ConfigureAwait(false);
            await sender.Messages.SendAsync(testDevice.Id, messageWithId).ConfigureAwait(false);

            await sender.Messages.CloseAsync().ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().BeNull();
            messageWithId.MessageId.Should().Be(messageId);
        }

        // Unfortunately, the way AmqpServiceClient is implemented, it makes mocking the required amqp types difficult
        // (the amqp types are private members of the class, and cannot be set from any public/ internal API).
        // For this reason the following test is tested in the E2E flow, even though this is a unit test scenario.
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task MessageIdDefaultSetToGuid_SendEventSetMessageIdIfNotSet()
        {
            // arrange
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            var options = new IotHubServiceClientOptions
            {
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
            };
            using var sender = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);
            await sender.Messages.OpenAsync().ConfigureAwait(false);
            string messageId = Guid.NewGuid().ToString();

            // act
            var messageWithoutId = new Message();
            var messageWithId = new Message
            {
                MessageId = messageId,
            };
            await sender.Messages.SendAsync(testDevice.Id, messageWithoutId).ConfigureAwait(false);
            await sender.Messages.SendAsync(testDevice.Id, messageWithId).ConfigureAwait(false);

            await sender.Messages.CloseAsync().ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().NotBeNullOrEmpty();
            messageWithId.MessageId.Should().Be(messageId);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task MessagingClient_SendToNonexistentDevice_ThrowIotHubServiceException(IotHubTransportProtocol protocol)
        {
            // arrange
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };
            using var sender = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);
            await sender.Messages.OpenAsync().ConfigureAwait(false);

            try
            {
                // act
                var message = new Message(new byte[10]); // arbitrary payload since it shouldn't matter
                Func<Task> act = async () => await sender.Messages.SendAsync("nonexistent-device-id", message).ConfigureAwait(false);

                // assert
                var error = await act.Should().ThrowAsync<IotHubServiceException>();
                error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
                error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.DeviceNotFound);
                error.And.IsTransient.Should().BeFalse();
            }
            finally
            {
                await sender.Messages.CloseAsync().ConfigureAwait(false);
            }
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task MessagingClient_SendToNonexistentModule_ThrowIotHubServiceException(IotHubTransportProtocol protocol)
        {
            // arrange
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            using var sender = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);
            await sender.Messages.OpenAsync().ConfigureAwait(false);

            try
            {
                // act
                var message = new Message(new byte[10]); // arbitrary payload since it shouldn't matter
                Func<Task> act = async () => await sender.Messages.SendAsync(testDevice.Id, "nonexistent-module-id", message).ConfigureAwait(false);

                // assert
                var error = await act.Should().ThrowAsync<IotHubServiceException>();
                error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);

                // AmqpErrorCode doesn't provide specific codes (6 digits) for the error NotFound 404,
                // as a result, we are mapping all of NotFound errors to IotHubServiceErrorCode.DeviceNotFound for AMQP operations.
                // For more details of this error, see error message via IotHubServiceException.Message.
                error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.DeviceNotFound);
                error.And.IsTransient.Should().BeFalse();
            }
            finally
            {
                await sender.Messages.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}
