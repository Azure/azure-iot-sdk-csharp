// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Service")]
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
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, ct: ct).ConfigureAwait(false);
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;

            // don't pass in cancellation token here. This test is for seeing how SendAsync reacts with an valid or expired token.
            await serviceClient.Messages.OpenAsync(CancellationToken.None).ConfigureAwait(false);

            var sw = new Stopwatch();
            sw.Start();

            VerboseTestLogger.WriteLine($"Testing ServiceClient SendAsync() cancellation.");
            try
            {
                var testMessage = new OutgoingMessage("Test Message");
                await serviceClient.Messages.SendAsync(testDevice.Id, testMessage, cancellationToken).ConfigureAwait(false);

                // Pass in the cancellation token to see how the operation reacts to it.
                await serviceClient.Messages.SendAsync(testDevice.Id, testMessage, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await serviceClient.Messages.CloseAsync(CancellationToken.None).ConfigureAwait(false);
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

            var message = new OutgoingMessage(new object()); // arbitrary payload since it shouldn't matter

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
            var message = new OutgoingMessage(new object()); // arbitrary payload since it shouldn't matter
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
        [DataRow(IotHubTransportProtocol.WebSocket)]
        public async Task MessagingClient_SendMessageOnClosedClient_ThrowsInvalidOperationException(IotHubTransportProtocol protocol)
        {
            // arrange
            var options = new IotHubServiceClientOptions
            {
                Protocol = protocol
            };

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);

            // act
            var message = new OutgoingMessage(new object());
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
                var message = new OutgoingMessage(new object()); // arbitrary payload since it shouldn't matter
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
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            await serviceClient.Messages.OpenAsync().ConfigureAwait(false);
            string messageId = Guid.NewGuid().ToString();

            // act
            var messageWithoutId = new OutgoingMessage();
            var messageWithId = new OutgoingMessage
            {
                MessageId = messageId,
            };
            await serviceClient.Messages.SendAsync(testDevice.Id, messageWithoutId).ConfigureAwait(false);
            await serviceClient.Messages.SendAsync(testDevice.Id, messageWithId).ConfigureAwait(false);

            await serviceClient.Messages.CloseAsync().ConfigureAwait(false);

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
            var messageWithoutId = new OutgoingMessage();
            var messageWithId = new OutgoingMessage
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
            var messageWithoutId = new OutgoingMessage();
            var messageWithId = new OutgoingMessage
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
                var message = new OutgoingMessage(new object()); // arbitrary payload since it shouldn't matter
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
                var message = new OutgoingMessage(new object()); // arbitrary payload since it shouldn't matter
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

        // By default, the service client serializes to JSON and encodes with UTF8. For clients wishing to use a binary payload
        // They should be able to specify the payload as a byte array and not have it serialized and encoded.
        // Then on the receiving end in the device client, rather than use TryGetPayload<T> which uses the configured payload
        // convention, they can get the payload as bytes and do their own deserialization.
        [TestMethod]
        public async Task OutgoingMessage_GetPayloadObjectBytes_DoesNotSerialize()
        {
            // arrange
            string actualPayloadString = null;
            var messageReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cts = new CancellationTokenSource(TestTimeoutMilliseconds);

            Encoding payloadEncoder = Encoding.UTF32; // use a different encoder than JSON

            const string payload = "My custom payload";
            byte[] payloadBytes = payloadEncoder.GetBytes(payload);
            var outgoingMessage = new OutgoingMessage(payloadBytes);

            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(nameof(OutgoingMessage_GetPayloadObjectBytes_DoesNotSerialize)).ConfigureAwait(false);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient();
            await testDevice.OpenWithRetryAsync(cts.Token).ConfigureAwait(false);

            await deviceClient
                .SetIncomingMessageCallbackAsync((incomingMessage) =>
                    {
                        byte[] actualPayloadBytes = incomingMessage.GetPayloadAsBytes();
                        actualPayloadString = payloadEncoder.GetString(actualPayloadBytes);
                        VerboseTestLogger.WriteLine($"Received message with payload [{actualPayloadString}].");
                        messageReceived.TrySetResult(true);
                        return Task.FromResult(MessageAcknowledgement.Complete);
                    },
                     cts.Token)
                .ConfigureAwait(false);

            // act
            await TestDevice.ServiceClient.Messages.OpenAsync(cts.Token).ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"Sending message with payload [{payload}] encoded to bytes using {payloadEncoder}.");
            await TestDevice.ServiceClient.Messages.SendAsync(testDevice.Id, outgoingMessage, cts.Token).ConfigureAwait(false);
            await messageReceived.WaitAsync(cts.Token).ConfigureAwait(false);

            // assert
            actualPayloadString.Should().Be(payload);
        }

        [TestMethod]
        public async Task MessagingClient_CloseGracefully_DoesNotExecuteConnectionLoss()
        {
            // arrange
            using var sender = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            bool connectionLossEventExecuted = false;
            Func<MessagesClientError, Task> OnConnectionLostAsync = delegate
            {
                // There is a small chance that this test's connection is interrupted by an actual
                // network failure (when this callback should be executed), but the operations
                // tested here are so quick that it should be safe to ignore that possibility.
                connectionLossEventExecuted = true;
                return Task.CompletedTask;
            };
            sender.Messages.ErrorProcessor = OnConnectionLostAsync;

            await sender.Messages.OpenAsync().ConfigureAwait(false);

            // act
            await sender.Messages.CloseAsync().ConfigureAwait(false);

            // assert
            connectionLossEventExecuted.Should().BeFalse(
                "One or more connection lost events were reported by the error processor unexpectedly");
        }
    }
}
