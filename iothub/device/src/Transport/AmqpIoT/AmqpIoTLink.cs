// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal abstract class AmqpIoTLink : AmqpLink
    {
        protected AmqpIoTLink(AmqpSession session, AmqpLinkSettings linkSettings) : base(session, linkSettings)
        {
        }

        protected AmqpIoTLink(string type, AmqpSession session, AmqpLinkSettings linkSettings) : base(type, session, linkSettings)
        {
        }
    }
}
