// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices
{
    // cc5b923d-ab24-57ee-bec8-d2f5cf1bb6e4
    [EventSource(Name = "Microsoft-Azure-Devices-Provisioning-Transport-Amqp")]
    internal sealed partial class Logging : EventSource
    {
    }
}
