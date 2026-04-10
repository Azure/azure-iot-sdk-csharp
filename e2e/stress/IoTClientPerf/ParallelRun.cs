// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class ParallelRun
    {
        private const int StatUpdateIntervalMilliseconds = 1000;

        private PerfScenario[] _tests;
        private int _parallelOperations;
        Func<PerfScenario, Task> _operation;
        private Action<ulong, ulong, ulong, double> _updateStatistics;
        private int _statisticsUpdateIntervalMilliseconds;

        /// <summary>
        /// Runs tests in parallel, limiting the degree of parallelism.
        /// </summary>
        /// <param name="tests"></param>
        /// <param name="parallelOperations"></param>
        /// <param name="operation">Operation on the PerfScenario</param>
        /// <param name="updateStatistics">Params: interimCompleted, interimFaulted, interimCancelled, interimTimeSec</param>
        /// <param name="statisticsUpdateIntervalMilliseconds"></param>
        public ParallelRun(
            PerfScenario[] tests,
            int parallelOperations,
            Func<PerfScenario, Task> operation,
            Action<ulong, ulong, ulong, double> updateStatistics,
            int statisticsUpdateIntervalMilliseconds = StatUpdateIntervalMilliseconds)
        {
            _tests = tests;
            _parallelOperations = parallelOperations;
            _operation = operation;
            _updateStatistics = updateStatistics;
            _statisticsUpdateIntervalMilliseconds = statisticsUpdateIntervalMilliseconds;
        }

        public async Task RunAsync(bool runOnce, CancellationToken ct)
        {
            int cursor_left, cursor_top;
            cursor_left = Console.CursorLeft;
            cursor_top = Console.CursorTop;

            int actualParallel = Math.Min(_parallelOperations, _tests.Length);
            int currentInstance = 0;

            // Intermediate status update
            ulong statInterimCompleted = 0;
            ulong statInterimFaulted = 0;
            ulong statInterimCancelled = 0;

            ulong statTotalCompleted = 0;
            Stopwatch statInterimSw = new Stopwatch();
            statInterimSw.Start();

            var tasks = new List<Task>(actualParallel);

            // Start first batch of parallel tests.
            for (; currentInstance < actualParallel; currentInstance++)
            {
                ct.ThrowIfCancellationRequested();
                tasks.Add(_operation(_tests[currentInstance]));
            }

            bool drain = false;

            while (true)
            {
                Task finished = await Task.WhenAny(tasks).ConfigureAwait(false);
                tasks.Remove(finished);

                switch (finished.Status)
                {
                    case TaskStatus.Canceled:
                        statInterimCancelled++;
                        break;
                    case TaskStatus.Faulted:
                        statInterimFaulted++;
                        break;
                    case TaskStatus.RanToCompletion:
                        statInterimCompleted++;
                        break;
                    case TaskStatus.Running:
                    case TaskStatus.WaitingForActivation:
                    case TaskStatus.WaitingForChildrenToComplete:
                    case TaskStatus.Created:
                    case TaskStatus.WaitingToRun:
                    default:
                        Debug.Fail($"Invalid completed task state {finished.Status}");
                        break;
                }

                if ((statInterimSw.Elapsed.TotalMilliseconds > _statisticsUpdateIntervalMilliseconds) ||
                    tasks.Count == 0)
                {
                    statInterimSw.Stop();
                    double statInterimSeconds = statInterimSw.Elapsed.TotalSeconds;
                    statTotalCompleted += statInterimCompleted;

                    Console.SetCursorPosition(cursor_left, cursor_top);
                    cursor_left = Console.CursorLeft;
                    cursor_top = Console.CursorTop;

                    _updateStatistics(statInterimCompleted, statInterimFaulted, statInterimCancelled, statInterimSeconds);
                    if (drain) Console.Write("Waiting for tasks to finish...\r");

                    statInterimCompleted = 0;
                    statInterimFaulted = 0;
                    statInterimCancelled = 0;
                    statInterimSw.Restart();
                }

                if (ct.IsCancellationRequested) drain = true;

                if (!drain && (currentInstance >= _tests.Length))
                {
                    if (runOnce)
                    {
                        drain = true;
                    }
                    else
                    {
                        currentInstance = 0;
                    }
                }

                if (!drain)
                {
                    tasks.Add(_operation(_tests[currentInstance]));
                    currentInstance++;
                }
                else
                {
                    if (tasks.Count == 0) return;
                }
            }
        }
    }
}
