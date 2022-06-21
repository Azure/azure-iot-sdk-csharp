// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices
{
    // 2143dadd-f500-5ff9-12b3-9afacae4d54c
    [EventSource(Name = "Microsoft-Azure-Devices-Provisioning-Transport-Mqtt")]
    internal sealed partial class Logging : EventSource
    {
    }
}
