// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    internal class ClientFactory
    {
        private const string DeviceId = "DeviceId";
        private const string DeviceIdParameterPattern = @"(^\s*?|.*;\s*?)" + DeviceId + @"\s*?=.*";

        private static readonly TimeSpan s_regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);

        private static readonly Regex s_deviceIdParameterRegex = new Regex(
            DeviceIdParameterPattern,
            RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);

        /// <summary>
        /// Create an Amqp InternalClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient Create(string hostname, IAuthenticationMethod authenticationMethod, ClientOptions options = default)
        {
            return Create(hostname, authenticationMethod, TransportType.Amqp, options);
        }

        /// <summary>
        /// Create an Amqp InternalClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="gatewayHostname">The fully-qualified DNS hostname of Gateway</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient Create(
            string hostname,
            string gatewayHostname,
            IAuthenticationMethod authenticationMethod,
            ClientOptions options = default)
        {
            return Create(hostname, gatewayHostname, authenticationMethod, TransportType.Amqp, options);
        }

        /// <summary>
        /// Create a InternalClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportType">The transportType used (Http1, Amqp or Mqtt), <see cref="TransportType"/></param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient Create(
            string hostname,
            IAuthenticationMethod authenticationMethod,
            TransportType transportType,
            ClientOptions options = default)
        {
            return Create(hostname, null, authenticationMethod, transportType, options);
        }

        /// <summary>
        /// Create a InternalClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="gatewayHostname">The fully-qualified DNS hostname of Gateway</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportType">The transportType used (Http1, Amqp or Mqtt), <see cref="TransportType"/></param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient Create(
            string hostname,
            string gatewayHostname,
            IAuthenticationMethod authenticationMethod,
            TransportType transportType,
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

            if (transportType != TransportType.Amqp_Tcp_Only
                && transportType != TransportType.Mqtt_Tcp_Only
                && authenticationMethod is DeviceAuthenticationWithX509Certificate certificate
                && certificate.ChainCertificates != null)
            {
                throw new ArgumentException("Certificate chains are only supported on Amqp_Tcp_Only and Mqtt_Tcp_Only");
            }

            if (!string.IsNullOrWhiteSpace(options?.ModelId)
                && transportType == TransportType.Http1)
            {
                throw new InvalidOperationException("Plug and Play is not supported over the HTTP transport.");
            }

            var connectionStringBuilder = IotHubConnectionStringBuilder.Create(hostname, gatewayHostname, authenticationMethod);

            // Make sure client options is initialized with the correct transport setting.
            EnsureOptionsIsSetup(connectionStringBuilder.Certificate, null, ref options);

            if (authenticationMethod is DeviceAuthenticationWithX509Certificate)
            {
                if (connectionStringBuilder.Certificate == null)
                {
                    throw new ArgumentException("No certificate was found. To use certificate authentication certificate must be present.");
                }

                InternalClient dc = CreateFromConnectionString(
                    connectionStringBuilder.ToString(),
                    PopulateCertificateInTransportSettings(connectionStringBuilder, transportType),
                    options);

                dc.Certificate = connectionStringBuilder.Certificate;

                // Install all the intermediate certificates in the chain if specified.
                if (connectionStringBuilder.ChainCertificates != null)
                {
                    try
                    {
                        CertificateInstaller.EnsureChainIsInstalled(connectionStringBuilder.ChainCertificates);
                    }
                    catch (Exception ex)
                    {
                        if (Logging.IsEnabled) Logging.Error(null, $"{nameof(CertificateInstaller)} failed to read or write to cert store due to: {ex}");
                        throw new UnauthorizedException($"Failed to provide certificates in the chain - {ex.Message}", ex);
                    }
                }

                return dc;
            }

            return CreateFromConnectionString(connectionStringBuilder.ToString(), authenticationMethod, transportType, null, options);
        }

        /// <summary>
        /// Create a InternalClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
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
            return Create(hostname, null, authenticationMethod, transportSettings, options);
        }

        /// <summary>
        /// Create a InternalClient from individual parameters
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="gatewayHostname">The fully-qualified DNS hostname of Gateway</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings</param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient Create(
            string hostname,
            string gatewayHostname,
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

            if (!string.IsNullOrWhiteSpace(options?.ModelId)
                && transportSettings.Any(x => x.GetTransportType() == TransportType.Http1))
            {
                throw new InvalidOperationException("Plug and Play is not supported over the HTTP transport.");
            }

            var connectionStringBuilder = IotHubConnectionStringBuilder.Create(hostname, gatewayHostname, authenticationMethod);

            // Make sure client options is initialized with the correct transport setting.
            EnsureOptionsIsSetup(connectionStringBuilder.Certificate, transportSettings.FirstOrDefault()?.Proxy, ref options);

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
            return CreateFromConnectionString(connectionString, TransportType.Amqp, options);
        }

        /// <summary>
        /// Create a InternalClient using Amqp transport from the specified connection string
        /// </summary>
        /// <param name="connectionString">IoT Hub-Scope Connection string for the IoT hub (without DeviceId)</param>
        /// <param name="deviceId">Id of the Device</param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient CreateFromConnectionString(
            string connectionString,
            string deviceId,
            ClientOptions options = default)
        {
            return CreateFromConnectionString(connectionString, deviceId, TransportType.Amqp, options);
        }

        /// <summary>
        /// Create InternalClient from the specified connection string using the specified transport type
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (including DeviceId)</param>
        /// <param name="transportType">Specifies whether Http1, Amqp or Mqtt transport is used, <see cref="TransportType"/></param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient CreateFromConnectionString(
            string connectionString,
            TransportType transportType,
            ClientOptions options = default)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            return CreateFromConnectionString(connectionString, null, transportType, null, options);
        }

        /// <summary>
        /// Create InternalClient from the specified connection string using the specified transport type
        /// </summary>
        /// <param name="connectionString">IoT Hub-Scope Connection string for the IoT hub (without DeviceId)</param>
        /// <param name="deviceId">Id of the device</param>
        /// <param name="transportType">The transportType used (Http1, Amqp or Mqtt), <see cref="TransportType"/></param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient CreateFromConnectionString(
            string connectionString,
            string deviceId,
            TransportType transportType,
            ClientOptions options = default)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (deviceId == null)
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            if (s_deviceIdParameterRegex.IsMatch(connectionString))
            {
                throw new ArgumentException("Connection string must not contain DeviceId keyvalue parameter", nameof(connectionString));
            }

            return CreateFromConnectionString(connectionString + ";" + DeviceId + "=" + deviceId, transportType, options);
        }

        /// <summary>
        /// Create InternalClient from the specified connection string using a prioritized list of transports
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (with DeviceId)</param>
        /// <param name="transportSettings">Prioritized list of transports and their settings</param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient CreateFromConnectionString(string connectionString,
            ITransportSettings[] transportSettings, ClientOptions options = default)
        {
            return CreateFromConnectionString(connectionString, null, transportSettings, null, options);
        }

        /// <summary>
        /// Create InternalClient from the specified connection string using the prioritized list of transports
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub (without DeviceId)</param>
        /// <param name="deviceId">Id of the device</param>
        /// <param name="transportSettings">Prioritized list of transportTypes and their settings</param>
        /// <param name="options">The options that allow configuration of the device client instance during initialization.</param>
        /// <returns>InternalClient</returns>
        internal static InternalClient CreateFromConnectionString(string connectionString, string deviceId, ITransportSettings[] transportSettings, ClientOptions options = default)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (deviceId == null)
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            if (s_deviceIdParameterRegex.IsMatch(connectionString))
            {
                throw new ArgumentException("Connection string must not contain DeviceId keyvalue parameter", nameof(connectionString));
            }

            return CreateFromConnectionString(connectionString + ";" + DeviceId + "=" + deviceId, transportSettings, options);
        }

        internal static InternalClient CreateFromConnectionString(
            string connectionString,
            IAuthenticationMethod authenticationMethod,
            TransportType transportType,
            IDeviceClientPipelineBuilder pipelineBuilder,
            ClientOptions options = default)
        {
            ITransportSettings[] transportSettings = GetTransportSettings(transportType);
            return CreateFromConnectionString(
                connectionString,
                authenticationMethod,
                transportSettings,
                pipelineBuilder,
                options);
        }

        internal static ITransportSettings[] GetTransportSettings(TransportType transportType)
        {
            switch (transportType)
            {
                case TransportType.Amqp:
                    return new ITransportSettings[]
                    {
                        new AmqpTransportSettings(TransportType.Amqp_Tcp_Only),
                        new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
                    };

                case TransportType.Mqtt:
                    return new ITransportSettings[]
                    {
                        new MqttTransportSettings(TransportType.Mqtt_Tcp_Only),
                        new MqttTransportSettings(TransportType.Mqtt_WebSocket_Only)
                    };

                case TransportType.Amqp_WebSocket_Only:
                case TransportType.Amqp_Tcp_Only:
                    return new ITransportSettings[]
                    {
                        new AmqpTransportSettings(transportType)
                    };

                case TransportType.Mqtt_WebSocket_Only:
                case TransportType.Mqtt_Tcp_Only:
                    return new ITransportSettings[]
                    {
                        new MqttTransportSettings(transportType)
                    };

                case TransportType.Http1:
                    return new ITransportSettings[] { new Http1TransportSettings() };

                default:
                    throw new InvalidOperationException("Unsupported Transport Type {0}".FormatInvariant(transportType));
            }
        }

        internal static InternalClient CreateFromConnectionString(
            string connectionString,
            IAuthenticationMethod authenticationMethod,
            ITransportSettings[] transportSettings,
            IDeviceClientPipelineBuilder pipelineBuilder,
            ClientOptions options = default)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (transportSettings == null)
            {
                throw new ArgumentNullException(nameof(transportSettings));
            }

            if (transportSettings.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(connectionString), "Must specify at least one TransportSettings instance");
            }

            if (!string.IsNullOrWhiteSpace(options?.ModelId)
                && transportSettings.Any(x => x.GetTransportType() == TransportType.Http1))
            {
                throw new InvalidOperationException("Plug and Play is not supported over the HTTP transport.");
            }

            var builder = IotHubConnectionStringBuilder.CreateWithIAuthenticationOverride(
                connectionString,
                authenticationMethod);

            // Clients that derive their authentication method from AuthenticationWithTokenRefresh will need to specify
            // the token time to live and renewal buffer values through the corresponding AuthenticationWithTokenRefresh
            // implementation constructors instead.
            if (!(builder.AuthenticationMethod is AuthenticationWithTokenRefresh))
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
                        if (!(transportSetting is AmqpTransportSettings))
                        {
                            throw new InvalidOperationException("Unknown implementation of ITransportSettings type");
                        }
                        break;

                    case TransportType.Http1:
                        if (!(transportSetting is Http1TransportSettings))
                        {
                            throw new InvalidOperationException("Unknown implementation of ITransportSettings type");
                        }
                        break;

                    case TransportType.Mqtt_WebSocket_Only:
                    case TransportType.Mqtt_Tcp_Only:
                        if (!(transportSetting is MqttTransportSettings))
                        {
                            throw new InvalidOperationException("Unknown implementation of ITransportSettings type");
                        }
                        break;

                    default:
                        throw new InvalidOperationException("Unsupported Transport Type {0}".FormatInvariant(transportSetting.GetTransportType()));
                }
            }

            if (authenticationMethod is DeviceAuthenticationWithX509Certificate
                && builder.Certificate == null)
            {
                throw new ArgumentException("No certificate was found. To use certificate authentication certificate must be present.");
            }

            // Make sure client options is initialized with the correct transport setting.
            EnsureOptionsIsSetup(builder.Certificate, transportSettings.FirstOrDefault()?.Proxy, ref options);

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
        private static void EnsureOptionsIsSetup(X509Certificate2 cert, IWebProxy proxy, ref ClientOptions options)
        {
            if (options == null)
            {
                options = new ClientOptions();
            }

            if (options.FileUploadTransportSettings == null)
            {
                options.FileUploadTransportSettings = new Http1TransportSettings();
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
            switch (transportType)
            {
                case TransportType.Amqp:
                    return new ITransportSettings[]
                    {
                        new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        },
                        new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    };

                case TransportType.Amqp_Tcp_Only:
                    return new ITransportSettings[]
                    {
                        new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    };

                case TransportType.Amqp_WebSocket_Only:
                    return new ITransportSettings[]
                    {
                        new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    };

                case TransportType.Http1:
                    return new ITransportSettings[]
                    {
                        new Http1TransportSettings()
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    };

                case TransportType.Mqtt:
                    return new ITransportSettings[]
                    {
                        new MqttTransportSettings(TransportType.Mqtt_Tcp_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        },
                        new MqttTransportSettings(TransportType.Mqtt_WebSocket_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    };

                case TransportType.Mqtt_Tcp_Only:
                    return new ITransportSettings[]
                    {
                        new MqttTransportSettings(TransportType.Mqtt_Tcp_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    };

                case TransportType.Mqtt_WebSocket_Only:
                    return new ITransportSettings[]
                    {
                        new MqttTransportSettings(TransportType.Mqtt_WebSocket_Only)
                        {
                            ClientCertificate = connectionStringBuilder.Certificate
                        }
                    };

                default:
                    throw new InvalidOperationException("Unsupported Transport {0}".FormatInvariant(transportType));
            }
        }

        private static ITransportSettings[] PopulateCertificateInTransportSettings(IotHubConnectionStringBuilder connectionStringBuilder, ITransportSettings[] transportSettings)
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
                        throw new InvalidOperationException("Unsupported Transport {0}".FormatInvariant(transportSetting.GetTransportType()));
                }
            }

            return transportSettings;
        }
    }
}
