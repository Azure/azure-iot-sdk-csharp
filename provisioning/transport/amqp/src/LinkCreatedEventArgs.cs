// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using System;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class LinkCreatedEventArgs : EventArgs
    {
        public LinkCreatedEventArgs(AmqpLink link)
        {
            Link = link;
        }

        public AmqpLink Link { get; }
    }
}
