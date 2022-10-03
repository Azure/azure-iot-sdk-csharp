// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal class AmqpLinkFactory : ILinkFactory
    {
        public IAsyncResult BeginOpenLink(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state)
        {
            Debug.Fail($"{nameof(AmqpLinkFactory)} open link should not be used.");
            throw new NotImplementedException();
        }

        public AmqpLink CreateLink(AmqpSession session, AmqpLinkSettings settings)
        {
            if (Logging.IsEnabled)
                Logging.Info(this, session, nameof(CreateLink));

            return settings.IsReceiver()
                ? new ReceivingAmqpLink(session, settings)
                : new SendingAmqpLink(session, settings);
        }

        public void EndOpenLink(IAsyncResult result)
        {
            Debug.Fail($"{nameof(AmqpLinkFactory)} end link should not be used.");
            throw new NotImplementedException();
        }
    }
}
