// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices
{
    // d209b8a1-2e02-5724-f341-677227d0ed22
    [EventSource(Name = "Microsoft-Azure-Devices-Provisioning-Transport-Http")]
    internal sealed partial class Logging : EventSource
    {
    }
}
