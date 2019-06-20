// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpTokenRefresherCreator
    {
        Task<IAmqpIoTAuthenticationRefresher> CreateRefresher(DeviceIdentity deviceIdentity, TimeSpan timeout);
    }
}
