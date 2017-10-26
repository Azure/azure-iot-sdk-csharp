// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Amqp
{
    public class LinkCreatedEventArgs : EventArgs
    {
        public LinkCreatedEventArgs(AmqpLink link)
        {
            Link = link;
        }

        public AmqpLink Link { get; }
    }
}
