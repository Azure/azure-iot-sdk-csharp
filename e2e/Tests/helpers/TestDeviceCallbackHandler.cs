// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal sealed class TestDeviceCallbackHandler : IDisposable
    {
        private readonly IotHubDeviceClient _deviceClient;
        private readonly string _testDeviceId;

        private readonly SemaphoreSlim _methodCallbackSemaphore = new(0, 1);
        private ExceptionDispatchInfo _methodExceptionDispatch;
        private DirectMethodServiceRequest _expectedDirectMethodRequest;

        private readonly SemaphoreSlim _twinCallbackSemaphore = new(0, 1);
        private ExceptionDispatchInfo _twinExceptionDispatch;
        private Tuple<string, object> _expectedTwinPatchKeyValuePair;

        private readonly SemaphoreSlim _receivedMessageCallbackSemaphore = new(0, 1);
        private ExceptionDispatchInfo _receiveMessageExceptionDispatch;
        private OutgoingMessage _expectedOutgoingMessage;

        internal TestDeviceCallbackHandler(IotHubDeviceClient deviceClient, string deviceId)
        {
            _testDeviceId = deviceId;

            if (deviceClient == null || deviceClient.ConnectionStatusInfo.Status != ConnectionStatus.Connected)
            {
                throw new InvalidOperationException($"The {nameof(IotHubDeviceClient)} for device Id {_testDeviceId}" +
                    $" needs to be already initialized and opened before initializing {nameof(TestDeviceCallbackHandler)}.");
            }
            _deviceClient = deviceClient;
        }

        internal DirectMethodServiceRequest ExpectedDirectMethodRequest
        {
            get => Volatile.Read(ref _expectedDirectMethodRequest);
            set => Volatile.Write(ref _expectedDirectMethodRequest, value);
        }

        internal Tuple<string, object> ExpectedTwinPatchKeyValuePair
        {
            get => Volatile.Read(ref _expectedTwinPatchKeyValuePair);
            set => Volatile.Write(ref _expectedTwinPatchKeyValuePair, value);
        }

        internal OutgoingMessage ExpectedOutgoingMessage
        {
            get => Volatile.Read(ref _expectedOutgoingMessage);
            set => Volatile.Write(ref _expectedOutgoingMessage, value);
        }

        public void Dispose()
        {
            _methodCallbackSemaphore?.Dispose();
            _twinCallbackSemaphore?.Dispose();
            _receivedMessageCallbackSemaphore?.Dispose();
        }

        // Set a direct method callback that expects a request with payload of type T.
        internal async Task SetDeviceReceiveMethodAndRespondAsync<T>(object deviceResponsePayload, CancellationToken ct = default)
        {
            await _deviceClient.SetDirectMethodCallbackAsync(
                (request) =>
                {
                    VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethodAndRespondAsync)}: DeviceClient {_testDeviceId} callback method: {request.MethodName} with timeout {request.ResponseTimeout}.");
                    try
                    {
                        request.MethodName.Should().Be(ExpectedDirectMethodRequest.MethodName, "The expected method name should match what was sent from service");

                        var expectedRequestPayload = (T)ExpectedDirectMethodRequest.Payload;
                        request.TryGetPayload(out T actualRequestPayload).Should().BeTrue();
                        actualRequestPayload.Should().BeEquivalentTo(expectedRequestPayload, "The expected method data should match what was sent from service");

                        var response = new DirectMethodResponse(200)
                        {
                            Payload = deviceResponsePayload,
                        };
                        return Task.FromResult(response);
                    }
                    catch (Exception ex)
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethodAndRespondAsync)}: Error during DeviceClient direct method callback {request.MethodName}: {ex}.");
                        _methodExceptionDispatch = ExceptionDispatchInfo.Capture(ex);

                        var response = new DirectMethodResponse(500);
                        return Task.FromResult(response);
                    }
                    finally
                    {
                        // Always notify that we got the callback.
                        _methodCallbackSemaphore.Release();
                    }
                },
                ct).ConfigureAwait(false);
        }

        internal async Task WaitForMethodCallbackAsync(CancellationToken ct)
        {
            await _methodCallbackSemaphore.WaitAsync(ct).ConfigureAwait(false);
            _methodExceptionDispatch?.Throw();
        }

        // Set a twin patch callback that expects a patch of type T.
        internal async Task SetTwinPropertyUpdateCallbackHandlerAndProcessAsync<T>(CancellationToken ct = default)
        {
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(
                (patch) =>
                {
                    VerboseTestLogger.WriteLine($"{nameof(SetTwinPropertyUpdateCallbackHandlerAndProcessAsync)}: DeviceClient {_testDeviceId} callback twin: DesiredProperty: {patch}");

                    try
                    {
                        string expectedPropertyName = ExpectedTwinPatchKeyValuePair.Item1;
                        var expectedTwinPropertyValue = (T)ExpectedTwinPatchKeyValuePair.Item2;
                        bool containsProperty = patch.TryGetValue(expectedPropertyName, out T actualPropertyValue);
                        containsProperty.Should().BeTrue($"Expecting property update patch received for {_testDeviceId} for {expectedPropertyName} to be {expectedTwinPropertyValue} but was: {patch.GetSerializedString()}");

                        // We don't support nested deserialization yet, so we'll need to serialize the response and compare them.
                        JsonConvert.SerializeObject(expectedTwinPropertyValue).Should().Be(JsonConvert.SerializeObject(actualPropertyValue), "The property value should match what was set by service");
                    }
                    catch (Exception ex)
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethodAndRespondAsync)}: Error during DeviceClient desired property callback for patch {patch}: {ex}.");
                        _twinExceptionDispatch = ExceptionDispatchInfo.Capture(ex);
                    }
                    finally
                    {
                        // Always notify that we got the callback.
                        _twinCallbackSemaphore.Release();
                    }

                    return Task.FromResult(true);
                },
                ct).ConfigureAwait(false);
        }

        internal async Task WaitForTwinCallbackAsync(CancellationToken ct)
        {
            await _twinCallbackSemaphore.WaitAsync(ct).ConfigureAwait(false);
            _twinExceptionDispatch?.Throw();
        }

        internal async Task SetIncomingMessageCallbackHandlerAndCompleteMessageAsync<T>(CancellationToken ct = default)
        {
            string receivedMessageDestination = $"/devices/{_testDeviceId}/messages/deviceBound";

            await _deviceClient.SetIncomingMessageCallbackAsync((IncomingMessage receivedMessage) =>
            {
                VerboseTestLogger.WriteLine($"{nameof(SetIncomingMessageCallbackHandlerAndCompleteMessageAsync)}: DeviceClient {_testDeviceId} received message with Id: {receivedMessage.MessageId}.");

                try
                {
                    receivedMessage.MessageId.Should().Be(ExpectedOutgoingMessage.MessageId, "Received message Id should match what was sent by service");
                    receivedMessage.UserId.Should().Be(ExpectedOutgoingMessage.UserId, "Received user Id should match what was sent by service");
                    receivedMessage.To.Should().Be(receivedMessageDestination, "Received message destination is not what was sent by service");

                    receivedMessage.TryGetPayload(out T actualPayload).Should().BeTrue();
                    ExpectedOutgoingMessage.Payload.Should().BeOfType<T>();
                    var expectedPayload = (T)ExpectedOutgoingMessage.Payload;
                    actualPayload.Should().Be(expectedPayload);

                    receivedMessage.Properties.Count.Should().Be(ExpectedOutgoingMessage.Properties.Count, $"The count of received properties did not match for device {_testDeviceId}");
                    KeyValuePair<string, string> expectedMessageProperties = ExpectedOutgoingMessage.Properties.Single();
                    KeyValuePair<string, string> receivedMessageProperties = receivedMessage.Properties.Single();
                    receivedMessageProperties.Key.Should().Be(expectedMessageProperties.Key, $"The key \"property1\" did not match for device {_testDeviceId}");
                    receivedMessageProperties.Value.Should().Be(expectedMessageProperties.Value, $"The value of \"property1\" did not match for device {_testDeviceId}");

                    VerboseTestLogger.WriteLine($"{nameof(SetIncomingMessageCallbackHandlerAndCompleteMessageAsync)}: DeviceClient completed message with Id: {receivedMessage.MessageId}.");
                    return Task.FromResult(MessageAcknowledgement.Complete);
                }
                catch (Exception ex)
                {
                    VerboseTestLogger.WriteLine($"{nameof(SetIncomingMessageCallbackHandlerAndCompleteMessageAsync)}: Error during DeviceClient receive message callback: {ex}.");
                    _receiveMessageExceptionDispatch = ExceptionDispatchInfo.Capture(ex);

                    return Task.FromResult(MessageAcknowledgement.Abandon);
                }
                finally
                {
                    // Always notify that we got the callback.
                    _receivedMessageCallbackSemaphore.Release();
                }
            },
            ct).ConfigureAwait(false);
        }

        internal async Task UnsetMessageReceiveCallbackHandlerAsync()
        {
            await _deviceClient.OpenAsync().ConfigureAwait(false);
            await _deviceClient.SetIncomingMessageCallbackAsync(null).ConfigureAwait(false);
        }

        internal async Task WaitForIncomingMessageCallbackAsync(CancellationToken ct)
        {
            await _receivedMessageCallbackSemaphore.WaitAsync(ct).ConfigureAwait(false);
            _receiveMessageExceptionDispatch?.Throw();
        }
    }
}
