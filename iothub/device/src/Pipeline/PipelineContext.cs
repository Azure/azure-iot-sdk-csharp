// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    internal class PipelineContext
    {
        internal ClientConfiguration ClientConfiguration { get; set; }

        internal Action<ConnectionInfo> ConnectionStatusChangeHandler { get; set; }

        internal Action<TwinCollection> DesiredPropertyUpdateCallback { get; set; }

        internal Func<MethodRequestInternal, Task> MethodCallback { get; set; }

        internal Func<string, Message, Task> ModuleEventCallback { get; set; }

        internal Func<Message, Task> DeviceEventCallback { get; set; }
    }
}
