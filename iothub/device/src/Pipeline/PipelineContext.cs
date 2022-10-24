// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    internal class PipelineContext
    {
        internal IotHubConnectionCredentials IotHubConnectionCredentials { get; set; }

        internal ProductInfo ProductInfo { get; set; }

        internal string ModelId { get; set; }

        internal PayloadConvention PayloadConvention { get; set; }

        internal IotHubClientTransportSettings IotHubClientTransportSettings { get; set; }

        internal Action<ConnectionStatusInfo> ConnectionStatusChangeHandler { get; set; }

        internal Action<DesiredProperties> DesiredPropertyUpdateCallback { get; set; }

        internal Func<DirectMethodRequest, Task> MethodCallback { get; set; }

        internal Func<IncomingMessage, Task<MessageAcknowledgement>> MessageEventCallback { get; set; }
    }
}
