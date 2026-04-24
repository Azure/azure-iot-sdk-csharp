// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class HarnessBaseline : PerfScenario
    {
        private TelemetryMetrics _m = new TelemetryMetrics();
        private Stopwatch _sw = new Stopwatch();
        private byte[] _messageBytes;

        public HarnessBaseline(PerfScenarioConfig config) : base(config)
        {
            _m.Id = _id;
            _messageBytes = new byte[_sizeBytes];
            BitConverter.TryWriteBytes(_messageBytes, _id);
        }

        public override async Task SetupAsync(CancellationToken ct)
        {
            _sw.Restart();
            _m.OperationType = "baseline_setup";
            await Task.Delay(100).ConfigureAwait(false);
            _m.ExecuteTime = _sw.ElapsedMilliseconds;
            _m.ScheduleTime = null; // sync operation
            await _writer.WriteAsync(_m).ConfigureAwait(false);
        }

        public override async Task RunTestAsync(CancellationToken ct)
        {
            _sw.Restart();
            _m.OperationType = "baseline_run";
            await Task.Delay(100).ConfigureAwait(false);
            _m.ExecuteTime = _sw.ElapsedMilliseconds;
            _m.ScheduleTime = null; // sync operation
            await _writer.WriteAsync(_m).ConfigureAwait(false);
        }

        public override async Task TeardownAsync(CancellationToken ct)
        {
            _sw.Restart();
            _m.OperationType = "baseline_teardown";
            await Task.Delay(100).ConfigureAwait(false);
            _m.ExecuteTime = _sw.ElapsedMilliseconds;
            _m.ScheduleTime = null; // sync operation
            await _writer.WriteAsync(_m).ConfigureAwait(false);
        }
    }
}
