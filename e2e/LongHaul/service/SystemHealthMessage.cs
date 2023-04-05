// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    internal class SystemHealthMessage : MessageBase
    {
        private static readonly Process s_currentProcess = Process.GetCurrentProcess();
        private static DateTime? s_previousCpuStartTime = null;
        private static TimeSpan? s_previousTotalProcessorTime = null;
        // always include HTTPS and AMQP port.
        public static HashSet<int> TcpPortFilter = new() { 443, 5671 };

        [JsonPropertyName("processCpuUsagePercent")]
        public double ProcessCpuUsagePercent { get; set; } = UpdateCpuUsage();
        
        [JsonPropertyName("totalAssignedMemoryBytes")]
        public long TotalAssignedMemoryBytes { get; set; } = s_currentProcess.WorkingSet64;
        
        [JsonPropertyName("totalGCBytes")]
        public long TotalGCBytes { get; set; } = GC.GetTotalMemory(false);
        
        [JsonPropertyName("activeTcpConnections")] 
        public long ActiveTcpConnections { get; set; } = UpdateTcpConnections();

        public SystemHealthMessage(int port)
        {
            TcpPortFilter.Add(port);
        }

        private static long UpdateTcpConnections()
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            return connections
                .Where(c => TcpPortFilter.Contains(c.RemoteEndPoint.Port))
                .Where(c => c.State == TcpState.Established)
                .Count();
        }

        private static double UpdateCpuUsage()
        {
            DateTime currentCpuStartTime = DateTime.UtcNow;
            TimeSpan currentCpuUsage = s_currentProcess.TotalProcessorTime;

            // If no start time set then set to now
            if (!s_previousCpuStartTime.HasValue)
            {
                s_previousCpuStartTime = currentCpuStartTime;
                s_previousTotalProcessorTime = currentCpuUsage;
            }

            double cpuUsedMs = (currentCpuUsage - s_previousTotalProcessorTime.Value).TotalMilliseconds;
            double totalMsPassed = (currentCpuStartTime - s_previousCpuStartTime.Value).TotalMilliseconds;
            double cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            // Set previous times.
            s_previousCpuStartTime = currentCpuStartTime;
            s_previousTotalProcessorTime = currentCpuUsage;

            return cpuUsageTotal * 100.0;
        }
    }
}
