// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;

namespace Microsoft.Azure.Devices.Client
{
    internal class ClientFactory
    {
        /// <summary>
        /// Create an Amqp InternalClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient Create(string hostname, IAuthenticationMethod authenticationMethod, ClientOptions options = default)
        {
            if (hostname == null)
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            if (authenticationMethod == null)
            {
                throw new ArgumentNullException(nameof(authenticationMethod));
            }

            if (options == default)
            {
                options = new();
            }

            if (options.TransportType != TransportType.Amqp_Tcp_Only
                && options.TransportType != TransportType.Mqtt_Tcp_Only
                && authenticationMethod is DeviceAuthenticationWithX509Certificate certificate
                && certificate.ChainCertificates != null)
            {
                throw new ArgumentException("Certificate chains are only supported on Amqp_Tcp_Only and Mqtt_Tcp_Only");
            }

            if (!string.IsNullOrWhiteSpace(options?.ModelId)
                && options.TransportType == TransportType.Http1)
            {
                throw new InvalidOperationException("Plug and Play is not supported over the HTTP transport.");
            }

            var connectionStringBuilder = IotHubConnectionStringBuilder.Create(hostname, options.GatewayHostName, authenticationMethod);

            // Make sure client options is initialized with the correct transport setting.
            EnsureOptionsIsSetup(connectionStringBuilder.Certificate, ref options);

            if (authenticationMethod is DeviceAuthenticationWithX509Certificate)
            {
                if (connectionStringBuilder.Certificate == null)
                {
                    throw new ArgumentException("No certificate was found. To use certificate authentication certificate must be present.");
                }

                InternalClient internalClient = CreateFromConnectionString(
                    connectionStringBuilder.ToString(),
                    authenticationMethod,
                    PopulateCertificateInTransportSettings(connectionStringBuilder, options.TransportType),
                    null,
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

            return CreateFromConnectionString(connectionStringBuilder.ToString(), authenticationMethod, null, options);
        }

        /// <summary>
        /// Create a InternalClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings</param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient Create(
            string hostname,
            IAuthenticationMethod authenticationMethod,
            ITransportSettings[] transportSettings,
            ClientOptions options = default)
        {
            if (hostname == null)
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            if (authenticationMethod == null)
            {
                throw new ArgumentNullException(nameof(authenticationMethod));
            }

            if (options == default)
            {
                options = new();
            }

            var connectionStringBuilder = IotHubConnectionStringBuilder.Create(hostname, options.GatewayHostName, authenticationMethod);

            // Make sure client options is initialized with the correct transport setting.
            EnsureOptionsIsSetup(connectionStringBuilder.Certificate, ref options);

            if (!string.IsNullOrWhiteSpace(options.ModelId)
                && transportSettings.Any(x => x.GetTransportType() == TransportType.Http1))
            {
                throw new InvalidOperationException("Plug and Play is not supported over the HTTP transport.");
            }

            if (authenticationMethod is DeviceAuthenticationWithX509Certificate)
            {
                if (connectionStringBuilder.Certificate == null)
                {
                    throw new ArgumentException("No certificate was found. To use certificate authentication certificate must be present.");
                }

                InternalClient dc = CreateFromConnectionString(
                    connectionStringBuilder.ToString(),
                    PopulateCertificateInTransportSettings(connectionStringBuilder, transportSettings),
                    options);
                dc.Certificate = connectionStringBuilder.Certificate;
                return dc;
            }

            return CreateFromConnectionString(connectionStringBuilder.ToString(), authenticationMethod, transportSettings, null, options);
        }

        /// <summary>
        /// Create a InternalClient using Amqp transport from the specified connection string
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (including DeviceId)</param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient CreateFromConnectionString(string connectionString, ClientOptions options = default)
        {
            return CreateFromConnectionString(connectionString, transportSettings: null, options: options);
        }

        /// <summary>
        /// Create InternalClient from the specified connection string using a prioritized list of transports
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (with DeviceId)</param>
        /// <param name="transportSettings">Prioritized list of transports and their settings</param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient CreateFromConnectionString(
            string connectionString,
            ITransportSettings[] transportSettings,
            ClientOptions options = default)
        {
            return CreateFromConnectionString(connectionString, null, transportSettings, null, options);
        }

        internal static InternalClient CreateFromConnectionString(
            string connectionString,
            IAuthenticationMethod authenticationMethod,
            IDeviceClientPipelineBuilder pipelineBuilder,
            ClientOptions options = default)
        {
            if (options == default)
            {
                options = new();
            }

            ITransportSettings[] transportSettings = GetTransportSettings(options.TransportType);
            return CreateFromConnectionString(
                connectionString,
                authenticationMethod,
                transportSettings,
                pipelineBuilder,
                options);
        }

        internal static ITransportSettings[] GetTransportSettings(TransportType transportType)
        {
            return transportType switch
            {
                TransportType.Amqp_WebSocket_Only or TransportType.Amqp_Tcp_Only => new ITransportSettings[]
                    {
                        new AmqpTransportSettings(transportType)
                    },
                TransportType.Mqtt_WebSocket_Only or TransportType.Mqtt_Tcp_Only => new ITransportSettings[]
                    {
                        new MqttTransportSettings(transportType)
                    },
                TransportType.Http1 => new ITransportSettings[] { new Http1TransportSettings() },
                _ => throw new InvalidOperationException($"Unsupported transport type {transportType}"),
            };
        }

        internal static InternalClient CreateFromConnectionString(
            string connectionString,
            IAuthenticationMethod authenticationMethod,
            ITransportSettings[] transportSettings,
            IDeviceClientPipelineBuilder pipelineBuilder,
            ClientOptions options = default)
        {
            if (options == default)
            {
                options = new();
            }

            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (transportSettings == null)
            {
                transportSettings = GetTransportSettings(options.TransportType);
            }

            if (transportSettings.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(transportSettings), "Must specify at least one TransportSettings instance");
            }

            if (!string.IsNullOrWhiteSpace(options.ModelId)
                && transportSettings.Any(x => x.GetTransportType() == TransportType.Http1))
            {
                throw new InvalidOperationException("Plug and Play is not supported over the HTTP transport.");
            }

            var builder = IotHubConnectionStringBuilder.CreateWithIAuthenticationOverride(
                connectionString,
                authenticationMethod);

            // Clients that derive their authentication method from AuthenticationWithTokenRefresh will need to specify
            // the token time to live and renewal buffer values through the corresponding AuthenticationWithTokenRefresh
            // implementation constructors instead, and these values are irrelevant for cert-based auth.
            if (builder.AuthenticationMethod is not AuthenticationWithTokenRefresh
                && builder.AuthenticationMethod is not DeviceAuthenticationWithX509Certificate)
            {
                builder.SasTokenTimeToLive = options?.SasTokenTimeToLive ?? default;
                builder.SasTokenRenewalBuffer = options?.SasTokenRenewalBuffer ?? default;
            }

            var iotHubConnectionString = builder.ToIotHubConnectionString();

            foreach (ITransportSettings transportSetting in transportSettings)
            {
                switch (transportSetting.GetTransportType())
                {
                    case TransportType.Amqp_WebSocket_Only:
                    case TransportType.Amqp_Tcp_Only:
                        if (transportSetting is not AmqpTransportSettings)
                        {
                            throw new InvalidOperationException("Unknown implementation of ITransportSettings type");
                        }
                        break;

                    case TransportType.Http1:
                        if (transportSetting is not Http1TransportSettings)
                        {
                            throw new InvalidOperationException("Unknown implementation of ITransportSettings type");
                        }
                        break;

                    case TransportType.Mqtt_WebSocket_Only:
                    case TransportType.Mqtt_Tcp_Only:
                        if (transportSetting is not MqttTransportSettings)
                        {
                            throw new InvalidOperationException("Unknown implementation of ITransportSettings type");
                        }
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Unsupported Transport Type {transportSetting.GetTransportType()}");
                }
            }

            if (authenticationMethod is DeviceAuthenticationWithX509Certificate
                && builder.Certificate == null)
            {
                throw new ArgumentException("No certificate was found. To use certificate authentication certificate must be present.");
            }

            // Make sure client options is initialized with the correct transport setting.
            EnsureOptionsIsSetup(builder.Certificate, ref options);

            pipelineBuilder ??= BuildPipeline();

            // Defer concrete InternalClient creation to OpenAsync
            var client = new InternalClient(iotHubConnectionString, transportSettings, pipelineBuilder, options);

            if (Logging.IsEnabled)
                Logging.CreateFromConnectionString(
                    client,
                    $"HostName={iotHubConnectionString.HostName};DeviceId={iotHubConnectionString.DeviceId};ModuleId={iotHubConnectionString.ModuleId}",
                    transportSettings,
                    options);

            return client;
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
                .With((ctx, innerHandler) => new ProtocolRoutingDelegatingHandler(ctx, innerHandler))
                .With((ctx, innerHandler) => transporthandlerFactory.Create(ctx));

            return pipelineBuilder;
        }

        private static ITransportSettings[] PopulateCertificateInTransportSettings(
            IotHubConnectionStringBuilder connectionStringBuilder,
            TransportType transportType)
        {
            return transportType switch
            {
                TransportType.Amqp_Tcp_Only => new ITransportSettings[]
                    {
                        new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    },
                TransportType.Amqp_WebSocket_Only => new ITransportSettings[]
                    {
                        new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    },
                TransportType.Http1 => new ITransportSettings[]
                    {
                        new Http1TransportSettings()
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    },
                TransportType.Mqtt_Tcp_Only => new ITransportSettings[]
                    {
                        new MqttTransportSettings(TransportType.Mqtt_Tcp_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    },
                TransportType.Mqtt_WebSocket_Only => new ITransportSettings[]
                    {
                        new MqttTransportSettings(TransportType.Mqtt_WebSocket_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    },
                _ => throw new InvalidOperationException($"Unsupported Transport {transportType}"),
            };
        }

        private static ITransportSettings[] PopulateCertificateInTransportSettings(
            IotHubConnectionStringBuilder connectionStringBuilder,
            ITransportSettings[] transportSettings)
        {
            foreach (ITransportSettings transportSetting in transportSettings)
            {
                switch (transportSetting.GetTransportType())
                {
                    case TransportType.Amqp_WebSocket_Only:
                    case TransportType.Amqp_Tcp_Only:
                        ((AmqpTransportSettings)transportSetting).ClientCertificate = connectionStringBuilder.Certificate;
                        break;

                    case TransportType.Http1:
                        ((Http1TransportSettings)transportSetting).ClientCertificate = connectionStringBuilder.Certificate;
                        break;

                    case TransportType.Mqtt_WebSocket_Only:
                    case TransportType.Mqtt_Tcp_Only:
                        ((MqttTransportSettings)transportSetting).ClientCertificate = connectionStringBuilder.Certificate;
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported Transport {transportSetting.GetTransportType()}");
                }
            }

            return transportSettings;
        }
    }
}
