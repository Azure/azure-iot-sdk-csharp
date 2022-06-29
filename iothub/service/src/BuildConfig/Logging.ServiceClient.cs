// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices.Shared
{
    // 1a3d8d74-0a87-550c-89d7-b5d40ccb459b
    [EventSource(Name = "Microsoft-Azure-Devices-Service-Client")]
    internal sealed partial class Logging : EventSource
    {
    }
}