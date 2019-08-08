// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>Contains advanced AMQP transport settings</summary>
    /// <remarks>
    ///   <para>
    /// We recommend that default settings are used in most applications.</para>
    ///   <para>If more granular control is required, advanced users can tweak this options with the following implications:</para>
    ///   <list type="bullet">
    ///     <item>Small timeouts (&lt;30 seconds) potentially consuming more CPU/battery as we will spin-wait on ReceiveAsync.</item>
    ///     <item>Small timeouts can cause inconsistent states for messages being sent or received.</item>
    ///     <item>
    /// There are cases where some of the timeouts out can cause the AMQP stack to become unstable/unusable which could cause a disconnect or data loss.
    /// </item>
    ///   </list>
    /// </remarks>
    public sealed class AmqpTransportSettings : ITransportSettings
    {
        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan s_defaultOpenTimeout = TimeSpan.FromMinutes(1);
        private const uint DefaultPrefetchCount = 50;

        private readonly TransportType _transportType;
        private TimeSpan _operationTimeout;
        private TimeSpan _openTimeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpTransportSettings"/> class./>
        /// </summary>
        /// <param name="transportType">The AMQP transport type.</param>
        public AmqpTransportSettings(TransportType transportType)
            : this(transportType, DefaultPrefetchCount, new AmqpConnectionPoolSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpTransportSettings"/> class.
        /// </summary>
        /// <param name="transportType">Type of the transport.</param>
        /// <param name="prefetchCount">The prefetch count (Total AMQP Link credit).</param>
        public AmqpTransportSettings(TransportType transportType, uint prefetchCount)
            : this(transportType, prefetchCount, new AmqpConnectionPoolSettings())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="AmqpTransportSettings"/> class.</summary>
        /// <param name="transportType">Type of the transport.</param>
        /// <param name="prefetchCount">The prefetch count (Total AMQP Link credit).</param>
        /// <param name="amqpConnectionPoolSettings">The amqp connection pool settings.</param>
        /// <exception cref="ArgumentOutOfRangeException">prefetchCount - Must be greater than zero
        /// or
        /// transportType - Must specify Amqp_WebSocket_Only or Amqp_Tcp_Only
        /// or
        /// transportType - null</exception>
        public AmqpTransportSettings(TransportType transportType, uint prefetchCount, AmqpConnectionPoolSettings amqpConnectionPoolSettings)
        {
            this._operationTimeout = s_defaultOperationTimeout;
            this._openTimeout = s_defaultOpenTimeout;

            if (prefetchCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(prefetchCount), "Must be greater than zero");
            }

            switch (transportType)
            {
                case TransportType.Amqp_WebSocket_Only:
                    this.Proxy = DefaultWebProxySettings.Instance;
                    this._transportType = transportType;
                    break;
                case TransportType.Amqp_Tcp_Only:
                    this._transportType = transportType;
                    break;
                case TransportType.Amqp:
                    throw new ArgumentOutOfRangeException(nameof(transportType), transportType, "Must specify Amqp_WebSocket_Only or Amqp_Tcp_Only");
                default:
                    throw new ArgumentOutOfRangeException(nameof(transportType), transportType, null);
            }

            this.PrefetchCount = prefetchCount;
            this.AmqpConnectionPoolSettings = amqpConnectionPoolSettings;
        }

        /// <summary>
        /// Returns the transport type of the TransportSettings object.
        /// </summary>
        /// <returns>
        /// The TransportType
        /// </returns>
        public TransportType GetTransportType()
        {
            return this._transportType;
        }

        /// <summary>
        /// The default receive timeout.
        /// </summary>
        public TimeSpan DefaultReceiveTimeout => this._operationTimeout;

        /// <summary>
        /// Gets or sets the operation timeout.
        /// </summary>
        /// <value>
        /// The operation timeout.
        /// </value>
        public TimeSpan OperationTimeout
        {
            get { return this._operationTimeout; }
            set { this.SetOperationTimeout(value); }
        }

        /// <summary>
        /// Gets or sets the open timeout.
        /// </summary>
        /// <value>
        /// The open timeout.
        /// </value>
        public TimeSpan OpenTimeout
        {
            get { return this._openTimeout; }
            set { this.SetOpenTimeout(value); }
        }

        /// <summary>
        /// Gets or sets the prefetch count. This is passed to the AMQP library as total link credit.
        /// </summary>
        /// <value>
        /// The prefetch count.
        /// </value>
        public uint PrefetchCount { get; set; }

        /// <summary>
        /// Gets or sets the client certificate.
        /// </summary>
        /// <value>
        /// The client certificate.
        /// </value>
        public X509Certificate2 ClientCertificate { get; set; }

        /// <summary>
        /// Gets or sets the proxy.
        /// </summary>
        /// <value>
        /// The proxy.
        /// </value>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// Gets or sets the remote certificate validation callback.
        /// </summary>
        /// <value>
        /// The remote certificate validation callback.
        /// </value>
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        /// <summary>
        /// Gets or sets the AMQP connection pool settings.
        /// </summary>
        /// <value>
        /// The AMQP connection pool settings.
        /// </value>
        public AmqpConnectionPoolSettings AmqpConnectionPoolSettings { get; set; }

        /// <summary>
        /// Determines whether two objects have the same value.
        /// </summary>
        /// <param name="other">The other object.</param>
        /// <returns>true if the object are equal</returns>
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

        private void SetOperationTimeout(TimeSpan timeout)
        {
            if (timeout > TimeSpan.Zero)
            {
                this._operationTimeout = timeout;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }
        }

        private void SetOpenTimeout(TimeSpan timeout)
        {
            if (timeout > TimeSpan.Zero)
            {
                this._openTimeout = timeout;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }
        }
    }
}
