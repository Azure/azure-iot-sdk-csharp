// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Transport;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Interface for device configurations and common attributes.
    /// </summary>
    internal interface IIotHubConnectionInfo : IAuthorizationProvider
    {
        AuthenticationWithTokenRefresh TokenRefresher { get; }

        string IotHubName { get; }

        string DeviceId { get; }

        string ModuleId { get; }

        string HostName { get; }

        Uri HttpsEndpoint { get; }

        Uri AmqpEndpoint { get; }

        string SharedAccessKeyName { get; }

        string SharedAccessKey { get; }

        string SharedAccessSignature { get; }

        bool IsUsingGateway { get; }

        AuthenticationModel AuthenticationModel { get; }

        IotHubClientAmqpSettings AmqpTransportSettings { get; }

        ProductInfo ProductInfo { get; }

        IotHubClientOptions ClientOptions { get; }

        string Audience { get; }

        bool IsPooling();

        Uri BuildLinkAddress(string path);
    }
}
