// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class TestDeviceCallbackHandler
    {
        private DeviceClient _deviceClient;

        private ExceptionDispatchInfo _methodExceptionDispatch;
        private SemaphoreSlim _methodCallbackSemaphore = new SemaphoreSlim(1, 1);

        private ExceptionDispatchInfo _twinExceptionDispatch;
        private SemaphoreSlim _twinCallbackSemaphore = new SemaphoreSlim(1, 1);

        private static TestLogging s_log = TestLogging.GetInstance();

        public TestDeviceCallbackHandler(DeviceClient deviceClient)
        {
            _deviceClient = deviceClient;
        }

        public async Task SetDeviceReceiveMethodAsync(string methodName, string deviceResponseJson, string expectedServiceRequestJson)
        {
            await _deviceClient.SetMethodHandlerAsync(methodName,
                (request, context) =>
                {
                    try
                    {
                        s_log.WriteLine($"{nameof(SetDeviceReceiveMethodAsync)}: DeviceClient callback method: {request.Name} {request.ResponseTimeout}.");
                        Assert.AreEqual(methodName, request.Name);
                        Assert.AreEqual(expectedServiceRequestJson, request.DataAsJson);

                        return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(deviceResponseJson), 200));
                    }
                    catch (Exception ex)
                    {
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
        
        public async Task SetTwinPropertyUpdateCallbackHandlerAsync(string expectedPropName, string expectedPropValue)
        {
            string userContext = "myContext";

            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(
                (patch, context) =>
                {
                    s_log.WriteLine($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DeviceClient callback twin: DesiredProperty: {patch}, {context}");

                    try
                    {
                        Assert.AreEqual(expectedPropValue, patch[expectedPropName].ToString());
                        Assert.AreEqual(userContext, context, "Context");
                    }
                    catch (Exception ex)
                    {
                        _twinExceptionDispatch = ExceptionDispatchInfo.Capture(ex);
                    }

                    return Task.FromResult<bool>(true);

                }, userContext).ConfigureAwait(false);
        }

        public async Task WaitForTwinCallbackAsync(CancellationToken ct)
        {
            await _twinCallbackSemaphore.WaitAsync(ct).ConfigureAwait(false);
        }
    }
}
