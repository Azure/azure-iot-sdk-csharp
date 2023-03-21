// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class MessagingClientE2ETests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(MessagingClientE2ETests)}_";

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [ExpectedException(typeof(OperationCanceledException))]
        [TestCategory("Flaky")]
        public async Task Message_TimeOutReachedResponse()
        {
            await FastTimeout().ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
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
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            IotHubServiceClient sender = TestDevice.ServiceClient;

            // don't pass in cancellation token here. This test is for seeing how SendAsync reacts with an valid or expired token.
            await sender.Messages.OpenAsync(CancellationToken.None).ConfigureAwait(false);

            var sw = new Stopwatch();
            sw.Start();

            VerboseTestLogger.WriteLine($"Testing ServiceClient SendAsync() cancellation.");
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
                VerboseTestLogger.WriteLine($"Testing ServiceClient SendAsync(): exiting test after time={sw.Elapsed}; ticks={sw.ElapsedTicks}");
            }
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task MessagingClient_SendsMessage(IotHubTransportProtocol protocol)
        {
            // arrange
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using var sender = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);
            await sender.Messages.OpenAsync().ConfigureAwait(false);

            var message = new Message(new byte[10]); // arbitrary payload since it shouldn't matter

            await sender.Messages.SendAsync(testDevice.Id, message).ConfigureAwait(false);
            await sender.Messages.CloseAsync().ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task MessagingClient_CanReopenClosedClient(IotHubTransportProtocol protocol)
        {
            // arrange
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
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

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task MessagingClient_OpeningAlreadyOpenClient_DoesNotThrow(IotHubTransportProtocol protocol)
        {
            // arrange
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);

            try
            {
                // act
                // Call OpenAsync on already open client
                await serviceClient.Messages.OpenAsync().ConfigureAwait(false);
                Func<Task> act = async () => await serviceClient.Messages.OpenAsync();

                // assert
                await act.Should().NotThrowAsync();
            }
            finally
            {
                await serviceClient.Messages.CloseAsync();
            }

        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow (IotHubTransportProtocol.WebSocket)]
        public async Task MessagingClient_SendMessageOnClosedClient_ThrowsIotHubServiceException(IotHubTransportProtocol protocol)
        {
            // arrange
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync (DevicePrefix).ConfigureAwait(false);
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);

            // act
            var message = new Message(new byte[10]);
            await serviceClient.Messages.OpenAsync().ConfigureAwait(false);
            await serviceClient.Messages.CloseAsync();

            Func<Task> act = async () => await serviceClient.Messages.SendAsync(testDevice.Id, message);

            // assert
            var error = await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task MessagingClient_CanSendMultipleMessagesInOneConnection(IotHubTransportProtocol protocol)
        {
            // arrange
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
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
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task MessageIdDefaultNotSet_SendEventDoesNotSetMessageId()
        {
            // arrange
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
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
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task MessageIdDefaultSetToNull_SendEventDoesNotSetMessageId()
        {
            // arrange
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
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
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task MessageIdDefaultSetToGuid_SendEventSetMessageIdIfNotSet()
        {
            // arrange
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
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

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
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

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubTransportProtocol.Tcp)]
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task MessagingClient_SendToNonexistentModule_ThrowIotHubServiceException(IotHubTransportProtocol protocol)
        {
            // arrange
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
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
