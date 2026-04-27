// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Text;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class TelemetryMetrics
    {
        public const string DeviceOperationCreate = "device_create";
        public const string DeviceOperationOpen = "device_open";
        public const string DeviceOperationClose = "device_close";
        public const string DeviceOperationSend = "device_send";
        public const string DeviceOperationReceive = "device_receive";
        public const string DeviceOperationMethodEnable = "device_method_enable";
        public const string DeviceOperationMethodCalled = "device_method_called";

        public const string DeviceStateConnected = "device_state_connected";
        public const string DeviceStateDisconnected = "device_state_disconnected";
        public const string DeviceStateDisconnectedRetrying = "device_state_disconnected_retrying";
        public const string DeviceStateUnknown = "device_state_unknown";
        
        public const string ServiceOperationCreate = "service_create";
        public const string ServiceOperationOpen = "service_open";
        public const string ServiceOperationClose = "service_close";
        public const string ServiceOperationSend = "service_send";
        public const string ServiceOperationMethodCall = "service_method_call";

        private static string s_configString; // Contains all Config* parameters.
        public int? Id;
        public string OperationType; // e.g. OpenAsync / SendAsync, etc
        public double? ScheduleTime;
        public double? ExecuteTime;
        public string ErrorMessage;

        public static string GetHeader()
        {
            return
                "TimeStamp," +
                "Id," + // Application metrics.
                "Operation," +
                "ScheduleTimeMs," +
                "ExecuteTimeMs," +

                "CPU," +            // System metrics.
                "TotalMemoryBytes," +
                "GCMemoryBytes," +
                "TCPConnections," +

                "RunId," + // The following are for reporting / filtering purposes.
                "ConfigScenario," +
                "ConfigTimeSeconds," +
                "ConfigTransportType," +
                "ConfigMessageSizeBytes," +
                "ConfigParallelOperations," +
                "ConfigScenarioInstances," +
                "ConfigAuthType," +
                "ConfigPoolSize," +

                "ErrorMessage";
        }

        public static void SetStaticConfigParameters(
            string runId,
            int timeSeconds,
            Client.TransportType transportType,
            int messageSizeBytes,
            int maximumParallelOperations,
            int scenarioInstances,
            string authType,
            int poolSize,
            string scenario)
        {
            s_configString = $"{runId},{scenario},{timeSeconds},{transportType.ToString()},{messageSizeBytes},{maximumParallelOperations},{scenarioInstances},{authType},{poolSize}";
        }

        public override string ToString()
        {
            var sb = new StringBuilder(
                            DateTime.Now.ToUniversalTime()
                            .ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));

            Add(sb, Id);
            Add(sb, OperationType);
            Add(sb, ScheduleTime);
            Add(sb, ExecuteTime);

            SystemMetrics.GetMetrics(out int cpuPercent, out long memoryBytes, out long gcBytes, out long tcpConn, out long devConn);

            Add(sb, cpuPercent);
            Add(sb, memoryBytes);
            Add(sb, gcBytes);
            Add(sb, tcpConn);

            Add(sb, s_configString);
            Add(sb, ErrorMessage?.Replace('\n', ' ').Replace('\r', ' ').Replace('"', ' ').Replace(',', ' '));

            return sb.ToString();
        }

        private void Add(StringBuilder sb, object data)
        {
            sb.Append(',');
            if (data != null)
            {
                sb.Append(data.ToString());
            }
        }
    }
}
