// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Configuration for the AMQP IoTHub Transport.
    /// </summary>
    public sealed class AmqpConnectionPoolSettings
    {
        private static readonly TimeSpan DefaultConnectionIdleTimeout = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan MinConnectionIdleTimeout = TimeSpan.FromSeconds(5);
        private const uint DefaultPoolSize = 100;
        private const uint MaxNumberOfPools = ushort.MaxValue;

        private uint _maxPoolSize;
        private TimeSpan _connectionIdleTimeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmqpConnectionPoolSettings"/> class.
        /// </summary>
        public AmqpConnectionPoolSettings()
        {
            this._maxPoolSize = DefaultPoolSize;
            this.Pooling = false;
            this._connectionIdleTimeout = DefaultConnectionIdleTimeout;
        }

        /// <summary>
        /// Gets or sets the maximum size of the pool.
        /// </summary>
        /// <value>
        /// The maximum size of the pool.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">value</exception>
        public uint MaxPoolSize
        {
            get { return this._maxPoolSize; }

            set
            {
                if (value > 0 && value <= MaxNumberOfPools)
                {
                    this._maxPoolSize = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
            }
        }

        /// <summary>Gets or sets a value indicating whether this <see cref="AmqpConnectionPoolSettings"/> is pooling.</summary>
        /// <value>
        ///   <c>true</c> if pooling; otherwise, <c>false</c>.</value>
        public bool Pooling { get; set; }

        /// <summary>Gets or sets the connection idle timeout.</summary>
        /// <value>The connection idle timeout.</value>
        /// <exception cref="ArgumentOutOfRangeException">value</exception>
        public TimeSpan ConnectionIdleTimeout
        {
            get { return this._connectionIdleTimeout; }

            set
            {
                if (value >= MinConnectionIdleTimeout)
                {
                    this._connectionIdleTimeout = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("value");
                }
            }
        }

        /// <summary>Equalses the specified other.</summary>
        /// <param name="other">The other.</param>
        /// <returns></returns>
        public bool Equals(AmqpConnectionPoolSettings other)
        {
            if (other == null)
            {
                return false;
            }

            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            return (this.Pooling == other.Pooling && this.MaxPoolSize == other.MaxPoolSize && this.ConnectionIdleTimeout == other.ConnectionIdleTimeout);
        }
    }
}
