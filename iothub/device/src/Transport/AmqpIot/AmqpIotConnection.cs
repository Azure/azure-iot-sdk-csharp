// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotConnection
    {
        public event EventHandler Closed;

        private readonly AmqpConnection _amqpConnection;
        private readonly AmqpIotCbsLink _amqpIotCbsLink;

        internal AmqpIotConnection(AmqpConnection amqpConnection)
        {
            _amqpConnection = amqpConnection;
            _amqpIotCbsLink = new AmqpIotCbsLink(new AmqpCbsLink(amqpConnection));
        }

        internal AmqpIotCbsLink GetCbsLink()
        {
            return _amqpIotCbsLink;
        }

        internal void AmqpConnectionClosed(object sender, EventArgs e)
        {
            if (Logging.IsEnabled)
            {
                Logging.Enter(this, nameof(AmqpConnectionClosed));
            }

            Closed?.Invoke(this, e);

            if (Logging.IsEnabled)
            {
                Logging.Exit(this, nameof(AmqpConnectionClosed));
            }
        }

        internal async Task<AmqpIotSession> OpenSessionAsync(CancellationToken cancellationToken)
        {
            if (_amqpConnection.IsClosing())
            {
                throw new IotHubCommunicationException("Amqp connection is disconnected.");
            }

            var amqpSessionSettings = new AmqpSessionSettings
            {
                Properties = new Fields(),
            };

            try
            {
                var amqpSession = new AmqpSession(_amqpConnection, amqpSessionSettings, AmqpIotLinkFactory.GetInstance());
                _amqpConnection.AddSession(amqpSession, new ushort?());
                await amqpSession.OpenAsync(cancellationToken).ConfigureAwait(false);
                return new AmqpIotSession(amqpSession);
            }
            catch (Exception e) when (!e.IsFatal())
            {
                Exception ex = AmqpIotExceptionAdapter.ConvertToIotHubException(e, _amqpConnection);
                if (ReferenceEquals(e, ex))
                {
                    throw;
                }
                else
                {
                    if (ex is AmqpIotResourceException)
                    {
                        _amqpConnection.SafeClose();
                        throw new IotHubCommunicationException(ex.Message, ex);
                    }
                    throw ex;
                }
            }
        }

        internal async Task<IAmqpAuthenticationRefresher> CreateRefresherAsync(DeviceIdentity deviceIdentity, CancellationToken cancellationToken)
        {
            if (_amqpConnection.IsClosing())
            {
                throw new IotHubCommunicationException("Amqp connection is disconnected.");
            }
            try
            {
                IAmqpAuthenticationRefresher amqpAuthenticator = new AmqpAuthenticationRefresher(deviceIdentity, _amqpIotCbsLink);
                await amqpAuthenticator.InitLoopAsync(cancellationToken).ConfigureAwait(false);
                return amqpAuthenticator;
            }
            catch (Exception e) when (!e.IsFatal())
            {
                Exception ex = AmqpIotExceptionAdapter.ConvertToIotHubException(e, _amqpConnection);
                if (ReferenceEquals(e, ex))
                {
                    throw;
                }
                else
                {
                    throw ex;
                }
            }
        }

        internal void SafeClose()
        {
            _amqpConnection.SafeClose();
        }

        internal bool IsClosing()
        {
            return _amqpConnection.IsClosing();
        }
    }
}
