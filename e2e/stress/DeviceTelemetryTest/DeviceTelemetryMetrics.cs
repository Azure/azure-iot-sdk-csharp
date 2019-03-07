// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.Azure.Devices.E2ETests
{
    public struct DeviceTelemetryMetrics
    {
        public double? WallTime;
        public string Protocol;
        public int? MessageSize;
        public double? SendMs;
        public double? AckMs;
        public string ErrorMessage;

        public static string GetHeader()
        {
            return
                "WallTime," +
                "Protocol, " +
                "MessageSize," +
                "SendMs, " +
                "AckMs, " +
                "ErrorMessage, ";
        }

        public override string ToString()
        {
            var sb = new StringBuilder(); 
            Add(sb, WallTime);
            Add(sb, Protocol);
            Add(sb, MessageSize);
            Add(sb, SendMs);
            Add(sb, AckMs);
            Add(sb, ErrorMessage);

            return sb.ToString();
        }

        private void Add(StringBuilder sb, object data)
        {
            if (data != null)
            {
                sb.Append(data.ToString());
            }

            sb.Append(',');
        }
    }
}
