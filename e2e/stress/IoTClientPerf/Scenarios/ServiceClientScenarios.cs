// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices;
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public abstract class ServiceClientScenario : PerfScenario
    {
        private static ServiceClient s_sc;

        // Shared by Create, Open and Send
        private TelemetryMetrics _m = new TelemetryMetrics();
        private Stopwatch _sw = new Stopwatch();

        // Separate metrics and time calculation for operations that can be parallelized.
        private const string TestMethodName = "PerfTestMethod";
        private TelemetryMetrics _mMethod = new TelemetryMetrics();
        private Stopwatch _swMethod = new Stopwatch();

        private byte[] _messageBytes;

        public ServiceClientScenario(PerfScenarioConfig config) : base(config)
        {
            _m.Id = _id;
            _mMethod.Id = _id;

            _messageBytes = new byte[_sizeBytes];
            BitConverter.TryWriteBytes(_messageBytes, _id);
        }

        protected void CreateServiceClient()
        {
            if (_id == 0) s_sc = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
        }

        protected async Task OpenServiceClientAsync(CancellationToken ct)
        {
            ExceptionDispatchInfo exInfo = null;
            _m.OperationType = TelemetryMetrics.ServiceOperationOpen;
            _m.ScheduleTime = null;
            _sw.Restart();
            try
            {
                Task t = s_sc.OpenAsync();
                _m.ScheduleTime = _sw.ElapsedMilliseconds;

                _sw.Restart();
                await t.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _m.ErrorMessage = $"{ex.GetType().Name} - {ex.Message}";
                exInfo = ExceptionDispatchInfo.Capture(ex);
            }

            _m.ExecuteTime = _sw.ElapsedMilliseconds;
            await _writer.WriteAsync(_m).ConfigureAwait(false);

            exInfo?.Throw();
        }

        protected async Task SendMessageAsync(CancellationToken ct)
        {
            ExceptionDispatchInfo exInfo = null;
            _m.OperationType = TelemetryMetrics.ServiceOperationSend;
            _m.ScheduleTime = null;
            _sw.Restart();

            try
            {
                var message = new Message(_messageBytes);
                Task t = s_sc.SendAsync(Configuration.Stress.GetDeviceNameById(_id, _authType), message);
                _m.ScheduleTime = _sw.ElapsedMilliseconds;

                _sw.Restart();
                await t.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _m.ErrorMessage = $"{ex.GetType().Name} - {ex.Message}";
                exInfo = ExceptionDispatchInfo.Capture(ex);
            }

            _m.ExecuteTime = _sw.ElapsedMilliseconds;
            await _writer.WriteAsync(_m).ConfigureAwait(false);
            exInfo?.Throw();
        }

        protected async Task CallMethodAsync(CancellationToken ct)
        {
            ExceptionDispatchInfo exInfo = null;
            _mMethod.ScheduleTime = null;
            _mMethod.OperationType = TelemetryMetrics.ServiceOperationMethodCall;
            _swMethod.Restart();

            try
            {
                var methodCall = new CloudToDeviceMethod(TestMethodName);
                Task t = s_sc.InvokeDeviceMethodAsync(Configuration.Stress.GetDeviceNameById(_id, _authType), methodCall);
                _mMethod.ScheduleTime = _swMethod.ElapsedMilliseconds;

                _swMethod.Restart();
                await t.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _mMethod.ErrorMessage = $"{ex.GetType().Name} - {ex.Message}";
                exInfo = ExceptionDispatchInfo.Capture(ex);
            }

            _mMethod.ExecuteTime = _swMethod.ElapsedMilliseconds;
            await _writer.WriteAsync(_mMethod).ConfigureAwait(false);
            exInfo?.Throw();
        }

        protected Task CloseAsync(CancellationToken ct)
        {
            if (s_sc == null) return Task.CompletedTask;
            return s_sc.CloseAsync();
        }
    }
}
