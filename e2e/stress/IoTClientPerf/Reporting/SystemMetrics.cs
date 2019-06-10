﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static class SystemMetrics
    {
        private const int RefreshIntervalMs = 500;
        private static readonly Stopwatch _sw = new Stopwatch();
        private static TimeSpan s_lastTotalCpuUsage = TimeSpan.Zero;
        private static int s_cpuPercent;
        private static long s_totalMemoryBytes;
        private static long s_lastGcBytes;
        private static long s_lastTcpConnections;
        private static object s_lock = new object();

        public static void GetMetrics(out int cpuPercent, out long memoryBytes, out long gcBytes, out long tcpConn)
        {
            EnsureUpToDate();
            cpuPercent = s_cpuPercent;
            memoryBytes = s_totalMemoryBytes;
            gcBytes = s_lastGcBytes;
            tcpConn = s_lastTcpConnections;
        }

        private static void UpdateCpuUsage()
        {
            var proc = Process.GetCurrentProcess();
            TimeSpan currentTotalCpuUsage = proc.TotalProcessorTime;

            long timeDelta = _sw.ElapsedMilliseconds;
            long usedTimeDelta = (long)(currentTotalCpuUsage - s_lastTotalCpuUsage).TotalMilliseconds;
            if (timeDelta != 0) s_cpuPercent = (int)(usedTimeDelta * 100 / (timeDelta * Environment.ProcessorCount));

            s_lastTotalCpuUsage = currentTotalCpuUsage;
        }

        private static void UpdateTotalMemoryBytes()
        {
            var proc = Process.GetCurrentProcess();
            s_totalMemoryBytes = proc.WorkingSet64;
        }

        private static void UpdateGCMemoryBytes()
        {
            s_lastGcBytes = GC.GetTotalMemory(false);
        }

        private static void UpdateTCPConnections()
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();

            long n  = 0;
            foreach (TcpConnectionInformation conn in connections)
            {
                if (conn.State == TcpState.Established) n++;
            }

            s_lastTcpConnections = n;
        }

        private static void EnsureUpToDate()
        {
            if (!_sw.IsRunning || _sw.ElapsedMilliseconds > RefreshIntervalMs)
            {
                lock (s_lock)
                {
                    UpdateCpuUsage();
                    UpdateGCMemoryBytes();
                    UpdateTCPConnections();
                    UpdateTotalMemoryBytes();

                    _sw.Restart();
                }
            }
        }
    }
}
