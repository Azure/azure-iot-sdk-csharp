// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTCbsToken : IAmqpIoTCbsToken
    {
        private CbsToken _cbsToken;

        public AmqpIoTCbsToken(object tokenValue, string tokenType, DateTime expiresAtUtc)
        {
            _cbsToken = new CbsToken(tokenValue, tokenType, expiresAtUtc);
        }

        public object GetTokenValue()
        {
            return _cbsToken.TokenValue;
        }
    }
}
