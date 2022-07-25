// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;

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
            ClientOptions options)
        {
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
        internal static InternalClient Create(string hostName, IAuthenticationMethod authenticationMethod, ClientOptions options = default)
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
            ClientOptions options)
        {
            Debug.Assert(options != null);

            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (!string.IsNullOrWhiteSpace(options.ModelId)
                && options.TransportSettings.GetTransportType() == TransportType.Http)
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

            var transportType = options.TransportSettings.GetTransportType();
            if (transportType != TransportType.Amqp_Tcp_Only
                && transportType != TransportType.Mqtt_Tcp_Only
                && authenticationMethod is DeviceAuthenticationWithX509Certificate certificate
                && certificate.ChainCertificates != null)
            {
                throw new ArgumentException("Certificate chains are only supported on Amqp_Tcp_Only and Mqtt_Tcp_Only");
            }

            var iotHubConnectionString = builder.ToIotHubConnectionString();

            switch (options.TransportSettings.GetTransportType())
            {
                case TransportType.Amqp_WebSocket_Only:
                case TransportType.Amqp_Tcp_Only:
                    if (options.TransportSettings is not AmqpTransportSettings)
                    {
                        throw new InvalidOperationException("Unknown implementation of ITransportSettings type");
                    }
                    break;

                case TransportType.Http:
                    if (options.TransportSettings is not HttpTransportSettings)
                    {
                        throw new InvalidOperationException("Unknown implementation of ITransportSettings type");
                    }
                    break;

                case TransportType.Mqtt_WebSocket_Only:
                case TransportType.Mqtt_Tcp_Only:
                    if (options.TransportSettings is not MqttTransportSettings)
                    {
                        throw new InvalidOperationException("Unknown implementation of ITransportSettings type");
                    }
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported Transport Type {options.TransportSettings.GetTransportType()}");
            }

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

        internal static ITransportSettings GetTransportSettings(TransportType transportType)
        {
            return transportType switch
            {
                TransportType.Amqp_WebSocket_Only or TransportType.Amqp_Tcp_Only => new AmqpTransportSettings(transportType),
                TransportType.Mqtt_WebSocket_Only or TransportType.Mqtt_Tcp_Only => new MqttTransportSettings(transportType),
                TransportType.Http => new HttpTransportSettings(),
                _ => throw new InvalidOperationException($"Unsupported transport type {transportType}"),
            };
        }

        /// <summary>
        /// Ensures that the ClientOptions is configured and initialized.
        /// If a certificate is provided, the fileUploadTransportSettings will use it during initialization.
        /// </summary>
        private static void EnsureOptionsIsSetup(X509Certificate2 cert, ref ClientOptions options)
        {
            if (options == null)
            {
                options = new();
            }

            if (options.FileUploadTransportSettings == null)
            {
                options.FileUploadTransportSettings = new();
            }

            if (cert != null
                && options.FileUploadTransportSettings.ClientCertificate == null)
            {
                options.FileUploadTransportSettings.ClientCertificate = cert;
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

        private static ITransportSettings PopulateCertificateInTransportSettings(
            IotHubConnectionStringBuilder csBuilder,
            TransportType transportType)
        {
            return transportType switch
            {
                TransportType.Amqp_Tcp_Only => new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
                {
                    ClientCertificate = csBuilder.Certificate
                },
                TransportType.Amqp_WebSocket_Only => new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
                {
                    ClientCertificate = csBuilder.Certificate
                },
                TransportType.Http => new HttpTransportSettings
                {
                    ClientCertificate = csBuilder.Certificate
                },
                TransportType.Mqtt_Tcp_Only => new MqttTransportSettings(TransportType.Mqtt_Tcp_Only)
                {
                    ClientCertificate = csBuilder.Certificate
                },
                TransportType.Mqtt_WebSocket_Only => new MqttTransportSettings(TransportType.Mqtt_WebSocket_Only)
                {
                    ClientCertificate = csBuilder.Certificate
                },
                _ => throw new InvalidOperationException($"Unsupported Transport {transportType}"),
            };
        }

        private static ITransportSettings PopulateCertificateInTransportSettings(
            IotHubConnectionStringBuilder connectionStringBuilder,
            ITransportSettings transportSettings)
        {
            switch (transportSettings.GetTransportType())
            {
                case TransportType.Amqp_WebSocket_Only:
                case TransportType.Amqp_Tcp_Only:
                    ((AmqpTransportSettings)transportSettings).ClientCertificate = connectionStringBuilder.Certificate;
                    break;

                case TransportType.Http:
                    ((HttpTransportSettings)transportSettings).ClientCertificate = connectionStringBuilder.Certificate;
                    break;

                case TransportType.Mqtt_WebSocket_Only:
                case TransportType.Mqtt_Tcp_Only:
                    ((MqttTransportSettings)transportSettings).ClientCertificate = connectionStringBuilder.Certificate;
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported Transport {transportSettings.GetTransportType()}");
            }

            return transportSettings;
        }
    }
}
