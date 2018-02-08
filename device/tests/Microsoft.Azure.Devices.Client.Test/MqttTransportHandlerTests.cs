using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test.Transport.Mqtt
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
    public class MqttTransportHandlerTests
    {
        const string DummyConnectionString = "HostName=127.0.0.1;SharedAccessKeyName=AllAccessKey;DeviceId=FakeDevice;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
        const string DummyModuleConnectionString = "HostName=127.0.0.1;SharedAccessKeyName=AllAccessKey;DeviceId=FakeDevice;ModuleId=FakeModule;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
        const string fakeMethodResponseBody = "{ \"foo\" : \"bar\" }";
        const string methodPostTopicFilter = "$iothub/methods/POST/#";
        const string twinPatchDesiredTopicFilter = "$iothub/twin/PATCH/properties/desired/#";
        const string twinPatchReportedTopicPrefix = "$iothub/twin/PATCH/properties/reported/";
        const string twinGetTopicPrefix = "$iothub/twin/GET/?$rid=";
        const int statusSuccess = 200;
        const int statusFailure = 400;
        const string fakeResponseId = "fakeResponseId";

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task MqttTransportHandler_OpenAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().OpenAsync(true, token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task MqttTransportHandler_SendEventAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendEventAsync(new Message(), token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task MqttTransportHandler_ReceiveAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().ReceiveAsync(new TimeSpan(0, 10, 0), token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task MqttTransportHandler_CompleteAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().CompleteAsync(Guid.NewGuid().ToString(), token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task MqttTransportHandler_EnableMethodsAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().EnableMethodsAsync(token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task MqttTransportHandler_DisableMethodsAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().DisableMethodsAsync(token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task MqttTransportHandler_SendMethodResponseAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendMethodResponseAsync(new MethodResponseInternal(), token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task MqttTransportHandler_EnableTwinPatchAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().EnableTwinPatchAsync(token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task MqttTransportHandler_SendTwinGetAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendTwinGetAsync(token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task MqttTransportHandler_SendTwinPatchAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendTwinPatchAsync(new TwinCollection(), token));
        }

        MqttTransportHandler CreateFromConnectionString()
        {
            return new MqttTransportHandler(
                new PipelineContext(),
                IotHubConnectionStringExtensions.Parse(DummyConnectionString),
                new MqttTransportSettings(Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only),
                (o, ea) => { },
                (o, ea) => { return TaskHelpers.CompletedTask; });
        }

        async Task TestOperationCanceledByToken(Func<CancellationToken, Task> asyncMethod)
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            try
            {
                await asyncMethod(tokenSource.Token);
            }
            catch (SocketException)
            {
                Assert.Fail("Fail to skip execution of this operation using cancellation token.");
            }
        }

        MqttTransportHandler CreateTransportHandlerWithMockChannel(out IChannel channel)
        {
            return CreateTransportHandlerWithMockChannel(out channel, (o, ea) => { }, (o, ea) => { return TaskHelpers.CompletedTask; });
        }

        MqttTransportHandler CreateTransportHandlerWithMockChannel(string connectionString, out IChannel channel)
        {
            return CreateTransportHandlerWithMockChannel(out channel, (o, ea) => { }, (o, ea) => { return TaskHelpers.CompletedTask; }, connectionString);
        }

        MqttTransportHandler CreateTransportHandlerWithMockChannel(out IChannel channel, Action<object, ConnectionEventArgs> onConnectionOpenedCallback, Func<object, ConnectionEventArgs, Task> onConnectionClosedCallback, string connectionString = DummyConnectionString)
        {
            var _channel = Substitute.For<IChannel>();
            channel = _channel;
            MqttTransportHandler transport = null;

            // The channel factory creates the channel.  This gets called from inside OpenAsync.  
            // Unfortunately, it needs access to the internals of the transport (like being able to call OnConnceted, which is passed into the Mqtt channel constructor, but we're not using that)
            Func<IPAddress, int, Task<IChannel>> factory = (a, i) =>
            {
                transport.OnConnected();
                return Task<IChannel>.FromResult<IChannel>(_channel);
            };

            transport = new MqttTransportHandler(new PipelineContext(), IotHubConnectionStringExtensions.Parse(connectionString), new MqttTransportSettings(Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only), factory, onConnectionOpenedCallback, onConnectionClosedCallback);
            return transport;
        }


        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_031: `OpenAsync` shall subscribe using the '$iothub/twin/res/#' topic filter
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Twin")]
        public async Task MqttTransportHandler_OpenAsync_SubscribesToTwinResultTopic()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);

            // act
            transport.OnConnected();
            await transport.OpenAsync(true, CancellationToken.None);

            // assert
            await channel
                .Received()
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals("$iothub/twin/res/#")));
        }


        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_001: `EnableMethodsAsync` shall subscribe using the '$iothub/methods/POST/' topic filter.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_032: `EnableMethodsAsync` shall open the transport if this method is called when the transport is not open.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_002: `EnableMethodsAsync` shall wait for a SUBACK for the subscription request.
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Methods")]
        public async Task MqttTransportHandler_EnableMethodsAsync_SubscribesSuccessfully()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);

            // act
            transport.OnConnected();
            await transport.EnableMethodsAsync(CancellationToken.None);

            // assert
            await channel
                .Received()
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(methodPostTopicFilter)));
        }


        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_003: `EnableMethodsAsync` shall return failure if the subscription request fails.
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Methods")]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandler_EnableMethodsAsync_SubscribeTimesOut()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);
            channel
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(methodPostTopicFilter)))
                .Returns(x => { throw new TimeoutException(); });

            // act & assert
            transport.OnConnected();
            await transport.EnableMethodsAsync(CancellationToken.None);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_28_001: `DisableMethodsAsync` shall unsubscribe using the '$iothub/methods/POST/' topic filter.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_28_002: `DisableMethodsAsync` shall wait for a UNSUBACK for the unsubscription.
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Methods")]
        public async Task MqttTransportHandler_DisableMethodsAsync_UnsubscribesSuccessfully()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);

            // act
            transport.OnConnected();
            await transport.OpenAsync(true, CancellationToken.None);
            await transport.DisableMethodsAsync(CancellationToken.None);

            // assert
            await channel
                .Received()
                .WriteAsync(Arg.Is<UnsubscribePacket>(msg => System.Linq.Enumerable.ElementAt<String>(msg.TopicFilters, 0).Equals(methodPostTopicFilter)));
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_28_003: `DisableMethodsAsync` shall return failure if the unsubscription fails.
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Methods")]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandler_DisablemethodsAsync_UnsubscribeTimesOut()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);
            channel
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(methodPostTopicFilter)))
                .Returns(x => { throw new TimeoutException(); });


            // act & assert
            transport.OnConnected();
            await transport.EnableMethodsAsync(CancellationToken.None);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_33_021: `EnableMethodsAsync` shall subscribe using the 'devices/{0}/modules/{1}/#' topic filter.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_33_020: `EnableMethodsAsync` shall open the transport if this method is called when the transport is not open.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_33_022: `EnableMethodsAsync` shall wait for a SUBACK for the subscription request.
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Methods")]
        public async Task MqttTransportHandler_EnableEventReceiveAsync_SubscribesSuccessfully()
        {
            // arrange
            IChannel channel;

            var transport = CreateTransportHandlerWithMockChannel(DummyModuleConnectionString, out channel);

            // act
            transport.OnConnected();
            await transport.EnableEventReceiveAsync(CancellationToken.None);

            // assert
            string expectedTopicFilter = "devices/FakeDevice/modules/FakeModule/#";
            await channel
                .Received()
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(expectedTopicFilter)));
        }


        // Tests_SRS_CSHARP_MQTT_TRANSPORT_33_023: `EnableMethodsAsync` shall return failure if the subscription request fails.
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Methods")]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandler_EnableEventReceiveAsync_SubscribeTimesOut()
        {
            // arrange
            IChannel channel;
            string expectedTopicFilter = "devices/FakeDevice/modules/FakeModule/#";
            var transport = CreateTransportHandlerWithMockChannel(DummyModuleConnectionString, out channel);
            channel
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(expectedTopicFilter)))
                .Returns(x => { throw new TimeoutException(); });

            // act & assert
            transport.OnConnected();
            await transport.EnableEventReceiveAsync(CancellationToken.None);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_33_021: `DisableEventReceiveAsync` shall unsubscribe using the 'devices/{0}/modules/{1}/#' topic filter.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_33_022: `DisableEventReceiveAsync` shall wait for a UNSUBACK for the unsubscription.
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Methods")]
        public async Task MqttTransportHandler_DisableEventReceiveAsync_UnsubscribesSuccessfully()
        {
            // arrange
            IChannel channel;
            string expectedTopicFilter = "devices/FakeDevice/modules/FakeModule/#";
            var transport = CreateTransportHandlerWithMockChannel(DummyModuleConnectionString, out channel);

            // act
            transport.OnConnected();
            await transport.OpenAsync(true, CancellationToken.None);
            await transport.DisableEventReceiveAsync(CancellationToken.None);

            // assert
            await channel
                .Received()
                .WriteAsync(Arg.Is<UnsubscribePacket>(msg => Enumerable.ElementAt(msg.TopicFilters, 0).Equals(expectedTopicFilter)));
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_33_023: `DisableEventReceiveAsync` shall return failure if the unsubscription fails.
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Methods")]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandler_DisableEventReceiveAsync_UnsubscribeTimesOut()
        {
            // arrange
            IChannel channel;
            string expectedTopicFilter = "devices/FakeDevice/modules/FakeModule/#";
            var transport = CreateTransportHandlerWithMockChannel(DummyModuleConnectionString, out channel);
            channel.WriteAsync(Arg.Is<UnsubscribePacket>(msg => Enumerable.ElementAt(msg.TopicFilters, 0).Equals(expectedTopicFilter)))
                .Returns(x => { throw new TimeoutException(); });

            // act & assert
            transport.OnConnected();
            await transport.OpenAsync(true, CancellationToken.None);
            await transport.DisableEventReceiveAsync(CancellationToken.None);
        }

        delegate bool MessageMatcher(Message msg);
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_005: `SendMethodResponseAsync` shall allocate a `Message` object containing the method response.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_006: `SendMethodResponseAsync` shall set the message topic to '$iothub/methods/res/<STATUS>/?$rid=<REQUEST_ID>' where STATUS is the return status for the method and REQUEST_ID is the request ID received from the service in the original method call.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_007: `SendMethodResponseAsync` shall set the message body to the response payload of the `Method` object.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_008: `SendMethodResponseAsync` shall send the message to the service.
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Methods")]
        public async Task MqttTransportHandler_SendMethodResponseAsync_SendsMessage()
        {
            // arrange
            IChannel channel;
            var responseBytes = System.Text.Encoding.UTF8.GetBytes(fakeMethodResponseBody);
            var transport = CreateTransportHandlerWithMockChannel(out channel);
            var response = new MethodResponseInternal(responseBytes, fakeResponseId, statusSuccess);

            // act
            transport.OnConnected();
            await transport.SendMethodResponseAsync(response, CancellationToken.None);

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
                .WriteAndFlushAsync(Arg.Is<Message>(msg => matches(msg)));
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_010: `EnableTwinPatchAsync` shall subscribe using the '$iothub/twin/PATCH/properties/desired/#' topic filter.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_011: `EnableTwinPatchAsync` shall wait for a SUBACK on the subscription request.
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_033: `EnableTwinPatchAsync` shall open the transport  if this method is called when the transport is not open.
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Twin")]
        public async Task MqttTransportHandler_EnableTwinPatchAsync_Subscribes()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);

            // act
            transport.OnConnected();
            await transport.EnableTwinPatchAsync(CancellationToken.None);

            // assert
            await channel
                .Received()
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(twinPatchDesiredTopicFilter)));
        }
        
        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_012: `EnableTwinPatchAsync` shall return failure if the subscription request fails.
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Twin")]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandler_EnableTwinPatchAsync_TimesOut()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);
            channel
                .WriteAsync(Arg.Is<SubscribePacket>(msg => msg.Requests[0].TopicFilter.Equals(twinPatchDesiredTopicFilter)))
                .Returns(x => { throw new TimeoutException(); });

            // act & assert
            transport.OnConnected();
            await transport.EnableTwinPatchAsync(CancellationToken.None);
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
        [TestCategory("TransportHandlers")]
        [TestCategory("Twin")]
        public async Task MqttTransportHandler_SendTwinGetAsync_HappyPath()
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
            var twinReturned = await transport.SendTwinGetAsync(CancellationToken.None);

            // assert
            Assert.AreEqual<string>(twin.Properties.Desired["foo"].ToString(), twinReturned.Properties.Desired["foo"].ToString());
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_019: If the response is failed, `SendTwinGetAsync` shall return that failure to the caller.
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Twin")]
        [ExpectedException(typeof(Exception))]
        public async Task MqttTransportHandler_SendTwinGetAsync_ReturnsFailure()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);
            channel
                .WriteAndFlushAsync(Arg.Is<Message>(msg => msg.MqttTopicName.StartsWith(twinGetTopicPrefix)))
                .Returns(msg =>
                {
                    var response = new Message();
                    response.MqttTopicName = getResponseTopic(msg.Arg<Message>().MqttTopicName, statusFailure);
                    transport.OnMessageReceived(response);
                    return TaskHelpers.CompletedTask;
                });

            // act & assert
            var twinReturned = await transport.SendTwinGetAsync(CancellationToken.None);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_020: If the response doesn't arrive within `MqttTransportHandler.TwinTimeout`, `SendTwinGetAsync` shall fail with a timeout error
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Twin")]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandler_SendTwinGetAsync_TimesOut()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);
            transport.TwinTimeout = TimeSpan.FromMilliseconds(20);

            // act & assert
            var twinReturned = await transport.SendTwinGetAsync(CancellationToken.None);
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
        [TestCategory("TransportHandlers")]
        [TestCategory("Twin")]
        public async Task MqttTransportHandler_SendTwinPatchAsync_HappyPath()
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
            await transport.SendTwinPatchAsync(props, CancellationToken.None);

            // assert
            string expectedBody = JsonConvert.SerializeObject(props);
            Assert.AreEqual<string>(expectedBody, receivedBody);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_028: If the response is failed, `SendTwinPatchAsync` shall return that failure to the caller.
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Twin")]
        [ExpectedException(typeof(Exception))]
        public async Task MqttTransportHandler_SendTwinPatchAsync_ReturnsFailure()
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
            await transport.SendTwinPatchAsync(props, CancellationToken.None);

        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_18_029: If the response doesn't arrive within `MqttTransportHandler.TwinTimeout`, `SendTwinPatchAsync` shall fail with a timeout error. 
        [TestMethod]
        [TestCategory("TransportHandlers")]
        [TestCategory("Twin")]
        [ExpectedException(typeof(TimeoutException))]
        public async Task MqttTransportHandler_SendTwinPatchAsync_TimesOut()
        {
            // arrange
            IChannel channel;
            var transport = this.CreateTransportHandlerWithMockChannel(out channel);
            transport.TwinTimeout = TimeSpan.FromMilliseconds(20);
            var props = new TwinCollection();

            // act & assert
            await transport.SendTwinPatchAsync(props, CancellationToken.None);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_28_04: If OnError is triggered after OpenAsync is called, onConnectionClosedCallback shall be invoked.
        public async Task MqttTransportHandler_OnError_CallConnectionClosedListener_Open()
        {
            // arrange
            IChannel channel;
            var tcs = new TaskCompletionSource<bool>();
            var transport = CreateTransportHandlerWithMockChannel(
                out channel, 
                (o, ea) => { }, 
                (o, ea) => {
                    tcs.SetResult(true);
                    return TaskHelpers.CompletedTask;
                });
            transport.OnConnected();
            await transport.OpenAsync(true, CancellationToken.None);

            // act
            await Task.Run(() => transport.OnError(new ApplicationException("Testing")));

            // assert
            await Task.WhenAny(
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    tcs.SetResult(false);
                }), tcs.Task);

            Assert.IsTrue(tcs.Task.Result);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_28_05: If OnError is triggered after ReceiveAsync is called, onConnectionClosedCallback shall be invoked.
        [TestMethod]
        public async Task MqttTransportHandler_OnError_CallConnectionClosedListener_Receiving()
        {
            // arrange
            IChannel channel;
            var tcs = new TaskCompletionSource<bool>();
            var transport = CreateTransportHandlerWithMockChannel(
                out channel, 
                (o, ea) => { },
                (o, ea) =>
                {
                    tcs.SetResult(true);
                    return TaskHelpers.CompletedTask;
                });
            transport.OnConnected();
            await transport.OpenAsync(true, CancellationToken.None);
            await transport.ReceiveAsync(new TimeSpan(0, 0, 0, 0, 5), CancellationToken.None);

            // act
            await Task.Run(() => transport.OnError(new ApplicationException("Testing")));

            // assert
            await Task.WhenAny(
                Task.Run(async () => 
                {
                    await Task.Delay(1000);
                    tcs.SetResult(false);
                }), tcs.Task);
            
            Assert.IsTrue(tcs.Task.Result);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_28_06: If OnError is triggered without any prior operation, onConnectionClosedCallback shall not be invoked.
        [TestMethod]
        public async Task MqttTransportHandler_OnError_CallConnectionClosedListener_NotInitialized()
        {
            // arrange
            IChannel channel;
            var tcs = new TaskCompletionSource<bool>();
            var transport = CreateTransportHandlerWithMockChannel(
                out channel, 
                (o, ea) => { },
                (o, ea) =>
                {
                    tcs.SetResult(true);
                    return TaskHelpers.CompletedTask;
                });

            // act
            await Task.Run(() => transport.OnError(new ApplicationException("Testing")));

            // assert
            await Task.WhenAny(
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    tcs.SetResult(false);
                }), tcs.Task);

            Assert.IsFalse(tcs.Task.Result);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_28_07: If OnError is triggered in error state, onConnectionClosedCallback shall not be invoked.
        [TestMethod]
        public async Task MqttTransportHandler_OnError_CallConnectionClosedListener_Error()
        {
            // arrange
            IChannel channel;
            var tcs = new TaskCompletionSource<bool>();
            var transport = CreateTransportHandlerWithMockChannel(
                out channel, 
                (o, ea) => { },
                (o, ea) =>
                {
                    tcs.SetResult(true);
                    return TaskHelpers.CompletedTask;
                });
            transport.OnError(new ApplicationException("Testing"));

            // act
            await Task.Run(() => transport.OnError(new ApplicationException("Testing")));

            // assert
            await Task.WhenAny(
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    tcs.SetResult(false);
                }), tcs.Task);

            Assert.IsFalse(tcs.Task.Result);
        }

        // Tests_SRS_CSHARP_MQTT_TRANSPORT_28_08: [** `RecoverConnections` shall throw IotHubClientException exception when in error state.
        [TestMethod]
        [ExpectedException(typeof(IotHubClientException))]
        public async Task MqttTransportHandler_RecoverConnections_throw()
        {
            // arrange
            IChannel channel;
            var transport = CreateTransportHandlerWithMockChannel(out channel);
            transport.OnError(new ApplicationException("Testing"));

            // act
            await transport.RecoverConnections(null, ConnectionType.MqttConnection, CancellationToken.None);
        }
    }

}
