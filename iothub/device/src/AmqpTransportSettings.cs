﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using Microsoft.Azure.Devices.Shared;
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// contains Amqp transport-specific settings for DeviceClient
    /// </summary>
    public sealed class AmqpTransportSettings : ITransportSettings
    {
        private TimeSpan _idleTimeout;
        static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);
        static readonly TimeSpan DefaultOpenTimeout = TimeSpan.FromMinutes(1);
        const uint DefaultPrefetchCount = 50;

        readonly TransportType transportType;
        TimeSpan operationTimeout;
        TimeSpan openTimeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpTransportSettings" class./>
        /// </summary>
        /// <param name="transportType">The AMQP transport type.</param>
        public AmqpTransportSettings(TransportType transportType)
            : this(transportType, DefaultPrefetchCount, new AmqpConnectionPoolSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpTransportSettings" class./>
        /// </summary>
        /// <param name="transportType">The AMQP transport type.</param>
        public AmqpTransportSettings(TransportType transportType, uint prefetchCount)
            :this(transportType, prefetchCount, new AmqpConnectionPoolSettings())
        {
        }

        public AmqpTransportSettings(TransportType transportType, uint prefetchCount, AmqpConnectionPoolSettings amqpConnectionPoolSettings)
        {
            this.operationTimeout = DefaultOperationTimeout;
            this.openTimeout = DefaultOpenTimeout;

            if (prefetchCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(prefetchCount), "Must be greater than zero");
            }

            switch (transportType)
            {
                case TransportType.Amqp_WebSocket_Only:
                    this.Proxy = DefaultWebProxySettings.Instance;
                    this.transportType = transportType;
                    break;
                case TransportType.Amqp_Tcp_Only:
                    this.transportType = transportType;
                    break;
                case TransportType.Amqp:
                    throw new ArgumentOutOfRangeException(nameof(transportType), transportType, "Must specify Amqp_WebSocket_Only or Amqp_Tcp_Only");
                default:
                    throw new ArgumentOutOfRangeException(nameof(transportType), transportType, null);
            }

            this.PrefetchCount = prefetchCount;
            this.AmqpConnectionPoolSettings = amqpConnectionPoolSettings;
        }

        public TransportType GetTransportType()
        {
            return this.transportType;
        }

        public TimeSpan DefaultReceiveTimeout => this.operationTimeout;

        public TimeSpan OperationTimeout {
            get { return this.operationTimeout; }
            set { this.SetOperationTimeout(value); }
        }

        public TimeSpan OpenTimeout
        {
            get { return this.openTimeout; }
            set { this.SetOpenTimeout(value); }
        }

        public uint PrefetchCount { get; set; }

        public X509Certificate2 ClientCertificate { get; set; }

        public IWebProxy Proxy { get; set; }

        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        public AmqpConnectionPoolSettings AmqpConnectionPoolSettings { get; set; }

        /// <summary>
        /// Specify client side heart beat interval.
        /// </summary>
        public TimeSpan IdleTimeout { get => _idleTimeout; set => _idleTimeout = value; }

        public bool Equals(AmqpTransportSettings other)
        {
            if (other == null)
            {
                return false;
            }

            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            // ClientCertificates are usually different
            return (this.PrefetchCount == other.PrefetchCount && this.OpenTimeout == other.OpenTimeout && this.OperationTimeout == other.OperationTimeout && this.AmqpConnectionPoolSettings.Equals(other.AmqpConnectionPoolSettings));
        }

        void SetOperationTimeout(TimeSpan timeout)
        {
            if (timeout > TimeSpan.Zero)
            {
                this.operationTimeout = timeout;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }
        }

        void SetOpenTimeout(TimeSpan timeout)
        {
            if (timeout > TimeSpan.Zero)
            {
                this.openTimeout = timeout;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }
        }
    }
}
