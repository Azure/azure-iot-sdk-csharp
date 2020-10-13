// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTLinkFactory : ILinkFactory
    {
        private static AmqpIoTLinkFactory s_instance = new AmqpIoTLinkFactory();

        private AmqpIoTLinkFactory()
        {
            // Important: must not throw as it's used within the static ctor.
        }

        public static AmqpIoTLinkFactory GetInstance()
        {
            return s_instance;
        }

        public IAsyncResult BeginOpenLink(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state)
        {
            Debug.Fail($"{nameof(AmqpIoTLinkFactory)} open link should not be used.");
            throw new NotImplementedException();
        }

        public AmqpLink CreateLink(AmqpSession session, AmqpLinkSettings settings)
        {
            if (Logging.IsEnabled)
            {
                Logging.Info(this, session, $"{nameof(CreateLink)}");
            }

            if (settings.IsReceiver())
            {
                return new ReceivingAmqpLink(session, settings);
            }
            else
            {
                return new SendingAmqpLink(session, settings);
            }
        }

        public void EndOpenLink(IAsyncResult result)
        {
            Debug.Fail($"{nameof(AmqpIoTLinkFactory)} open link should not be used.");
            throw new NotImplementedException();
        }
    }
}
