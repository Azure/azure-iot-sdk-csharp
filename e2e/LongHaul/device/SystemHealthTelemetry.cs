// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace Microsoft.Azure.Devices.LongHaul.Device
{
    internal class SystemHealthTelemetry : TelemetryBase
    {


        private static readonly Process s_currentProcess = Process.GetCurrentProcess();
        public static long TcpPortFilter;
        public double ProcessCpu { get; set; } = s_currentProcess.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount;
        public long TotalAssignedMemoryBytes { get; set; } = s_currentProcess.WorkingSet64;
        public long TotalGCBytes { get; set; } = GC.GetTotalMemory(false);
        public long TcpConnections { get; set; } = UpdateTCPConnections();

        public SystemHealthTelemetry(int port)
        {
            TcpPortFilter = port;
        }


        private static long UpdateTCPConnections()
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();

            long n = 0;
            foreach (TcpConnectionInformation conn in connections)
            {
                if (TcpPortFilter != 0 && conn.RemoteEndPoint.Port != TcpPortFilter)
                    continue;
                if (conn.State == TcpState.Established)
                    n++;
            }
            return n;
        }
    }
}
