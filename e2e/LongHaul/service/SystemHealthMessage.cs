// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    internal class SystemHealthMessage : MessageBase
    {
        public SystemHealthMessage(int port) 
        {
            s_tcpPortFilter = port;
        }

        private static readonly Process s_currentProcess = Process.GetCurrentProcess();
        public static long s_tcpPortFilter;
        public double processCpu { get; set; } = s_currentProcess.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount;
        public long totalAssignedMemoryBytes { get; set; } = s_currentProcess.WorkingSet64;
        public long totalGCBytes { get; set; } = GC.GetTotalMemory(false);
        public long tcpConnections { get; set; } = updateTCPConnections();


        private static long updateTCPConnections()
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();

            long n = 0;
            foreach (TcpConnectionInformation conn in connections)
            {
                if (s_tcpPortFilter != 0 && conn.RemoteEndPoint.Port != s_tcpPortFilter)
                    continue;
                if (conn.State == TcpState.Established)
                    n++;
            }
            return n;
        }
    }
}
