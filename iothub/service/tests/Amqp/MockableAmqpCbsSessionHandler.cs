// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Amqp;

namespace Microsoft.Azure.Devices.Tests.Amqp
{
    internal class MockableAmqpCbsSessionHandler : AmqpCbsSessionHandler
    {
        private readonly IotHubConnectionProperties _credential;
        private MockableAmqpCbsLink _cbsLink;
        private static readonly TimeSpan s_refreshTokenBuffer = TimeSpan.FromMinutes(2);
        private readonly IOThreadTimerSlim _refreshTokenTimer;

        public MockableAmqpCbsSessionHandler(IotHubConnectionProperties credential, EventHandler connectionLossHandler) : base(credential, connectionLossHandler)
        { }

        public async Task OpenAsync(MockableAmqpCbsLink cbsLink, CancellationToken cancellationToken)
        {
            _cbsLink = cbsLink;
            await SendCbsTokenAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task SendCbsTokenAsync(CancellationToken cancellationToken)
        {
            Uri amqpEndpoint = new UriBuilder(AmqpConstants.SchemeAmqps, _credential.HostName, AmqpConstants.DefaultSecurePort).Uri;

            string audience = amqpEndpoint.AbsoluteUri;
            string resource = amqpEndpoint.AbsoluteUri;

            DateTime expiresAtUtc = await _cbsLink.SendTokenAsync(
                    _credential,
                    amqpEndpoint,
                    audience,
                    resource,
                    _credential.AmqpAudience.ToArray(),
                    cancellationToken)
                .ConfigureAwait(false);

            ScheduleTokenRefresh(expiresAtUtc);
        }

        public void ScheduleTokenRefresh(DateTime expiresAtUtc)
        {
            if (expiresAtUtc == DateTime.MaxValue)
            {
                return;
            }

            TimeSpan timeFromNow = expiresAtUtc.Subtract(s_refreshTokenBuffer).Subtract(DateTime.UtcNow);
            if (timeFromNow > TimeSpan.Zero)
            {
                _refreshTokenTimer.Set(timeFromNow);
            }
        }
    }
}
