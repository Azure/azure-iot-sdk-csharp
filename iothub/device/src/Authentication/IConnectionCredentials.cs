// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Interface for client connection credentials.
    /// This has been included for our own unit testing.
    /// </summary>
    internal interface IConnectionCredentials
    {
        /// <summary>
        /// The fully-qualified DNS host name of the IoT hub service.
        /// </summary>
        string IotHubHostName { get; }

        /// <summary>
        /// The optional name of the gateway service to connect to.
        /// </summary>
        string GatewayHostName { get; }

        /// <summary>
        /// The host service that this client connects to.
        /// This can either be the IoT hub name or a gateway service name.
        /// </summary>
        string HostName { get; }

        /// <summary>
        /// The device identifier of the device connecting to the service.
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// The module identifier of the module connecting to the service.
        /// </summary>
        string ModuleId { get; }

        /// <summary>
        /// The authentication model for the device; i.e. X.509 certificates, individual client scoped SAS tokens or IoT hub level scoped SAS tokens.
        /// </summary>
        AuthenticationModel AuthenticationModel { get; }

        /// <summary>
        /// The shared access key name used to connect the device to the IoT hub service.
        /// </summary>
        string SharedAccessKeyName { get; }

        /// <summary>
        /// The shared access key used to connect to the IoT hub service.
        /// </summary>
        string SharedAccessKey { get; }

        /// <summary>
        /// The shared access signature used to connect to the IoT hub service.
        /// </summary>
        /// <remarks>
        /// This is used when a device app creates its own limited-lifespan SAS token, instead of letting
        /// this SDK derive one from a shared access token. When a device client is initialized with a
        /// SAS token, when that token expires, the client must be disposed, and if desired, recreated
        /// with a newly derived SAS token.
        /// </remarks>
        string SharedAccessSignature { get; }

        /// <summary>
        /// The client X509 certificates used for authenticating with IoT hub.
        /// </summary>
        X509Certificate2 Certificate { get; }

        /// <summary>
        /// The full chain of certificates from the one used to sign the client certificate to the one uploaded to the service.
        /// </summary>
        X509Certificate2Collection ChainCertificates { get; }

        /// <summary>
        /// The suggested time to live value for tokens generated for SAS authenticated clients.
        /// </summary>
        TimeSpan SasTokenTimeToLive { get; }

        /// <summary>
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        /// </summary>
        int SasTokenRenewalBuffer { get; }

        /// <summary>
        /// The token refresh logic to be used for clients authenticating with either an ClientAuthenticationWithTokenRefresh IAuthenticationMethod mechanism
        /// or through a shared access key value that can be used by the SDK to generate SAS tokens.
        /// </summary>
        ClientAuthenticationWithTokenRefresh SasTokenRefresher { get; }

        /// <summary>
        /// The authentication method to be used with the IoT hub service.
        /// </summary>
        IAuthenticationMethod AuthenticationMethod { get; }

        /// <summary>
        /// Gets the SAS token credential required for authenticating the client with IoT hub service.
        /// </summary>
        Task<string> GetPasswordAsync();
    }
}
