// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System.Threading;

    /// <summary>
    /// Connection status change result supported by DeviceClient
    /// </summary>   
    public class ConnectionStatusChangeResult
    {
        private ConnectionStatus clientStatus = ConnectionStatus.Disabled;

        public CancellationTokenSource StatusChangeCancellationTokenSource { get; set; }

        public bool IsConnectionStatusChanged { get; set; }
        public bool IsClientStatusChanged { get; set; }

        public ConnectionStatus ClientStatus {
            get { return this.clientStatus; }
            set { this.clientStatus = value; }
        }
    }
}
