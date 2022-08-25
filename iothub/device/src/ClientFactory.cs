// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client.Exceptions;

namespace Microsoft.Azure.Devices.Client
{
    internal static class ClientFactory
    {
        /// <summary>
        /// Create an instance of InternalClient with the given parameters.
        /// </summary>
        /// <param name="connectionString">The device connection string.</param>
        /// <param name="options">The optional client settings.</param>
        /// <returns>An instance of InternalClient.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connectionString"/>, IoT hub host name or device Id is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="connectionString"/>, IoT hub host name or device Id are an empty string or consist only of white-space characters.</exception>
        /// <exception cref="ArgumentException">Neither shared access key nor shared access signature were presented for authentication.</exception>
        /// <exception cref="ArgumentException">Either shared access key or shared access signature where presented together with X509 certificates for authentication.</exception>
        internal static InternalClient CreateFromConnectionString(
            string connectionString,
            IotHubClientOptions options)
        {
            Argument.AssertNotNullOrWhiteSpace(connectionString, nameof(connectionString));

            if (options == default)
            {
                options = new();
            }

            var iotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString);
            return CreateInternal(null, iotHubConnectionCredentials, options);
        }

        /// <summary>
        /// Create an instance of <c>InternalClient</c> with the given parameters.
        /// </summary>
        /// <param name="hostName">The fully-qualified DNS host name of IoT hub</param>
        /// <param name="authenticationMethod">The authentication method.</param>
        /// <param name="options">The optional client settings.</param>
        /// <returns>InternalClient</returns>
        /// <exception cref="ArgumentNullException"><paramref name="hostName"/>, device Id or <paramref name="authenticationMethod"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="hostName"/> or device Id are an empty string or consist only of white-space characters.</exception>
        /// <exception cref="ArgumentException">Neither shared access key, shared access signature or X509 certificates were presented for authentication.</exception>
        /// <exception cref="ArgumentException">Either shared access key or shared access signature where presented together with X509 certificates for authentication.</exception>
        /// <exception cref="ArgumentException"><see cref="DeviceAuthenticationWithX509Certificate"/> is used but <see cref="DeviceAuthenticationWithX509Certificate.Certificate"/> is null.</exception>
        /// <exception cref="ArgumentException"><see cref="DeviceAuthenticationWithX509Certificate.ChainCertificates"/> is used over a protocol other than MQTT over TCP or AMQP over TCP></exception>
        /// <exception cref="IotHubClientException"><see cref="DeviceAuthenticationWithX509Certificate.ChainCertificates"/> could not be installed.</exception>
        internal static InternalClient Create(string hostName, IAuthenticationMethod authenticationMethod, IotHubClientOptions options)
        {
            Argument.AssertNotNullOrWhiteSpace(hostName, nameof(hostName));
            Argument.AssertNotNull(authenticationMethod, nameof(authenticationMethod));

            var iotHubConnectionCredentials = new IotHubConnectionCredentials(authenticationMethod, hostName, options?.GatewayHostName);

            // Make sure client options is initialized.
            if (options == null)
            {
                options = new();
            }

            // Validate certs.
            if (authenticationMethod is DeviceAuthenticationWithX509Certificate)
            {
                // Prep for certificate auth.
                if (iotHubConnectionCredentials.Certificate == null)
                {
                    throw new ArgumentException("No certificate was found. To use certificate authentication certificate must be present.");
                }

                if (iotHubConnectionCredentials.ChainCertificates != null)
                {
                    if (options.TransportSettings is not IotHubClientAmqpSettings
                        && options.TransportSettings is not IotHubClientMqttSettings
                        || options.TransportSettings.Protocol != IotHubClientTransportProtocol.Tcp)
                    {
                        throw new ArgumentException("Certificate chains are only supported on MQTT over TCP and AMQP over TCP.");
                    }

                    // Install all the intermediate certificates in the chain if specified.
                    try
                    {
                        CertificateInstaller.EnsureChainIsInstalled(iotHubConnectionCredentials.ChainCertificates);
                    }
                    catch (Exception ex)
                    {
                        if (Logging.IsEnabled)
                            Logging.Error(null, $"{nameof(CertificateInstaller)} failed to read or write to cert store due to: {ex}");

                        throw new IotHubClientException($"Failed to provide certificates in the chain - {ex.Message}", ex, false, IotHubStatusCode.Unauthorized);
                    }
                }
            }

            InternalClient internalClient = CreateInternal(null, iotHubConnectionCredentials, options);
            internalClient.Certificate = iotHubConnectionCredentials.Certificate;

            return internalClient;
        }

        /// <summary>
        /// This initializer exists only for unit tests to override the pipeline creation
        /// </summary>
        /// <param name="pipelineBuilder">Should only be specified by SDK tests to override the operation pipeline.</param>
        /// <param name="iotHubConnectionCredentials">The device connection string builder.</param>
        /// <param name="options">The optional client settings.</param>
#if DEBUG
        internal
#else
        private
#endif
        static InternalClient CreateInternal(
            IDeviceClientPipelineBuilder pipelineBuilder,
            IotHubConnectionCredentials iotHubConnectionCredentials,
            IotHubClientOptions options)
        {
            Argument.AssertNotNull(iotHubConnectionCredentials, nameof(iotHubConnectionCredentials));
            Argument.AssertNotNull(options, nameof(options));

            var client = new InternalClient(iotHubConnectionCredentials, options, pipelineBuilder);

            if (Logging.IsEnabled)
                Logging.CreateFromConnectionString(
                    client,
                    $"HostName={iotHubConnectionCredentials.HostName};DeviceId={iotHubConnectionCredentials.DeviceId};ModuleId={iotHubConnectionCredentials.ModuleId}",
                    options);

            return client;
        }
    }
}
