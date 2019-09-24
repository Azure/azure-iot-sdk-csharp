﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test.Transport
{
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;
    using System.Net;
    using DotNetty.Transport.Channels;
    using NSubstitute;
    using DotNetty.Codecs.Mqtt.Packets;
    using Newtonsoft.Json;
    using System.IO;
    using System.Linq;
    using Exceptions;
    using Client.Transport;
    using DotNetty.Common.Concurrency;
    using Microsoft.Azure.Devices.Client.Test.ConnectionString;

    [TestClass]
    [TestCategory("Unit")]
    public class MqttTransportHandlerTests
    {
        const string DummyConnectionString = "HostName=127.0.0.1;SharedAccessKeyName=AllAccessKey;DeviceId=FakeDevice;SharedAccessKey=dGVzdFN0cmluZzE=";
        const string DummyModuleConnectionString = "HostName=127.0.0.1;SharedAccessKeyName=AllAccessKey;DeviceId=FakeDevice;ModuleId=FakeModule;SharedAccessKey=dGVzdFN0cmluZzE=";
        const string fakeMethodResponseBody = "{ \"foo\" : \"bar\" }";
        const string methodPostTopicFilter = "$iothub/methods/POST/#";
        const string twinPatchDesiredTopicFilter = "$iothub/twin/PATCH/properties/desired/#";
        const string twinPatchReportedTopicPrefix = "$iothub/twin/PATCH/properties/reported/";
        const string twinGetTopicPrefix = "$iothub/twin/GET/?$rid=";
        const int statusSuccess = 200;
        const int statusFailure = 400;
        const string fakeResponseId = "fakeResponseId";

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
            await TestOperationCanceledByToken(token => CreateFromConnectionString().ReceiveAsync(token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MqttTransportHandlerCompleteAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().CompleteAsync(Guid.NewGuid().ToString(), token)).ConfigureAwait(false);
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
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendMethodResponseAsync(new MethodResponseInternal(), token)).ConfigureAwait(false);
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

        MqttTransportHandler CreateFromConnectionString()
        {
            return new MqttTransportHandler(
                new PipelineContext(),
                IotHubConnectionStringExtensions.Parse(DummyConnectionString),
                new MqttTransportSettings(TransportType.Mqtt_Tcp_Only));
        }

        async Task TestOperationCanceledByToken(Func<CancellationToken, Task> asyncMethod)
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            try
            {
                await asyncMethod(tokenSource.Token).ConfigureAwait(false);
                Assert.Fail("Operation did not throw expected exception.");
            }
            catch (OperationCanceledException) { }
        }

        MqttTransportHandler CreateTransportHandlerWithMockChannel(out IChannel channel, string connectionString = DummyConnectionString)
        {
            var _channel = Substitute.For<IChannel>();
            channel = _channel;
            MqttTransportHandler transport = null;

            // The channel factory creates the channel.  This gets called from inside OpenAsync.  
            // Unfortunately, it needs access to the internals of the transport (like being able to call OnConnceted, which is passed into the Mqtt channel constructor, but we're not using that)
            Func<IPAddress[], int, Task<IChannel>> factory = (a, i) =>
            {
                transport.OnConnected();
                return Task<IChannel>.FromResult<IChannel>(_channel);
            };

            transport = new MqttTransportHandler(new PipelineContext(), IotHubConnectionStringExtensions.Parse(connectionString), new MqttTransportSettings(Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only), factory);
            return transport;
        }


        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_031: `OpenAsync` shall subscribe using the '$iothub/twin/res/#' topic filter
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
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals("$iothub/twin/res/#"))).ConfigureAwait(false);
        }


        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_001: `EnableMethodsAsync` shall subscribe using the '$iothub/methods/POST/' topic filter.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_032: `EnableMethodsAsync` shall open the transport if this method is called when the transport is not open.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_002: `EnableMethodsAsync` shall wait for a SUBACK for the subscription request.
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
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(methodPostTopicFilter))).ConfigureAwait(false);
        }


        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_003: `EnableMethodsAsync` shall return failure if the subscription request fails.
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

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_28_001: `DisableMethodsAsync` shall unsubscribe using the '$iothub/methods/POST/' topic filter.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_28_002: `DisableMethodsAsync` shall wait for a UNSUBACK for the unsubscription.
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

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_28_003: `DisableMethodsAsync` shall return failure if the unsubscription fails.
        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandlerDisablemethodsAsyncUnsubscribeTimesOut()
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

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_33_021: `EnableMethodsAsync` shall subscribe using the 'devices/{0}/modules/{1}/#' topic filter.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_33_020: `EnableMethodsAsync` shall open the transport if this method is called when the transport is not open.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_33_022: `EnableMethodsAsync` shall wait for a SUBACK for the subscription request.
        [TestMethod]
        public async Task MqttTransportHandler_EnableEventReceiveAsync_SubscribesSuccessfully()
        {
            // arrange
            IChannel channel;

            var transport = CreateTransportHandlerWithMockChannel(out channel, DummyModuleConnectionString);

            // act
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.EnableEventReceiveAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            string expectedTopicFilter = "devices/FakeDevice/modules/FakeModule/#";
            await channel
                .Received()
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(expectedTopicFilter))).ConfigureAwait(false);
        }


        // Tests_SRS_CSHARP_MQTT_TRANSPORT_33_023: `EnableMethodsAsync` shall return failure if the subscription request fails.
        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandler_EnableEventReceiveAsync_SubscribeTimesOut()
        {
            // arrange
            IChannel channel;
            string expectedTopicFilter = "devices/FakeDevice/modules/FakeModule/#";
            var transport = CreateTransportHandlerWithMockChannel(out channel, DummyModuleConnectionString);
            channel
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(expectedTopicFilter)))
                .Returns(x => { throw new TimeoutException(); });

            // act & assert
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.EnableEventReceiveAsync(CancellationToken.None).ConfigureAwait(false);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_33_021: `DisableEventReceiveAsync` shall unsubscribe using the 'devices/{0}/modules/{1}/#' topic filter.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_33_022: `DisableEventReceiveAsync` shall wait for a UNSUBACK for the unsubscription.
        [TestMethod]
        public async Task MqttTransportHandler_DisableEventReceiveAsync_UnsubscribesSuccessfully()
        {
            // arrange
            IChannel channel;
            string expectedTopicFilter = "devices/FakeDevice/modules/FakeModule/#";
            var transport = CreateTransportHandlerWithMockChannel(out channel, DummyModuleConnectionString);

            // act
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.DisableEventReceiveAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            await channel
                .Received()
                .WriteAsync(Arg.Is<UnsubscribePacket>(msg => Enumerable.ElementAt(msg.TopicFilters, 0).Equals(expectedTopicFilter))).ConfigureAwait(false);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_33_023: `DisableEventReceiveAsync` shall return failure if the unsubscription fails.
        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandler_DisableEventReceiveAsync_UnsubscribeTimesOut()
        {
            // arrange
            IChannel channel;
            string expectedTopicFilter = "devices/FakeDevice/modules/FakeModule/#";
            var transport = CreateTransportHandlerWithMockChannel(out channel, DummyModuleConnectionString);
            channel.WriteAsync(Arg.Is<UnsubscribePacket>(msg => Enumerable.ElementAt(msg.TopicFilters, 0).Equals(expectedTopicFilter)))
                .Returns(x => { throw new TimeoutException(); });

            // act & assert
            transport.OnConnected();
            await transport.OpenAsync( CancellationToken.None).ConfigureAwait(false);
            await transport.DisableEventReceiveAsync(CancellationToken.None).ConfigureAwait(false);
        }

        delegate bool MessageMatcher(Message msg);
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_005: `SendMethodResponseAsync` shall allocate a `Message` object containing the method response.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_006: `SendMethodResponseAsync` shall set the message topic to '$iothub/methods/res/<STATUS>/?$rid=<REQUEST_ID>' where STATUS is the return status for the method and REQUEST_ID is the request ID received from the service in the original method call.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_007: `SendMethodResponseAsync` shall set the message body to the response payload of the `Method` object.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_008: `SendMethodResponseAsync` shall send the message to the service.
        [TestMethod]
        public async Task MqttTransportHandlerSendMethodResponseAsyncSendsMessage()
        {
            // arrange
            IChannel channel;
            var responseBytes = System.Text.Encoding.UTF8.GetBytes(fakeMethodResponseBody);
            var transport = CreateTransportHandlerWithMockChannel(out channel);
            var response = new MethodResponseInternal(responseBytes, fakeResponseId, statusSuccess);

            // act
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.SendMethodResponseAsync(response, CancellationToken.None).ConfigureAwait(false);

            // assert
            MessageMatcher matches = (msg) =>
            {
                using (StreamReader reader = new StreamReader(msg.GetBodyStream(), System.Text.Encoding.UTF8))
                {
                    string body = reader.ReadToEnd();

                    return (fakeMethodResponseBody.Equals(body) &&
                        msg.MqttTopicName.Equals("$iothub/methods/res/"+statusSuccess+"/?$rid="+fakeResponseId));
                }
            };
            await channel
                .Received()
                .WriteAndFlushAsync(Arg.Is<Message>(msg => matches(msg))).ConfigureAwait(false);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_010: `EnableTwinPatchAsync` shall subscribe using the '$iothub/twin/PATCH/properties/desired/#' topic filter.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_011: `EnableTwinPatchAsync` shall wait for a SUBACK on the subscription request.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_033: `EnableTwinPatchAsync` shall open the transport  if this method is called when the transport is not open.
        [TestMethod]
        public async Task MqttTransportHandlerEnableTwinPatchAsyncSubscribes()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);

            // act
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.EnableTwinPatchAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            await channel
                .Received()
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(twinPatchDesiredTopicFilter))).ConfigureAwait(false);
        }
        
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_012: `EnableTwinPatchAsync` shall return failure if the subscription request fails.
        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandlerEnableTwinPatchAsyncTimesOut()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);
            channel
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(twinPatchDesiredTopicFilter)))
                .Returns(x => { throw new TimeoutException(); });

            // act & assert
            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.EnableTwinPatchAsync(CancellationToken.None).ConfigureAwait(false);
        }

        string getResponseTopic(string requestTopic, int status)
        {
            var index = requestTopic.IndexOf("=");
            var rid = requestTopic.Remove(0, index + 1);

            return "$iothub/twin/res/" + status + "/?$rid=" + rid;
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_014: `SendTwinGetAsync` shall allocate a `Message` object to hold the `GET` request
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_015: `SendTwinGetAsync` shall generate a GUID to use as the $rid property on the request
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_016: `SendTwinGetAsync` shall set the `Message` topic to '$iothub/twin/GET/?$rid=<REQUEST_ID>' where REQUEST_ID is the GUID that was generated
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_017: `SendTwinGetAsync` shall wait for a response from the service with a matching $rid value
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_018: When a response is received, `SendTwinGetAsync` shall return the Twin object to the caller.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_034: `SendTwinGetAsync` shall shall open the transport  if this method is called when the transport is not open.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_021: If the response contains a success code, `SendTwinGetAsync` shall return success to the caller 
        [TestMethod]
        public async Task MqttTransportHandlerSendTwinGetAsyncHappyPath()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);
            var twin = new Twin();
            twin.Properties.Desired["foo"] = "bar";
            var twinByteStream = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(twin.Properties));
            channel
                .WriteAndFlushAsync(Arg.Is<Message>(msg => msg.MqttTopicName.StartsWith(twinGetTopicPrefix)))
                .Returns(msg =>
                {
                    var response = new Message(twinByteStream);
                    response.MqttTopicName = getResponseTopic(msg.Arg<Message>().MqttTopicName, statusSuccess);
                    transport.OnMessageReceived(response);
                    return TaskHelpers.CompletedTask;
                });

            // act
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            var twinReturned = await transport.SendTwinGetAsync(CancellationToken.None).ConfigureAwait(false);

            // assert
            Assert.AreEqual<string>(twin.Properties.Desired["foo"].ToString(), twinReturned.Properties.Desired["foo"].ToString());
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_019: If the response is failed, `SendTwinGetAsync` shall return that failure to the caller.
        [TestMethod]
        public async Task MqttTransportHandlerSendTwinGetAsyncReturnsFailure()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);

            channel.WriteAndFlushAsync(Arg.Is<Message>(msg => msg.MqttTopicName.StartsWith(twinGetTopicPrefix)))
                   .Returns(msg =>
                   {
                       var response = new Message();
                       response.MqttTopicName = getResponseTopic(msg.Arg<Message>().MqttTopicName, statusFailure);
                       transport.OnMessageReceived(response);
                       return TaskHelpers.CompletedTask;
                   });

            // act & assert
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.SendTwinGetAsync(CancellationToken.None).ExpectedAsync<IotHubException>().ConfigureAwait(false);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_020: If the response doesn't arrive within `MqttTransportHandler.TwinTimeout`, `SendTwinGetAsync` shall fail with a timeout error
        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandlerSendTwinGetAsyncTimesOut()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);
            transport.TwinTimeout = TimeSpan.FromMilliseconds(20);

            // act & assert
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            var twinReturned = await transport.SendTwinGetAsync(CancellationToken.None).ConfigureAwait(false);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_022: `SendTwinPatchAsync` shall allocate a `Message` object to hold the update request
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_023: `SendTwinPatchAsync` shall generate a GUID to use as the $rid property on the request
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_024: `SendTwinPatchAsync` shall set the `Message` topic to '$iothub/twin/PATCH/properties/reported/?$rid=<REQUEST_ID>' where REQUEST_ID is the GUID that was generated
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_025: `SendTwinPatchAsync` shall serialize the `reportedProperties` object into a JSON string
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_026: `SendTwinPatchAsync` shall set the body of the message to the JSON string
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_027: `SendTwinPatchAsync` shall wait for a response from the service with a matching $rid value
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_030: If the response contains a success code, `SendTwinPatchAsync` shall return success to the caller.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_035: `SendTwinPatchAsync` shall shall open the transport if this method is called when the transport is not open.
        [TestMethod]
        public async Task MqttTransportHandlerSendTwinPatchAsyncHappyPath()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);
            var props = new TwinCollection();
            string receivedBody = null;
            props["foo"] = "bar";
            channel
                .WriteAndFlushAsync(Arg.Is<Message>(msg => msg.MqttTopicName.StartsWith(twinPatchReportedTopicPrefix)))
                .Returns(msg =>
                {
                    var request = msg.Arg<Message>();
                    using (StreamReader reader = new StreamReader(request.GetBodyStream(), System.Text.Encoding.UTF8))
                    {
                        receivedBody = reader.ReadToEnd();
                    }
                    var response = new Message();
                    response.MqttTopicName = getResponseTopic(request.MqttTopicName, statusSuccess);
                    transport.OnMessageReceived(response);
                    return TaskHelpers.CompletedTask;
                });

            // act
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.SendTwinPatchAsync(props, CancellationToken.None).ConfigureAwait(false);

            // assert
            string expectedBody = JsonConvert.SerializeObject(props);
            Assert.AreEqual<string>(expectedBody, receivedBody);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_028: If the response is failed, `SendTwinPatchAsync` shall return that failure to the caller.
        [TestMethod]
        public async Task MqttTransportHandlerSendTwinPatchAsyncReturnsFailure()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);
            var props = new TwinCollection();
            channel
                .WriteAndFlushAsync(Arg.Is<Message>(msg => msg.MqttTopicName.StartsWith(twinPatchReportedTopicPrefix)))
                .Returns(msg =>
                {
                    var request = msg.Arg<Message>();
                    var response = new Message();
                    response.MqttTopicName = getResponseTopic(request.MqttTopicName, statusFailure);
                    transport.OnMessageReceived(response);
                    return TaskHelpers.CompletedTask;
                });

            // act & assert
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.SendTwinPatchAsync(props, CancellationToken.None).ExpectedAsync<IotHubException>().ConfigureAwait(false);

        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_029: If the response doesn't arrive within `MqttTransportHandler.TwinTimeout`, `SendTwinPatchAsync` shall fail with a timeout error. 
        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandlerSendTwinPatchAsyncTimesOut()
        {
            // arrange
            IChannel channel;
            var transport = this.CreateTransportHandlerWithMockChannel(out channel);
            transport.TwinTimeout = TimeSpan.FromMilliseconds(20);
            var props = new TwinCollection();

            // act & assert
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.SendTwinPatchAsync(props, CancellationToken.None).ConfigureAwait(false);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_28_04: If OnError is triggered after OpenAsync is called, WaitForTransportClosedAsync shall be invoked.
        [TestMethod]
        public async Task MqttTransportHandlerOnErrorCallConnectionClosedListenerOpen()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);

            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            Task task = transport.WaitForTransportClosedAsync();

            // act
            transport.OnError(new ApplicationException("Testing"));

            // assert
            await task.ConfigureAwait(false);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_28_05: If OnError is triggered after ReceiveAsync is called, WaitForTransportClosedAsync shall be invoked.
        [TestMethod]
        public async Task MqttTransportHandlerOnErrorCallConnectionClosedListenerReceiving()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);

            transport.OnConnected();
            await transport.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await transport.ReceiveAsync( CancellationToken.None).ConfigureAwait(false);
            Task task = transport.WaitForTransportClosedAsync();

            // act
            transport.OnError(new ApplicationException("Testing"));

            // assert
            await task.ConfigureAwait(false);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_28_06: If OnError is triggered without any prior operation, WaitForTransportClosedAsync shall not be invoked.
        [TestMethod]
        public async Task MqttTransportHandlerOnErrorCallConnectionClosedListenerNotInitialized()
        {
            // arrange
            IChannel channel;
            var tcs = new TaskCompletionSource<bool>();
            var transport = CreateTransportHandlerWithMockChannel(out channel);

            // act
            transport.OnError(new ApplicationException("Testing"));

            // assert
            Task t = transport.WaitForTransportClosedAsync();
            await Task.Delay(300).ConfigureAwait(false);
            Assert.IsFalse(t.IsCompleted);

            transport.Dispose();
            Assert.IsTrue(t.IsCompleted);
        }
    }
}
