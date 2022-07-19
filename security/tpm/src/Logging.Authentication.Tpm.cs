// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices
{
    // 06e3e7c9-2cd0-57c7-e3b3-c5965ff2736e
    [EventSource(Name = "Microsoft-Azure-Devices-Security-Tpm")]
    internal sealed partial class Logging : EventSource
    {
    }
}
