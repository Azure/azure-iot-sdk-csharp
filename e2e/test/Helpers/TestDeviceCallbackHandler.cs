// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    public class TestDeviceCallbackHandler
    {
        private DeviceClient _deviceClient;

        private ExceptionDispatchInfo _methodExceptionDispatch;
        private SemaphoreSlim _methodCallbackSemaphore = new SemaphoreSlim(1, 1);

        private ExceptionDispatchInfo _twinExceptionDispatch;
        private SemaphoreSlim _twinCallbackSemaphore = new SemaphoreSlim(1, 1);
        private string _expectedTwinPropertyValue = null;

        private readonly TestLogger _log;

        public TestDeviceCallbackHandler(DeviceClient deviceClient, TestLogger logger)
        {
            _deviceClient = deviceClient;
            _log = logger;
        }

        public string ExpectedTwinPropertyValue
        {
            get
            {
                return Volatile.Read(ref _expectedTwinPropertyValue);
            }

            set
            {
                Volatile.Write(ref _expectedTwinPropertyValue, value);
            }
        }

        public async Task SetDeviceReceiveMethodAsync(string methodName, string deviceResponseJson, string expectedServiceRequestJson)
        {
            await _deviceClient.SetMethodHandlerAsync(methodName,
                (request, context) =>
                {
                    try
                    {
                        _log.Trace($"{nameof(SetDeviceReceiveMethodAsync)}: DeviceClient callback method: {request.Name} {request.ResponseTimeout}.");
                        Assert.AreEqual(methodName, request.Name, $"The expected method name should be {methodName} but was {request.Name}");
                        Assert.AreEqual(expectedServiceRequestJson, request.DataAsJson, $"The expected method name should be {expectedServiceRequestJson} but was {request.DataAsJson}");

                        return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(deviceResponseJson), 200));
                    }
                    catch (Exception ex)
                    {
                        _log.Trace($"{nameof(SetDeviceReceiveMethodAsync)}: Error during DeviceClient callback method: {ex}.");

                        _methodExceptionDispatch = ExceptionDispatchInfo.Capture(ex);
                        return Task.FromResult(new MethodResponse(500));
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

            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(
                (patch, context) =>
                {
                    _log.Trace($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DeviceClient callback twin: DesiredProperty: {patch}, {context}");

                    try
                    {
                        Assert.AreEqual(ExpectedTwinPropertyValue, patch[expectedPropName].ToString());
                        Assert.AreEqual(userContext, context, "Context");
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
    }
}
