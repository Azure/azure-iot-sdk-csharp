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
    internal sealed class TestModuleCallbackHandler : IDisposable
    {
        private readonly IotHubModuleClient _moduleClient;
        private readonly string _testDeviceId;
        private readonly string _testModuleId;

        private readonly SemaphoreSlim _methodCallbackSemaphore = new(0, 1);
        private ExceptionDispatchInfo _methodExceptionDispatch;
        private DirectMethodServiceRequest _expectedDirectMethodRequest;

        internal TestModuleCallbackHandler(IotHubModuleClient moduleClient, string deviceId, string moduleId)
        {
            _testDeviceId = deviceId;
            _testModuleId = moduleId;

            if (moduleClient == null || moduleClient.ConnectionStatusInfo.Status != ConnectionStatus.Connected)
            {
                throw new InvalidOperationException($"The {nameof(IotHubDeviceClient)} for device Id {_testDeviceId} and module id {_testModuleId}" +
                    $" needs to be already initialized and opened before initializing {nameof(TestModuleCallbackHandler)}.");
            }
            _moduleClient = moduleClient;
        }

        internal DirectMethodServiceRequest ExpectedDirectMethodRequest
        {
            get => Volatile.Read(ref _expectedDirectMethodRequest);
            set => Volatile.Write(ref _expectedDirectMethodRequest, value);
        }

        public void Dispose()
        {
            _methodCallbackSemaphore?.Dispose();
        }

        internal async Task SetModuleReceiveMethodAndRespondAsync<T,H>(H moduleResponsePayload)
        {
            await _moduleClient.SetDirectMethodCallbackAsync(
                (request) =>
                {
                    VerboseTestLogger.WriteLine($"{nameof(SetModuleReceiveMethodAndRespondAsync)}: ModuleClient {_testDeviceId}:{_testModuleId} callback method: {request.MethodName} with timeout {request.ResponseTimeout}.");
                    try
                    {
                        request.MethodName.Should().Be(ExpectedDirectMethodRequest.MethodName, "The expected method name should match what was sent from service");

                        var expectedRequestPayload = (T)ExpectedDirectMethodRequest.Payload;
                        request.TryGetPayload(out T actualRequestPayload).Should().BeTrue();
                        actualRequestPayload.Should().BeEquivalentTo(expectedRequestPayload, "The expected method data should match what was sent from service");

                        var response = new DirectMethodResponse(200)
                        {
                            Payload = moduleResponsePayload,
                        };
                        return Task.FromResult(response);
                    }
                    catch (Exception ex)
                    {
                        VerboseTestLogger.WriteLine($"{nameof(SetModuleReceiveMethodAndRespondAsync)}: Error during DeviceClient direct method callback {request.MethodName}: {ex}.");
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
    }
}
