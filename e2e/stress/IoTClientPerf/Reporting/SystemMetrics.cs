// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static class SystemMetrics
    {
        private const int RefreshIntervalMs = 500;
        private static readonly Stopwatch s_sw = new Stopwatch();
        private static TimeSpan s_lastProcCpuUsageMs = TimeSpan.Zero;
        private static int s_cpuLoad;
        private static long s_totalMemoryBytes;
        private static long s_lastGcBytes;
        private static long s_lastTcpConnections;
        private static long s_tcpPortFilter;

        private static long s_devicesConnected;

        private static readonly object s_lock = new object();

        public static void GetMetrics(out int cpuLoad, out long memoryBytes, out long gcBytes, out long tcpConn, out long devicesConn)
        {
            EnsureUpToDate();
            cpuLoad = s_cpuLoad;
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
            TimeSpan elapsed = s_sw.Elapsed;
            Process proc = Process.GetCurrentProcess();

            if ((elapsed.Ticks != 0) && (s_lastProcCpuUsageMs != TimeSpan.Zero))
            {

                TimeSpan currentTotalCpuUsageMs = proc.TotalProcessorTime;
                TimeSpan usedTimeDelta = currentTotalCpuUsageMs - s_lastProcCpuUsageMs;

                s_cpuLoad = (int)(((double)usedTimeDelta.Ticks / elapsed.Ticks) * 100);
            }
            else
            {
                s_cpuLoad = -1;
            }

            s_lastProcCpuUsageMs = proc.TotalProcessorTime;
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
            if (!s_sw.IsRunning)
            {
                s_sw.Start();
            }
            else if (s_sw.ElapsedMilliseconds > RefreshIntervalMs)
            {
                lock (s_lock)
                {
                    UpdateGCMemoryBytes();
                    UpdateTCPConnections();
                    UpdateTotalMemoryBytes();
                    UpdateCpuUsage();

                    s_sw.Restart();
                }
            }
        }
    }
}
