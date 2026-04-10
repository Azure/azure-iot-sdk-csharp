// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static class SystemMetrics
    {
        private const int RefreshIntervalMs = 1000;
        private static readonly Stopwatch _sw = new Stopwatch();
        private static double s_lastTotalCpuUsageMs = 0.0;
        private static int s_cpuPercent;
        private static long s_totalMemoryBytes;
        private static long s_lastGcBytes;
        private static long s_lastTcpConnections;
        private static long s_tcpPortFilter;

        private static long s_devicesConnected;

        private static object s_lock = new object();

        public static void GetMetrics(out int cpuPercent, out long memoryBytes, out long gcBytes, out long tcpConn, out long devicesConn)
        {
            EnsureUpToDate();
            cpuPercent = s_cpuPercent;
            memoryBytes = s_totalMemoryBytes;
            gcBytes = s_lastGcBytes;
            tcpConn = s_lastTcpConnections;
            devicesConn = s_devicesConnected;
        }

        public static void DeviceConnected()
        {
            Interlocked.Increment(ref s_devicesConnected);
        }

        public static void DeviceDisconnected()
        {
            Interlocked.Decrement(ref s_devicesConnected);
        }

        public static void TcpFilterPort(int port)
        {
            s_tcpPortFilter = port;
        }

        private static void UpdateCpuUsage()
        {
            var proc = Process.GetCurrentProcess();
            double currentTotalCpuUsageMs = proc.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount;
            double timeDeltaMs = _sw.Elapsed.TotalMilliseconds;

            double usedTimeDeltaMs = currentTotalCpuUsageMs - s_lastTotalCpuUsageMs;
            if (timeDeltaMs > 0.1) s_cpuPercent = (int)(usedTimeDeltaMs * 100 / timeDeltaMs);
            if (s_cpuPercent > 100) s_cpuPercent = 100;

            s_lastTotalCpuUsageMs = currentTotalCpuUsageMs;
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
                if ((s_tcpPortFilter != 0) && (conn.RemoteEndPoint.Port != s_tcpPortFilter)) continue;
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
