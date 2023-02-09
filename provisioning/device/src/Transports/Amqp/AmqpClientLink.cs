// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal sealed class AmqpClientLink
    {
        public const string ClientVersionName = "client-version";
        private readonly AmqpClientSession _amqpSession;

        public AmqpClientLink(AmqpClientSession amqpClientSession)
        {
            _amqpSession = amqpClientSession;

            AmqpLinkSettings = new AmqpLinkSettings
            {
                LinkName = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                TotalLinkCredit = AmqpConstants.DefaultLinkCredit,
                AutoSendFlow = true,
                Source = new Source(),
                Target = new Target(),
                SettleType = SettleMode.SettleOnDispose,
            };
        }

        internal AmqpLink AmqpLink { get; private set; }

        public AmqpLinkSettings AmqpLinkSettings { get; private set; }

        public bool IsLinkClosed { get; private set; }

        public async Task OpenAsync(CancellationToken cancellationToken)
        {
            AmqpLink = Extensions.IsReceiver(AmqpLinkSettings)
                ? new ReceivingAmqpLink(_amqpSession.AmqpSession, AmqpLinkSettings)
                : new SendingAmqpLink(_amqpSession.AmqpSession, AmqpLinkSettings);

            AmqpLink.SafeAddClosed(OnLinkClosed);
            await AmqpLink.OpenAsync(cancellationToken).ConfigureAwait(false);
            IsLinkClosed = false;
        }

        private void AddProperty(AmqpSymbol symbol, object value)
        {
            Extensions.AddProperty(AmqpLinkSettings, symbol, value);
        }

        public void AddApiVersion(string apiVersion)
        {
            AddProperty(AmqpConstants.Vendor + ":" + ClientApiVersionHelper.ApiVersionName, apiVersion);
        }

        public void AddClientVersion(string clientVersion)
        {
            AddProperty(AmqpConstants.Vendor + ":" + ClientVersionName, clientVersion);
        }

        public async Task<Outcome> SendMessageAsync(
            AmqpMessage message,
            ArraySegment<byte> deliveryTag,
            CancellationToken cancellationToken)
        {
            return AmqpLink is not SendingAmqpLink sendLink
                ? throw new InvalidOperationException("Link does not support sending.")
                : await sendLink
                .SendMessageAsync(message,
                    deliveryTag,
                    AmqpConstants.NullBinary,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<AmqpMessage> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            return AmqpLink is not ReceivingAmqpLink receiveLink
                ? throw new InvalidOperationException("Link does not support receiving.")
                : await receiveLink.ReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
        }

        public void AcceptMessage(AmqpMessage amqpMessage)
        {
            if (AmqpLink is not ReceivingAmqpLink receiveLink)
            {
                throw new InvalidOperationException("Link does not support receiving.");
            }
            receiveLink.AcceptMessage(amqpMessage, false);
        }

        private void OnLinkClosed(object o, EventArgs args)
        {
            IsLinkClosed = true;
        }
    }
}
