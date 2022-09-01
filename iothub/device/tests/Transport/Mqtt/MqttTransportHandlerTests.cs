// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;

namespace Microsoft.Azure.Devices.Client.Test.Transport
{
    [TestClass]
    [TestCategory("Unit")]
    public class MqttTransportHandlerTests
    {
        private const string DummyConnectionString = "HostName=127.0.0.1;SharedAccessKeyName=AllAccessKey;DeviceId=FakeDevice;SharedAccessKey=dGVzdFN0cmluZzE=";
        private const string DummyModuleConnectionString = "HostName=127.0.0.1;SharedAccessKeyName=AllAccessKey;DeviceId=FakeDevice;ModuleId=FakeModule;SharedAccessKey=dGVzdFN0cmluZzE=";
        private const string fakeMethodResponseBody = "{ \"foo\" : \"bar\" }";
        private const string methodPostTopicFilter = "$iothub/methods/POST/#";
        private const string twinPatchDesiredTopicFilter = "$iothub/twin/PATCH/properties/desired/#";
        private const string twinPatchReportedTopicPrefix = "$iothub/twin/PATCH/properties/reported/";
        private const string twinGetTopicPrefix = "$iothub/twin/GET/?$rid=";
        private const int statusSuccess = 200;
        private const int statusFailure = 400;
        private const string fakeResponseId = "fakeResponseId";
        private static readonly TimeSpan ReceiveTimeoutBuffer = TimeSpan.FromSeconds(5);

        private delegate bool MessageMatcher(Message msg);

        [TestMethod]
        public async Task MqttTransportHandlerOpenAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().OpenAsync(token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerSendEventAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendEventAsync(new Message(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerReceiveAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().ReceiveMessageAsync(token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerCompleteAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().CompleteMessageAsync(Guid.NewGuid().ToString(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerEnableMethodsAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().EnableMethodsAsync(token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerDisableMethodsAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().DisableMethodsAsync(token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerSendMethodResponseAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendMethodResponseAsync(new DirectMethodResponse(0, null), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerEnableTwinPatchAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().EnableTwinPatchAsync(token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerSendTwinGetAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendTwinGetAsync(token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerSendTwinPatchAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendTwinPatchAsync(new TwinCollection(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandler_OpenAsync_OpenHandlesConnectExceptionAndThrowsWhenChannelIsNotInitialized()
        {
            // arrange
            var transport = CreateTransportHandlerWithRealChannel(out IChannel channel);

            // act
            Func<Task> act = async () =>
            {
                // act
                // Open will attempt to connect to localhost, and get a connect exception. Expected behavior is for this exception to be ignored.
                // However, later in the open call, the lack of an opened channel should throw an IotHubCommunicationException.
                await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            };

            //assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.StatusCode.Should().Be(IotHubStatusCode.NetworkErrors);
        }

        [TestMethod]
        public async Task MqttTransportHandlerOpenAsyncSubscribesToTwinResultTopic()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);

            // act
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            await channel
                .Received()
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals("$iothub/twin/res/#")))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerEnableMethodsAsyncSubscribesSuccessfully()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);

            // act
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.EnableMethodsAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            await channel
                .Received()
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(methodPostTopicFilter)))
                .ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandlerEnableMethodsAsyncSubscribeTimesOut()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);
            channel
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(methodPostTopicFilter)))
                .Returns(x => { throw new TimeoutException(); });

            // act & assert
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.EnableMethodsAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerDisableMethodsAsyncUnsubscribesSuccessfully()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);

            // act
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.DisableMethodsAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            await channel
                .Received()
                .WriteAsync(Arg.Is<UnsubscribePacket>(msg => System.Linq.Enumerable.ElementAt(msg.TopicFilters, 0).Equals(methodPostTopicFilter))).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandlerDisablemethodsAsyncUnsubscribeTimesOut()
        {
            // arrange
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel);
            channel
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(methodPostTopicFilter)))
                .Returns(x => { throw new TimeoutException(); });

            // act & assert
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.EnableMethodsAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandler_EnableEventReceiveAsync_SubscribesSuccessfully()
        {
            // arrange
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel, DummyModuleConnectionString);

            // act
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.EnableEventReceiveAsync(false, CancellationToken.None).ConfigureAwait(false);

            // assert
            string expectedTopicFilter = "devices/FakeDevice/modules/FakeModule/#";
            await channel
                .Received()
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(expectedTopicFilter))).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandler_EnableEventReceiveAsync_SubscribeTimesOut()
        {
            // arrange
            string expectedTopicFilter = "devices/FakeDevice/modules/FakeModule/#";
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel, DummyModuleConnectionString);
            channel
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(expectedTopicFilter)))
                .Returns(x => { throw new TimeoutException(); });

            // act & assert
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.EnableEventReceiveAsync(false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandler_DisableEventReceiveAsync_UnsubscribesSuccessfully()
        {
            // arrange
            string expectedTopicFilter = "devices/FakeDevice/modules/FakeModule/#";
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel, DummyModuleConnectionString);

            // act
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.DisableEventReceiveAsync(false, CancellationToken.None).ConfigureAwait(false);

            // assert
            await channel
                .Received()
                .WriteAsync(Arg.Is<UnsubscribePacket>(msg => Enumerable.ElementAt(msg.TopicFilters, 0).Equals(expectedTopicFilter))).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandler_DisableEventReceiveAsync_UnsubscribeTimesOut()
        {
            // arrange
            string expectedTopicFilter = "devices/FakeDevice/modules/FakeModule/#";
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel, DummyModuleConnectionString);
            channel
                .WriteAsync(Arg.Is<UnsubscribePacket>(msg => Enumerable.ElementAt(msg.TopicFilters, 0).Equals(expectedTopicFilter)))
                .Returns(x => { throw new TimeoutException(); });

            // act & assert
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.DisableEventReceiveAsync(false, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandler_SendMethodResponseAsync_SendsMessage()
        {
            // arrange
            var responseBytes = Encoding.UTF8.GetBytes(fakeMethodResponseBody);
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel);
            var response = new DirectMethodResponse(statusSuccess, responseBytes);
            MessageMatcher matches = (msg) =>
            {
                return StringComparer.InvariantCulture.Equals(msg.MqttTopicName, $"$iothub/methods/res/{statusSuccess}/?$rid={fakeResponseId}");
            };

            // act
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.SendMethodResponseAsync(response, CancellationToken.None).ConfigureAwait(false);

            // assert
            await channel
                .Received().WriteAndFlushAsync(Arg.Is<Message>(msg => matches(msg)))
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerEnableTwinPatchAsyncSubscribes()
        {
            // arrange
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel);

            // act
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.EnableTwinPatchAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            await channel
                .Received()
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(twinPatchDesiredTopicFilter))).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandlerEnableTwinPatchAsyncTimesOut()
        {
            // arrange
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel);
            channel
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(twinPatchDesiredTopicFilter)))
                .Returns(x => { throw new TimeoutException(); });

            // act & assert
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.EnableTwinPatchAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerSendTwinGetAsyncHappyPath()
        {
            // arrange
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel);
            var twin = new Twin();
            twin.Properties.Desired["foo"] = "bar";
            var twinByteStream = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(twin.Properties));
            channel
                .WriteAndFlushAsync(Arg.Is<Message>(msg => msg.MqttTopicName.StartsWith(twinGetTopicPrefix)))
                .Returns(msg =>
                {
                    var response = new Message(twinByteStream);
                    response.MqttTopicName = GetResponseTopic(msg.Arg<Message>().MqttTopicName, statusSuccess);
                    transport.OnMessageReceived(response);
                    return Task.CompletedTask;
                });

            // act
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            var twinReturned = await transport.SendTwinGetAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            Assert.AreEqual<string>(twin.Properties.Desired["foo"].ToString(), twinReturned.Properties.Desired["foo"].ToString());
        }

        [TestMethod]
        public async Task MqttTransportHandlerSendTwinGetAsyncReturnsFailure()
        {
            // arrange
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel);

            channel.WriteAndFlushAsync(Arg.Is<Message>(msg => msg.MqttTopicName.StartsWith(twinGetTopicPrefix)))
                   .Returns(msg =>
                   {
                       var response = new Message();
                       response.MqttTopicName = GetResponseTopic(msg.Arg<Message>().MqttTopicName, statusFailure);
                       transport.OnMessageReceived(response);
                       return Task.CompletedTask;
                   });

            // act & assert
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.SendTwinGetAsync(CancellationToken.None).ExpectedAsync<IotHubClientException>().ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandlerSendTwinGetAsyncTimesOut()
        {
            // arrange
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel);
            transport.TwinTimeout = TimeSpan.FromMilliseconds(20);

            // act & assert
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            var twinReturned = await transport.SendTwinGetAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerSendTwinPatchAsyncHappyPath()
        {
            // arrange
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel);
            var props = new TwinCollection();
            string receivedBody = null;
            props["foo"] = "bar";
            string expectedBody = JsonConvert.SerializeObject(props);

            channel
                .WriteAndFlushAsync(Arg.Is<Message>(msg => msg.MqttTopicName.StartsWith(twinPatchReportedTopicPrefix)))
                .Returns(msg =>
                {
                    var request = msg.Arg<Message>();
                    receivedBody = Encoding.UTF8.GetString(request.Payload);

                    var response = new Message
                    {
                        MqttTopicName = GetResponseTopic(request.MqttTopicName, statusSuccess),
                    };
                    transport.OnMessageReceived(response);

                    return Task.CompletedTask;
                });

            // act
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.SendTwinPatchAsync(props, CancellationToken.None).ConfigureAwait(false);

            // assert
            receivedBody.Should().Be(expectedBody);
        }

        [TestMethod]
        public async Task MqttTransportHandlerSendTwinPatchAsyncReturnsFailure()
        {
            // arrange
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel);
            var props = new TwinCollection();
            channel
                .WriteAndFlushAsync(Arg.Is<Message>(msg => msg.MqttTopicName.StartsWith(twinPatchReportedTopicPrefix)))
                .Returns(msg =>
                {
                    var request = msg.Arg<Message>();
                    var response = new Message();
                    response.MqttTopicName = GetResponseTopic(request.MqttTopicName, statusFailure);
                    transport.OnMessageReceived(response);
                    return Task.CompletedTask;
                });

            // act & assert
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.SendTwinPatchAsync(props, CancellationToken.None).ExpectedAsync<IotHubClientException>().ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandlerSendTwinPatchAsyncTimesOut()
        {
            // arrange
            var transport = this.CreateTransportHandlerWithMockChannel(out IChannel channel);
            transport.TwinTimeout = TimeSpan.FromMilliseconds(20);
            var props = new TwinCollection();

            // act & assert
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.SendTwinPatchAsync(props, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerOnErrorCallConnectionClosedListenerOpen()
        {
            // arrange
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel);

            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            Task task = transport.WaitForTransportClosedAsync();

            // act
            transport.OnError(new Exception("Testing"));

            // assert
            await task.ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task MqttTransportHandlerOnErrorCallConnectionClosedListenerReceiving()
        {
            // arrange
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel);

            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            Task receivingTask = transport.ReceiveMessageAsync(CancellationToken.None);
            Task task = transport.WaitForTransportClosedAsync();

            // act
            transport.OnError(new Exception("Testing"));

            // assert
            await task.ConfigureAwait(false);
            await receivingTask.ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerOnErrorCallConnectionClosedListenerNotInitialized()
        {
            // arrange
            var tcs = new TaskCompletionSource<bool>();
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel);

            // act
            transport.OnError(new Exception("Testing"));

            // assert
            Task t = transport.WaitForTransportClosedAsync();
            await Task.Delay(300).ConfigureAwait(false);
            Assert.IsFalse(t.IsCompleted);

            transport.Dispose();
            Assert.IsTrue(t.IsCompleted);
        }

        [TestMethod]
        public async Task MqttTransportHandler_EnableReceiveMessageAsync_SubscribesSuccessfully()
        {
            // arrange
            string expectedTopicFilter = "devices/FakeDevice/messages/devicebound/#";
            var transport = CreateTransportHandlerWithMockChannel(out IChannel channel, DummyModuleConnectionString);

            // act
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.EnableReceiveMessageAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            await channel
                .Received()
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(expectedTopicFilter))).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandler_DisableReceiveMessageAsync_UnsubscribesSuccessfully()
        {
            // arrange
            IChannel channel;
            string expectedTopicFilter = "devices/FakeDevice/messages/devicebound/#";
            var transport = CreateTransportHandlerWithMockChannel(out channel);

            // act
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.EnableReceiveMessageAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.DisableReceiveMessageAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            await channel
                .Received()
                .WriteAsync(Arg.Is<UnsubscribePacket>(msg => System.Linq.Enumerable.ElementAt(msg.TopicFilters, 0).Equals(expectedTopicFilter))).ConfigureAwait(false);
        }

        private string GetResponseTopic(string requestTopic, int status)
        {
            var index = requestTopic.IndexOf("=");
            var rid = requestTopic.Remove(0, index + 1);

            return $"$iothub/twin/res/{status}/?$rid={rid}";
        }

        private MqttTransportHandler CreateFromConnectionString()
        {
            return new MqttTransportHandler(
                new PipelineContext
                {
                    IotHubConnectionCredentials = new IotHubConnectionCredentials(DummyConnectionString),
                    IotHubClientTransportSettings = new IotHubClientMqttSettings(),
                },
                new IotHubClientMqttSettings());
        }

        private async Task TestOperationCanceledByToken(Func<CancellationToken, Task> asyncMethod)
        {
            using var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            try
            {
                await asyncMethod(tokenSource.Token).ConfigureAwait(false);
                Assert.Fail("Operation did not throw expected exception.");
            }
            catch (OperationCanceledException) { }
        }

        private MqttTransportHandler CreateTransportHandlerWithMockChannel(out IChannel channel, string connectionString = DummyConnectionString)
        {
            var channelMock = Substitute.For<IChannel>();
            channel = channelMock;
            MqttTransportHandler transport = null;

            // The channel factory creates the channel.  This gets called from inside OpenAsync.
            // Unfortunately, it needs access to the internals of the transport (like being able to call OnConnceted, which is passed into the Mqtt channel
            // constructor, but we're not using that)
            Func<IPAddress[], int, Task<IChannel>> factory = (a, i) =>
            {
                transport.OnConnected();
                return Task<IChannel>.FromResult<IChannel>(channelMock);
            };

            transport = new MqttTransportHandler(
                new PipelineContext
                {
                    IotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString),
                    IotHubClientTransportSettings = new IotHubClientMqttSettings(),
                },
                new IotHubClientMqttSettings(),
                factory);
            return transport;
        }

        private MqttTransportHandler CreateTransportHandlerWithRealChannel(out IChannel channel, string connectionString = DummyConnectionString)
        {
            var _channel = Substitute.For<IChannel>();
            channel = _channel;
            return new MqttTransportHandler(
                new PipelineContext
                {
                    IotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString),
                    IotHubClientTransportSettings = new IotHubClientMqttSettings(),
                },
                new IotHubClientMqttSettings(),
                null);
        }
    }
}
