﻿// Copyright (c) Microsoft. All rights reserved.
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

        private readonly SemaphoreSlim _twinCallbackSemaphore = new(0, 1);
        private ExceptionDispatchInfo _twinExceptionDispatch;

        private readonly SemaphoreSlim _receivedMessageCallbackSemaphore = new(0, 1);
        private ExceptionDispatchInfo _receiveMessageExceptionDispatch;
        private OutgoingMessage _expectedMessageSentByService;

        internal TestDeviceCallbackHandler(TestDevice testDevice)
        {
            _testDeviceId = testDevice.Id;

            if (testDevice.DeviceClient == null)
            {
                throw new InvalidOperationException($"The {nameof(IotHubDeviceClient)} for device Id {_testDeviceId} hasn't been initialized yet." +
                    $" Initialize the {nameof(IotHubDeviceClient)} before creating a {nameof(TestDeviceCallbackHandler)}.");
            }
            _deviceClient = testDevice.DeviceClient;
        }

        internal OutgoingMessage ExpectedMessageSentByService
        {
            get => Volatile.Read(ref _expectedMessageSentByService);
            set => Volatile.Write(ref _expectedMessageSentByService, value);
        }

        public void Dispose()
        {
            _methodCallbackSemaphore?.Dispose();
            _twinCallbackSemaphore?.Dispose();
            _receivedMessageCallbackSemaphore?.Dispose();
        }

        internal async Task SetDeviceReceiveMethodAsync<T>(string methodName, object deviceResponse, T expectedServiceRequestJson)
        {
            await _deviceClient.OpenAsync().ConfigureAwait(false);
            await _deviceClient.SetDirectMethodCallbackAsync(
                (request) =>
                {
                    try
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethodAsync)}: DeviceClient {_testDeviceId} callback method: {request.MethodName} with timeout {request.ResponseTimeout}.");
                        request.MethodName.Should().Be(methodName, "The expected method name should match what was sent from service");
                        request.TryGetPayload(out T actualRequestPayload).Should().BeTrue();
                        actualRequestPayload.Should().BeEquivalentTo(expectedServiceRequestJson, "The expected method data should match what was sent from service");

                        var response = new DirectMethodResponse(200)
                        {
                            Payload = deviceResponse,
                        };
                        return Task.FromResult(response);
                    }
                    catch (Exception ex)
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethodAsync)}: Error during DeviceClient direct method callback {request.MethodName}: {ex}.");
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

        internal async Task SetTwinPropertyUpdateCallbackHandlerAsync<T>(string expectedPropName, T expectedPropValue)
        {
            await _deviceClient.OpenAsync().ConfigureAwait(false);
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(
                (patch) =>
                {
                    VerboseTestLogger.WriteLine($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DeviceClient {_testDeviceId} callback twin: DesiredProperty: {patch}");

                    try
                    {
                        bool containsProperty = patch.TryGetValue(expectedPropName, out T actualPropertyValue);
                        containsProperty.Should().BeTrue($"Expecting property update patch received for {_testDeviceId} for {expectedPropName} to be {expectedPropValue} but was: {patch.GetSerializedString()}");
                        actualPropertyValue.Should().Be(expectedPropValue, "The property value should match what was set by service");
                    }
                    catch (Exception ex)
                    {
     
                        VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethodAsync)}: Error during DeviceClient desired property callback for patch {patch}: {ex}.");
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

        internal async Task SetMessageReceiveCallbackHandlerAsync<T>()
        {
            await _deviceClient.OpenAsync().ConfigureAwait(false);
            await _deviceClient.SetIncomingMessageCallbackAsync((IncomingMessage message) =>
            {
                VerboseTestLogger.WriteLine($"{nameof(SetMessageReceiveCallbackHandlerAsync)}: DeviceClient {_testDeviceId} received message with Id: {message.MessageId}.");

                try
                {
                    if (ExpectedMessageSentByService != null)
                    {
                        message.MessageId.Should().Be(ExpectedMessageSentByService.MessageId, "Received message Id should match what was sent by service");
                        message.UserId.Should().Be(ExpectedMessageSentByService.UserId, "Received user Id should match what was sent by service");
                        message.TryGetPayload(out T payload).Should().BeTrue();

                        ExpectedMessageSentByService.Payload.Should().BeOfType<T>();
                        var expectedPayload = (T)ExpectedMessageSentByService.Payload;
                        payload.Should().Be(expectedPayload);
                    }

                    VerboseTestLogger.WriteLine($"{nameof(SetMessageReceiveCallbackHandlerAsync)}: DeviceClient completed message with Id: {message.MessageId}.");
                    return Task.FromResult(MessageAcknowledgement.Complete);
                }
                catch (Exception ex)
                {
                    VerboseTestLogger.WriteLine($"{nameof(SetMessageReceiveCallbackHandlerAsync)}: Error during DeviceClient receive message callback: {ex}.");
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
