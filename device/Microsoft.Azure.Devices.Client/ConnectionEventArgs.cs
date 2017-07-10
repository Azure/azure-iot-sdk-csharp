// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;

    /// <summary>
    /// Connection event arguments for passing information from tranpsort to DeviceClient
    /// </summary>
    public class ConnectionEventArgs : EventArgs
    {
        public string ConnectionKey { get; set; }

        public ConnectionStatus ConnectionStatus { get; set; }

        public ConnectionStatusChangeReason ConnectionStatusChangeReason { get; set; }
    }
}
