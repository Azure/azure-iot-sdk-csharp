// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal interface IDeviceIdentity
    {
        AuthenticationModel AuthenticationModel { get; }
        AmqpTransportSettings AmqpTransportSettings { get; }
        IotHubConnectionString IotHubConnectionString { get; }
        ProductInfo ProductInfo { get; }
        ClientOptions Options { get; }
        string Audience { get; }

        bool IsPooling();
    }
}
