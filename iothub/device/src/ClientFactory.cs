// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport;

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
        internal static InternalClient CreateFromConnectionString(
            string connectionString,
            IotHubClientOptions options)
        {
            Argument.AssertNotNullOrWhiteSpace(connectionString, nameof(connectionString));

            var builder = IotHubConnectionStringBuilder.CreateWithIAuthenticationOverride(connectionString, null);

            return CreateInternal(null, connectionString, builder.AuthenticationMethod, options);
        }

        /// <summary>
        /// Create an instance of InternalClient with the given parameters.
        /// </summary>
        /// <param name="hostName">The fully-qualified DNS hostname of IoT hub</param>
        /// <param name="authenticationMethod">The authentication method.</param>
        /// <param name="options">The optional client settings.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient Create(string hostName, IAuthenticationMethod authenticationMethod, IotHubClientOptions options = default)
        {
            if (hostName == null)
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            if (authenticationMethod == null)
            {
                throw new ArgumentNullException(nameof(authenticationMethod));
            }

            if (options == default)
            {
                options = new();
            }

            var connectionStringBuilder = IotHubConnectionStringBuilder.Create(hostName, options.GatewayHostName, authenticationMethod);

            // Make sure client options is initialized with the correct transport setting.
            EnsureOptionsIsSetup(connectionStringBuilder.Certificate, ref options);

            if (authenticationMethod is not DeviceAuthenticationWithX509Certificate)
            {
                return CreateInternal(null, connectionStringBuilder.ToString(), authenticationMethod, options);
            }

            // Prep for certificate auth.

            if (connectionStringBuilder.Certificate == null)
            {
                throw new ArgumentException("No certificate was found. To use certificate authentication certificate must be present.");
            }

            InternalClient internalClient = CreateInternal(
                null,
                connectionStringBuilder.ToString(),
                authenticationMethod,
                options);

            internalClient.Certificate = connectionStringBuilder.Certificate;

            // Install all the intermediate certificates in the chain if specified.
            if (connectionStringBuilder.ChainCertificates != null)
            {
                try
                {
                    CertificateInstaller.EnsureChainIsInstalled(connectionStringBuilder.ChainCertificates);
                }
                catch (Exception ex)
                {
                    if (Logging.IsEnabled)
                        Logging.Error(null, $"{nameof(CertificateInstaller)} failed to read or write to cert store due to: {ex}");

                    throw new UnauthorizedException($"Failed to provide certificates in the chain - {ex.Message}", ex);
                }
            }

            return internalClient;
        }

        /// <summary>
        /// This initializer exists only for unit tests to override the pipeline creation
        /// </summary>
        /// <param name="pipelineBuilder">Should only be specified by SDK tests to override the operation pipeline.</param>
        /// <param name="connectionString">The device connection string.</param>
        /// <param name="authenticationMethod">The authentication method.</param>
        /// <param name="options">The optional client settings.</param>
#if DEBUG
        internal
#else
        private
#endif
        static InternalClient CreateInternal(
            IDeviceClientPipelineBuilder pipelineBuilder,
            string connectionString,
            IAuthenticationMethod authenticationMethod,
            IotHubClientOptions options)
        {
            if (options == null)
            {
                options = new();
            }

            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (!string.IsNullOrWhiteSpace(options.ModelId)
                && options.TransportSettings is HttpTransportSettings)
            {
                throw new InvalidOperationException("Plug and Play is not supported over the HTTP transport.");
            }

            var builder = IotHubConnectionStringBuilder.CreateWithIAuthenticationOverride(
                connectionString,
                authenticationMethod);
            if (authenticationMethod == null)
            {
                authenticationMethod = builder.AuthenticationMethod;
            }

            // Clients that derive their authentication method from AuthenticationWithTokenRefresh will need to specify
            // the token time to live and renewal buffer values through the corresponding AuthenticationWithTokenRefresh
            // implementation constructors instead, and these values are irrelevant for cert-based auth.
            if (builder.AuthenticationMethod is not AuthenticationWithTokenRefresh
                && builder.AuthenticationMethod is not DeviceAuthenticationWithX509Certificate)
            {
                builder.SasTokenTimeToLive = options?.SasTokenTimeToLive ?? default;
                builder.SasTokenRenewalBuffer = options?.SasTokenRenewalBuffer ?? default;
            }

            if (authenticationMethod is DeviceAuthenticationWithX509Certificate certificate
                && certificate.ChainCertificates != null
                && (options.TransportSettings is not AmqpTransportSettings
                && options.TransportSettings is not MqttTransportSettings
                || options.TransportSettings.Protocol != TransportProtocol.Tcp))
            {
                throw new ArgumentException("Certificate chains are only supported on MQTT and AMQP over TCP.");
            }

            var iotHubConnectionString = builder.ToIotHubConnectionString();

            if (authenticationMethod is DeviceAuthenticationWithX509Certificate
                && builder.Certificate == null)
            {
                throw new ArgumentException("No certificate was found. To use certificate authentication certificate must be present.");
            }

            // Make sure client options is initialized with the correct transport setting.
            EnsureOptionsIsSetup(builder.Certificate, ref options);

            pipelineBuilder ??= BuildPipeline();

            var client = new InternalClient(iotHubConnectionString, pipelineBuilder, options);

            if (Logging.IsEnabled)
                Logging.CreateFromConnectionString(
                    client,
                    $"HostName={iotHubConnectionString.HostName};DeviceId={iotHubConnectionString.DeviceId};ModuleId={iotHubConnectionString.ModuleId}",
                    options.TransportSettings,
                    options);

            return client;
        }

        /// <summary>
        /// Ensures that the client options are configured and initialized.
        /// If a certificate is provided, the fileUploadTransportSettings will use it during initialization.
        /// </summary>
        private static void EnsureOptionsIsSetup(X509Certificate2 cert, ref IotHubClientOptions options)
        {
            if (options == null)
            {
                options = new();
            }

            if (options.FileUploadTransportSettings == null)
            {
                options.FileUploadTransportSettings = new();
            }

            if (cert != null)
            {
                if (options.FileUploadTransportSettings.ClientCertificate == null)
                {
                    options.FileUploadTransportSettings.ClientCertificate = cert;
                }

                if (options.TransportSettings.ClientCertificate == null)
                {
                    options.TransportSettings.ClientCertificate = cert;
                }
            }

        }

        private static IDeviceClientPipelineBuilder BuildPipeline()
        {
            var transporthandlerFactory = new TransportHandlerFactory();
            IDeviceClientPipelineBuilder pipelineBuilder = new DeviceClientPipelineBuilder()
                .With((ctx, innerHandler) => new RetryDelegatingHandler(ctx, innerHandler))
                .With((ctx, innerHandler) => new ErrorDelegatingHandler(ctx, innerHandler))
                .With((ctx, innerHandler) => transporthandlerFactory.Create(ctx));

            return pipelineBuilder;
        }
    }
}
