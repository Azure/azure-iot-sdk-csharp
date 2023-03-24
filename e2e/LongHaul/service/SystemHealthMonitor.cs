// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mash.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    /// <summary>
    /// Acts as a sensor for the hub, but what it "senses" is system and process health.
    /// In this way we can have the SDK be used for various functionality, but also get reports
    /// of its health.
    /// </summary>
    internal class SystemHealthMonitor
    {
        internal static SystemHealthC2DMessage BuildAndLogSystemHealth(Logger logger)
        {
            var message = new SystemHealthC2DMessage();
            logger.Metric(nameof(message.ProcessCpuUsagePercent), message.ProcessCpuUsagePercent);
            logger.Metric(nameof(message.ProcessWorkingSet), message.ProcessWorkingSet);
            logger.Metric(nameof(message.ProcessWorkingSetPrivate), message.ProcessWorkingSetPrivate);
            logger.Metric(nameof(message.ProcessPrivateBytes), message.ProcessPrivateBytes);
            if (message.ProcessBytesInAllHeaps.HasValue)
            {
                logger.Metric(nameof(message.ProcessBytesInAllHeaps), message.ProcessBytesInAllHeaps.Value);
            }

            return message;
        }
    }
}
