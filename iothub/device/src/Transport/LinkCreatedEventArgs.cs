// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Transport.AmqpIoT;
using System;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class LinkCreatedEventArgs : EventArgs
    {
        public LinkCreatedEventArgs(AmqpIoTLink link)
        {
            Link = link;
        }

        public AmqpIoTLink Link { get; }
    }
}
