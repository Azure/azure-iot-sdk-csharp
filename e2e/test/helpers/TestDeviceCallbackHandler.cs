// Copyright (c) Microsoft. All rights reserved.
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
        private readonly DeviceClient _deviceClient;
        private readonly TestDevice _testDevice;
        private readonly MsTestLogger _logger;

        private readonly SemaphoreSlim _methodCallbackSemaphore = new SemaphoreSlim(0, 1);
        private ExceptionDispatchInfo _methodExceptionDispatch;

        private readonly SemaphoreSlim _twinCallbackSemaphore = new SemaphoreSlim(0, 1);
        private ExceptionDispatchInfo _twinExceptionDispatch;
        private string _expectedTwinPropertyValue;

        private readonly SemaphoreSlim _receivedMessageCallbackSemaphore = new SemaphoreSlim(0, 1);
        private ExceptionDispatchInfo _receiveMessageExceptionDispatch;
        private Message _expectedMessageSentByService;

        private readonly SemaphoreSlim _clientPropertyCallbackSemaphore = new SemaphoreSlim(0, 1);
        private ExceptionDispatchInfo _clientPropertyExceptionDispatch;
        private object _expectedClientPropertyValue;

        public TestDeviceCallbackHandler(DeviceClient deviceClient, TestDevice testDevice, MsTestLogger logger)
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

        public object ExpectedClientPropertyValue
        {
            get => Volatile.Read(ref _expectedClientPropertyValue);
            set => Volatile.Write(ref _expectedClientPropertyValue, value);
        }

        public async Task SetDeviceReceiveMethodAsync(string methodName, string deviceResponseJson, string expectedServiceRequestJson)
        {
            await _deviceClient
                .SetMethodHandlerAsync(
                    methodName,
                    (request, context) =>
                    {
                        try
                        {
                            _logger.Trace($"{nameof(SetDeviceReceiveMethodAsync)}: DeviceClient {_testDevice.Id} callback method: {request.Name} {request.ResponseTimeout}.");
                            request.Name.Should().Be(methodName, "The expected method name should match what was sent from service");
                            request.DataAsJson.Should().Be(expectedServiceRequestJson, "The expected method data should match what was sent from service");

                            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(deviceResponseJson), 200));
                        }
                        catch (Exception ex)
                        {
                            _logger.Trace($"{nameof(SetDeviceReceiveMethodAsync)}: Error during DeviceClient callback method: {ex}.");

                            _methodExceptionDispatch = ExceptionDispatchInfo.Capture(ex);
                            return Task.FromResult(new MethodResponse(500));
                        }
                        finally
                        {
                            // Always notify that we got the callback.
                            _methodCallbackSemaphore.Release();
                        }
                    },
                    null)
                .ConfigureAwait(false);
        }

        public async Task WaitForMethodCallbackAsync(CancellationToken ct)
        {
            await _methodCallbackSemaphore.WaitAsync(ct).ConfigureAwait(false);
            _methodExceptionDispatch?.Throw();
        }

        public async Task SetTwinPropertyUpdateCallbackHandlerAsync(string expectedPropName)
        {
            string userContext = "myContext";

            await _deviceClient
                .SetDesiredPropertyUpdateCallbackAsync(
                    (patch, context) =>
                    {
                        _logger.Trace($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DeviceClient {_testDevice.Id} callback twin: DesiredProperty: {patch}, {context}");

                        try
                        {
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
                    },
                    userContext)
                .ConfigureAwait(false);
        }

        public async Task WaitForTwinCallbackAsync(CancellationToken ct)
        {
            await _twinCallbackSemaphore.WaitAsync(ct).ConfigureAwait(false);
            _twinExceptionDispatch?.Throw();
        }

        public async Task SetMessageReceiveCallbackHandlerAsync()
        {
            await _deviceClient
                .SetReceiveMessageHandlerAsync(
                    async (receivedMessage, context) =>
                    {
                        _logger.Trace($"{nameof(SetMessageReceiveCallbackHandlerAsync)}: DeviceClient {_testDevice.Id} received message with Id: {receivedMessage.MessageId}.");

                        try
                        {
                            receivedMessage.MessageId.Should().Be(ExpectedMessageSentByService.MessageId, "Received message Id should match what was sent by service");
                            receivedMessage.UserId.Should().Be(ExpectedMessageSentByService.UserId, "Received user Id should match what was sent by service");

                            await CompleteMessageAsync(receivedMessage).ConfigureAwait(false);
                            _logger.Trace($"{nameof(SetMessageReceiveCallbackHandlerAsync)}: DeviceClient completed message with Id: {receivedMessage.MessageId}.");
                        }
                        catch (Exception ex)
                        {
                            _logger.Trace($"{nameof(SetMessageReceiveCallbackHandlerAsync)}: Error during DeviceClient receive message callback: {ex}.");
                            _receiveMessageExceptionDispatch = ExceptionDispatchInfo.Capture(ex);
                        }
                        finally
                        {
                            // Always notify that we got the callback.
                            _receivedMessageCallbackSemaphore.Release();
                        }
                    },
                    null)
                .ConfigureAwait(false);
        }

        private async Task CompleteMessageAsync(Client.Message message)
        {
            await _deviceClient.CompleteAsync(message).ConfigureAwait(false);
        }

        public async Task WaitForReceiveMessageCallbackAsync(CancellationToken ct)
        {
            await _receivedMessageCallbackSemaphore.WaitAsync(ct).ConfigureAwait(false);
            _receiveMessageExceptionDispatch?.Throw();
        }

        public async Task SetClientPropertyUpdateCallbackHandlerAsync<T>(string expectedPropName, string componentName = default)
        {
            await _deviceClient
                .SubscribeToWritablePropertyUpdateRequestsAsync(
                    patch =>
                    {
                        _logger.Trace($"{nameof(SetClientPropertyUpdateCallbackHandlerAsync)}: DeviceClient {_testDevice.Id} callback property: WritableProperty: {patch}.");

                        try
                        {
                            bool isPropertyPresent = componentName == null
                                ? patch.TryGetValue(expectedPropName, out T propertyFromCollection)
                                : patch.TryGetValue(componentName, expectedPropName, out propertyFromCollection);

                            isPropertyPresent.Should().BeTrue();
                            propertyFromCollection.Should().BeEquivalentTo((T)ExpectedClientPropertyValue);
                        }
                        catch (Exception ex)
                        {
                            _clientPropertyExceptionDispatch = ExceptionDispatchInfo.Capture(ex);
                        }
                        finally
                        {
                            // Always notify that we got the callback.
                            _clientPropertyCallbackSemaphore.Release();
                        }

                        return Task.FromResult(true);
                    })
                .ConfigureAwait(false);
        }

        public async Task WaitForClientPropertyUpdateCallbcakAsync(CancellationToken ct)
        {
            await _clientPropertyCallbackSemaphore.WaitAsync(ct).ConfigureAwait(false);
            _clientPropertyExceptionDispatch?.Throw();
        }

        public void Dispose()
        {
            _methodCallbackSemaphore?.Dispose();
            _twinCallbackSemaphore?.Dispose();
            _receivedMessageCallbackSemaphore?.Dispose();
        }
    }
}
