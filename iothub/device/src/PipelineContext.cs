// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    internal class PipelineContext
    {
        internal ITransportSettings TransportSettings { get; set; }

        internal ITransportSettings[] TransportSettingsArray { get; set; }

        internal ProductInfo ProductInfo { get; set; }

        internal IotHubConnectionString IotHubConnectionString { get; set; }

        internal ClientOptions ClientOptions { get; set; }

        internal ConnectionStatusChangesHandler ConnectionStatusChangesHandler { get; set; }

        internal Action<TwinCollection> DesiredPropertyUpdateCallback { get; set; }

        internal InternalClient.OnMethodCalledDelegate MethodCallback { get; set; }

        internal InternalClient.OnModuleEventMessageReceivedDelegate ModuleEventCallback { get; set; }

        internal InternalClient.OnDeviceMessageReceivedDelegate DeviceEventCallback { get; set; }
    }
}
