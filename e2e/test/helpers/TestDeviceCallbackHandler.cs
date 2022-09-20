﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    public class TestDeviceCallbackHandler : IDisposable
    {
        private readonly IotHubDeviceClient _deviceClient;
        private readonly TestDevice _testDevice;
        private readonly MsTestLogger _logger;

        private readonly SemaphoreSlim _methodCallbackSemaphore = new(0, 1);
        private ExceptionDispatchInfo _methodExceptionDispatch;

        private readonly SemaphoreSlim _twinCallbackSemaphore = new(0, 1);
        private ExceptionDispatchInfo _twinExceptionDispatch;
        private string _expectedTwinPropertyValue;

        private readonly SemaphoreSlim _receivedMessageCallbackSemaphore = new(0, 1);
        private ExceptionDispatchInfo _receiveMessageExceptionDispatch;
        private Message _expectedMessageSentByService;

        public TestDeviceCallbackHandler(IotHubDeviceClient deviceClient, TestDevice testDevice, MsTestLogger logger)
        {
            _deviceClient = deviceClient;
            _testDevice = testDevice;
            _logger = logger;
        }

        public string ExpectedTwinPropertyValue
        {
            get => Volatile.Read(ref _expectedTwinPropertyValue);
            set => Volatile.Write(ref _expectedTwinPropertyValue, value);
        }

        public Message ExpectedMessageSentByService
        {
            get => Volatile.Read(ref _expectedMessageSentByService);
            set => Volatile.Write(ref _expectedMessageSentByService, value);
        }

        public async Task SetDeviceReceiveMethodAsync(string methodName, string deviceResponseJson, string expectedServiceRequestJson)
        {
            await _deviceClient.OpenAsync().ConfigureAwait(false);
            await _deviceClient.SetMethodHandlerAsync(
                (request, context) =>
                {
                    try
                    {
                        _logger.Trace($"{nameof(SetDeviceReceiveMethodAsync)}: DeviceClient {_testDevice.Id} callback method: {request.MethodName} with timeout {request.ResponseTimeout}.");
                        request.MethodName.Should().Be(methodName, "The expected method name should match what was sent from service");
                        request.Payload.Should().Be(expectedServiceRequestJson, "The expected method data should match what was sent from service");

                        var response = new Client.DirectMethodResponse()
                        {
                            Status = 200,
                            Payload = Encoding.UTF8.GetBytes(deviceResponseJson)
                        };
                        return Task.FromResult(response);
                    }
                    catch (Exception ex)
                    {
                        _logger.Trace($"{nameof(SetDeviceReceiveMethodAsync)}: Error during DeviceClient callback method: {ex}.");

                        _methodExceptionDispatch = ExceptionDispatchInfo.Capture(ex);

                        var response = new Client.DirectMethodResponse()
                        {
                            Status = 500,
                        };

                        return Task.FromResult(response);
                    }
                    finally
                    {
                        // Always notify that we got the callback.
                        _methodCallbackSemaphore.Release();
                    }
                },
                null).ConfigureAwait(false);
        }

        public async Task WaitForMethodCallbackAsync(CancellationToken ct)
        {
            await _methodCallbackSemaphore.WaitAsync(ct).ConfigureAwait(false);
            _methodExceptionDispatch?.Throw();
        }

        public async Task SetTwinPropertyUpdateCallbackHandlerAsync(string expectedPropName)
        {
            string userContext = "myContext";

            await _deviceClient.OpenAsync().ConfigureAwait(false);
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(
                (patch, context) =>
                {
                    _logger.Trace($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DeviceClient {_testDevice.Id} callback twin: DesiredProperty: {patch}, {context}");

                    try
                    {
                        bool containsProperty = patch.Contains(expectedPropName);
                        containsProperty.Should().BeTrue($"Expecting property update patch received for {_testDevice.Id} to be {expectedPropName} but was: {patch.ToJson()}");

                        string propertyValue = patch[expectedPropName];
                        propertyValue.Should().Be(ExpectedTwinPropertyValue, "The property value should match what was set by service");
                        context.Should().Be(userContext, "The context should match what was set by service");
                    }
                    catch (Exception ex)
                    {
                        _twinExceptionDispatch = ExceptionDispatchInfo.Capture(ex);
                    }
                    finally
                    {
                        // Always notify that we got the callback.
                        _twinCallbackSemaphore.Release();
                    }

                    return Task.FromResult<bool>(true);
                }, userContext).ConfigureAwait(false);
        }

        public async Task WaitForTwinCallbackAsync(CancellationToken ct)
        {
            await _twinCallbackSemaphore.WaitAsync(ct).ConfigureAwait(false);
            _twinExceptionDispatch?.Throw();
        }

        public async Task SetMessageReceiveCallbackHandlerAsync()
        {
            await _deviceClient.OpenAsync().ConfigureAwait(false);
            await _deviceClient.SetReceiveMessageHandlerAsync(OnC2dMessageReceivedAsync, null).ConfigureAwait(false);
        }

        public async Task UnSetMessageReceiveCallbackHandlerAsync()
        {
            await _deviceClient.OpenAsync().ConfigureAwait(false);
            await _deviceClient.SetReceiveMessageHandlerAsync(null, null).ConfigureAwait(false);
        }

        private Task<MessageResponse> OnC2dMessageReceivedAsync(Client.Message message, object context)
        {
            _logger.Trace($"{nameof(SetMessageReceiveCallbackHandlerAsync)}: DeviceClient {_testDevice.Id} received message with Id: {message.MessageId}.");

            try
            {
                if (ExpectedMessageSentByService != null)
                {
                    message.MessageId.Should().Be(ExpectedMessageSentByService.MessageId, "Received message Id should match what was sent by service");
                    message.UserId.Should().Be(ExpectedMessageSentByService.UserId, "Received user Id should match what was sent by service");
                }

                _logger.Trace($"{nameof(SetMessageReceiveCallbackHandlerAsync)}: DeviceClient completed message with Id: {message.MessageId}.");
                return Task.FromResult(MessageResponse.Completed);
            }
            catch (Exception ex)
            {
                _logger.Trace($"{nameof(SetMessageReceiveCallbackHandlerAsync)}: Error during DeviceClient receive message callback: {ex}.");
                _receiveMessageExceptionDispatch = ExceptionDispatchInfo.Capture(ex);
                return Task.FromResult(MessageResponse.None);
            }
            finally
            {
                // Always notify that we got the callback.
                _receivedMessageCallbackSemaphore.Release();
            }
        }

        public async Task WaitForReceiveMessageCallbackAsync(CancellationToken ct)
        {
            await _receivedMessageCallbackSemaphore.WaitAsync(ct).ConfigureAwait(false);
            _receiveMessageExceptionDispatch?.Throw();
        }

        public void Dispose()
        {
            _methodCallbackSemaphore?.Dispose();
            _twinCallbackSemaphore?.Dispose();
            _receivedMessageCallbackSemaphore?.Dispose();
        }
    }
}
