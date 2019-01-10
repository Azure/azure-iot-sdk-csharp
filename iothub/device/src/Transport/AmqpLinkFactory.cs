// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal class AmqpLinkFactory : ILinkFactory
    {
        public event EventHandler<LinkCreatedEventArgs> LinkCreated;

        public IAsyncResult BeginOpenLink(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return TaskHelpers.ToAsyncResult(OpenLinkAsync(link, timeout), callback, state);
        }

        public AmqpLink CreateLink(AmqpSession session, AmqpLinkSettings settings)
        {
            AmqpLink link;
            if (settings.IsReceiver())
            {
                link = new ReceivingAmqpLink(session, settings);
            }
            else
            {
                link = new SendingAmqpLink(session, settings);
            }
            OnLinkCreated(link);
            return link;
        }

        public static Task<bool> OpenLinkAsync(AmqpLink link, TimeSpan timeout)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (timeout.TotalMilliseconds > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return Task.FromResult(true);
        }

        protected virtual void OnLinkCreated(AmqpLink o)
        {
            LinkCreated?.Invoke(o, new LinkCreatedEventArgs(o));
        }

        public void EndOpenLink(IAsyncResult result)
        {
            Task task = result as Task;
            if (task == null)
            {
                throw new ArgumentNullException(nameof(result));
            }
        }
    }
}
