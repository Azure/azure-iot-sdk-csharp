// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// contains Amqp transport-specific settings for DeviceClient
    /// </summary>
    public sealed class AmqpTransportSettings : ITransportSettings
    {
        private readonly TransportType _transportType;
        private TimeSpan _operationTimeout;
        private TimeSpan _openTimeout;

        /// <summary>
        /// Used by Edge runtime to specify an authentication chain for Edge-to-Edge connections
        /// </summary>
        internal string AuthenticationChain { get; set; }

        /// <summary>
        /// To enable certificate revocation check. Default to be false.
        /// </summary>
        [SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "Cannot change public property in public facing classes.")]
        public bool CertificateRevocationCheck
        {
            get => TlsVersions.Instance.CertificateRevocationCheck;
            set => TlsVersions.Instance.CertificateRevocationCheck = value;
        }

        /// <summary>
        /// The default operation timeout
        /// </summary>
        public static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The default open timeout
        /// </summary>
        public static readonly TimeSpan DefaultOpenTimeout = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The default idle timeout
        /// </summary>
        public static readonly TimeSpan DefaultIdleTimeout = TimeSpan.FromMinutes(2);

        /// <summary>
        /// The default pre-fetch count
        /// </summary>
        public const uint DefaultPrefetchCount = 50;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpTransportSettings" />
        /// </summary>
        /// <param name="transportType">The AMQP transport type.</param>
        public AmqpTransportSettings(TransportType transportType)
            : this(transportType, DefaultPrefetchCount, new AmqpConnectionPoolSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpTransportSettings" />
        /// </summary>
        /// <param name="transportType">The AMQP transport type.</param>
        /// <param name="prefetchCount">The pre-fetch count.</param>
        public AmqpTransportSettings(TransportType transportType, uint prefetchCount)
            : this(transportType, prefetchCount, new AmqpConnectionPoolSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpTransportSettings" />
        /// </summary>
        /// <param name="transportType">The AMQP transport type.</param>
        /// <param name="prefetchCount">The pre-fetch count.</param>
        /// <param name="amqpConnectionPoolSettings">AMQP connection pool settings.</param>
        public AmqpTransportSettings(TransportType transportType, uint prefetchCount, AmqpConnectionPoolSettings amqpConnectionPoolSettings)
        {
            OperationTimeout = DefaultOperationTimeout;
            OpenTimeout = DefaultOpenTimeout;
            IdleTimeout = DefaultIdleTimeout;

            PrefetchCount = prefetchCount <= 0
                ? throw new ArgumentOutOfRangeException(nameof(prefetchCount), "Must be greater than zero")
                : prefetchCount;

            switch (transportType)
            {
                case TransportType.Amqp_WebSocket_Only:
                    Proxy = DefaultWebProxySettings.Instance;
                    _transportType = transportType;
                    break;

                case TransportType.Amqp_Tcp_Only:
                    _transportType = transportType;
                    break;

                case TransportType.Amqp:
                    throw new ArgumentOutOfRangeException(nameof(transportType), transportType, "Must specify Amqp_WebSocket_Only or Amqp_Tcp_Only");

                default:
                    throw new ArgumentOutOfRangeException(nameof(transportType), transportType, null);
            }

            AmqpConnectionPoolSettings = amqpConnectionPoolSettings;
        }

        /// <summary>
        /// Specify client-side heartbeat interval.
        /// The interval, that the client establishes with the service, for sending keep alive pings.
        /// The default value is 2 minutes.
        /// </summary>
        /// <remarks>
        /// The client will consider the connection as disconnected if the keep alive ping fails.
        /// Setting a very low idle timeout value can cause aggressive reconnects, and might not give the
        /// client enough time to establish a connection before disconnecting and reconnecting.
        /// </remarks>
        public TimeSpan IdleTimeout { get; set; }

        /// <summary>
        /// The time to wait for any operation to complete. The default is 1 minute.
        /// </summary>
        public TimeSpan OperationTimeout
        {
            get => _operationTimeout;
            set => SetOperationTimeout(value);
        }

        /// <summary>
        /// The open timeout. The default is 1 minute.
        /// </summary>
        /// <remarks>
        /// This property is currently unused.
        /// </remarks>
        public TimeSpan OpenTimeout
        {
            get => _openTimeout;
            set => SetOpenTimeout(value);
        }

        /// <summary>
        /// The pre-fetch count
        /// </summary>
        public uint PrefetchCount { get; set; }

        /// <summary>
        /// The client certificate to use for authenticating
        /// </summary>
        public X509Certificate2 ClientCertificate { get; set; }

        /// <inheritdoc/>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// A callback for remote certificate validation.
        /// If incorrectly implemented, your device may fail to connect to IoTHub and/or be open to security vulnerabilities.
        /// </summary>
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        /// <summary>
        /// The connection pool settings for AMQP
        /// </summary>
        public AmqpConnectionPoolSettings AmqpConnectionPoolSettings { get; set; }

        /// <summary>
        /// Returns the configured transport type
        /// </summary>
        public TransportType GetTransportType()
        {
            return _transportType;
        }

        /// <summary>
        /// The time to wait for a receive operation. The default value is 1 minute.
        /// </summary>
        public TimeSpan DefaultReceiveTimeout => DefaultOperationTimeout;

        /// <summary>
        /// Compares the properties of this instance to another
        /// </summary>
        /// <param name="other">The other instance to compare to</param>
        /// <returns>True if equal</returns>
        public bool Equals(AmqpTransportSettings other)
        {
            return other == null
                ? false
                : ReferenceEquals(this, other)
                    // ClientCertificates are usually different, so ignore them in the comparison
                    || PrefetchCount == other.PrefetchCount
                        && OpenTimeout == other.OpenTimeout
                        && OperationTimeout == other.OperationTimeout
                        && AmqpConnectionPoolSettings.Equals(other.AmqpConnectionPoolSettings);
        }

        private void SetOperationTimeout(TimeSpan timeout)
        {
            _operationTimeout = timeout > TimeSpan.Zero
                ? timeout
                : throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        private void SetOpenTimeout(TimeSpan timeout)
        {
            _openTimeout = timeout > TimeSpan.Zero
                ? timeout
                : throw new ArgumentOutOfRangeException(nameof(timeout));
        }
    }
}
