// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;

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

        private readonly SemaphoreSlim _receivedMessageCallbackSemaphore = new(0, 1);
        private ExceptionDispatchInfo _receiveMessageExceptionDispatch;
        private OutgoingMessage _expectedOutgoingMessage
            ;

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

        internal async Task SetDeviceReceiveMethodAndRespondAsync<T,H>(H deviceResponsePayload)
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
                }).ConfigureAwait(false);
        }

        internal async Task WaitForMethodCallbackAsync(CancellationToken ct)
        {
            await _methodCallbackSemaphore.WaitAsync(ct).ConfigureAwait(false);
            _methodExceptionDispatch?.Throw();
        }

        internal async Task SetTwinPropertyUpdateCallbackHandlerAndProcessAsync<T>(string expectedPropName, T expectedPropValue)
        {
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(
                (patch) =>
                {
                    VerboseTestLogger.WriteLine($"{nameof(SetTwinPropertyUpdateCallbackHandlerAndProcessAsync)}: DeviceClient {_testDeviceId} callback twin: DesiredProperty: {patch}");

                    try
                    {
                        bool containsProperty = patch.TryGetValue(expectedPropName, out T actualPropertyValue);
                        containsProperty.Should().BeTrue($"Expecting property update patch received for {_testDeviceId} for {expectedPropName} to be {expectedPropValue} but was: {patch.GetSerializedString()}");
                        actualPropertyValue.Should().Be(expectedPropValue, "The property value should match what was set by service");
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

                    return Task.FromResult<bool>(true);
                }).ConfigureAwait(false);
        }

        internal async Task WaitForTwinCallbackAsync(CancellationToken ct)
        {
            await _twinCallbackSemaphore.WaitAsync(ct).ConfigureAwait(false);
            _twinExceptionDispatch?.Throw();
        }

        internal async Task SetMessageReceiveCallbackHandlerAndCompleteMessageAsync<T>()
        {
            await _deviceClient.SetIncomingMessageCallbackAsync((IncomingMessage message) =>
            {
                VerboseTestLogger.WriteLine($"{nameof(SetMessageReceiveCallbackHandlerAndCompleteMessageAsync)}: DeviceClient {_testDeviceId} received message with Id: {message.MessageId}.");

                try
                {
                    message.MessageId.Should().Be(ExpectedOutgoingMessage.MessageId, "Received message Id should match what was sent by service");
                    message.UserId.Should().Be(ExpectedOutgoingMessage.UserId, "Received user Id should match what was sent by service");
                    message.TryGetPayload(out T payload).Should().BeTrue();

                    ExpectedOutgoingMessage.Payload.Should().BeOfType<T>();
                    var expectedPayload = (T)ExpectedOutgoingMessage.Payload;
                    payload.Should().Be(expectedPayload);

                    VerboseTestLogger.WriteLine($"{nameof(SetMessageReceiveCallbackHandlerAndCompleteMessageAsync)}: DeviceClient completed message with Id: {message.MessageId}.");
                    return Task.FromResult(MessageAcknowledgement.Complete);
                }
                catch (Exception ex)
                {
                    VerboseTestLogger.WriteLine($"{nameof(SetMessageReceiveCallbackHandlerAndCompleteMessageAsync)}: Error during DeviceClient receive message callback: {ex}.");
                    _receiveMessageExceptionDispatch = ExceptionDispatchInfo.Capture(ex);

                    return Task.FromResult(MessageAcknowledgement.Abandon);
                }
                finally
                {
                    // Always notify that we got the callback.
                    _receivedMessageCallbackSemaphore.Release();
                }
            }).ConfigureAwait(false);
        }

        internal async Task UnsetMessageReceiveCallbackHandlerAsync()
        {
            await _deviceClient.OpenAsync().ConfigureAwait(false);
            await _deviceClient.SetIncomingMessageCallbackAsync(null).ConfigureAwait(false);
        }

        internal async Task WaitForReceiveMessageCallbackAsync(CancellationToken ct)
        {
            await _receivedMessageCallbackSemaphore.WaitAsync(ct).ConfigureAwait(false);
            _receiveMessageExceptionDispatch?.Throw();
        }
    }
}
