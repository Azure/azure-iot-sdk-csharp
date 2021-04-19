﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotLinkFactory : ILinkFactory
    {
        private static readonly AmqpIotLinkFactory s_instance = new AmqpIotLinkFactory();

        private AmqpIotLinkFactory()
        {
            // Important: must not throw as it's used within the static ctor.
        }

        public static AmqpIotLinkFactory GetInstance()
        {
            return s_instance;
        }

        public IAsyncResult BeginOpenLink(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state)
        {
            Debug.Fail($"{nameof(AmqpIotLinkFactory)} open link should not be used.");
            throw new NotImplementedException();
        }

        public AmqpLink CreateLink(AmqpSession session, AmqpLinkSettings settings)
        {
            if (Logging.IsEnabled)
            {
                Logging.Info(this, session, nameof(CreateLink));
            }

            return settings.IsReceiver()
                ? new ReceivingAmqpLink(session, settings)
                : (AmqpLink)new SendingAmqpLink(session, settings);
        }

        public void EndOpenLink(IAsyncResult result)
        {
            Debug.Fail($"{nameof(AmqpIotLinkFactory)} open link should not be used.");
            throw new NotImplementedException();
        }
    }
}
