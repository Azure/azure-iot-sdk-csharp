// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class DeviceOneD2CTest : PerfScenario
    {
        private static DeviceClient s_dc = null;
        private static SemaphoreSlim s_semaphore = new SemaphoreSlim(1);
        private TelemetryMetrics _m = new TelemetryMetrics();
        private Stopwatch _sw = new Stopwatch();

        private byte[] _messageBytes;

        public DeviceOneD2CTest(PerfScenarioConfig config) : base(config)
        {
            _m.Id = _id;
            _messageBytes = new byte[_sizeBytes];

            BitConverter.TryWriteBytes(_messageBytes, _id);
        }

        public override async Task SetupAsync(CancellationToken ct)
        {
            if (s_dc != null) return;
            await s_semaphore.WaitAsync(ct).ConfigureAwait(false);
            if (s_dc != null) return;

            await CreateDeviceAsync().ConfigureAwait(false);
            await OpenDeviceAsync(ct).ConfigureAwait(false);

            s_semaphore.Release();
        }

        private async Task CreateDeviceAsync()
        {
            _sw.Restart();
            _m.OperationType = TelemetryMetrics.DeviceOperationCreate;

            ITransportSettings transportSettings = null;

            if (_authType == "sas")
            {
                if (transportSettings == null)
                {
                    s_dc = DeviceClient.CreateFromConnectionString(Configuration.Stress.GetConnectionStringById(_id, _authType), _transport);
                }
                else
                {
                    s_dc = DeviceClient.CreateFromConnectionString(Configuration.Stress.GetConnectionStringById(_id, _authType), new ITransportSettings[] { transportSettings });
                }
            }
            else if (_authType == "x509")
            {
                s_dc = DeviceClient.Create(
                    Configuration.Stress.Endpoint,
                    new DeviceAuthenticationWithX509Certificate(
                        Configuration.Stress.GetDeviceNameById(_id, _authType),
                        Configuration.Stress.Certificate),
                    _transport);
            }
            else
            {
                throw new NotImplementedException($"Not implemented for authType {_authType}");
            }

            _m.ExecuteTime = _sw.ElapsedMilliseconds;
            _m.ScheduleTime = null; // sync operation
            await _writer.WriteAsync(_m).ConfigureAwait(false);
        }

        protected async Task OpenDeviceAsync(CancellationToken ct)
        {
            ExceptionDispatchInfo exInfo = null;
            _m.OperationType = TelemetryMetrics.DeviceOperationOpen;
            _m.ScheduleTime = null;
            _sw.Restart();
            try
            {
                Task t = s_dc.OpenAsync(ct);
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

        public override Task RunTestAsync(CancellationToken ct)
        {
            return SendMessageAsync(ct);
        }

        protected async Task SendMessageAsync(CancellationToken ct)
        {
            ExceptionDispatchInfo exInfo = null;
            _m.OperationType = TelemetryMetrics.DeviceOperationSend;
            _m.ScheduleTime = null;
            _sw.Restart();

            try
            {
                Client.Message message = new Client.Message(_messageBytes);
                Task t = s_dc.SendEventAsync(message, ct);
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

        public override async Task TeardownAsync(CancellationToken ct)
        {
            if (s_dc != null) return;
            await s_semaphore.WaitAsync(ct).ConfigureAwait(false);
            if (s_dc != null) return;

            await s_dc.CloseAsync().ConfigureAwait(false);
            s_dc = null;

            s_semaphore.Release();

        }
    }
}
