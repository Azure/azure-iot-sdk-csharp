// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains AMQP connection pool settings for DeviceClient.
    /// </summary>
    public sealed class AmqpConnectionPoolSettings
    {
        private uint _maxPoolSize;
        // IotHub allows up to 999 tokens per connection. Setting the threshold just below that.
        internal const uint MaxDevicesPerConnection = 995;

        /// <summary>
        /// The default size of the pool.
        /// </summary>
        /// <remarks>
        /// Allows up to 100,000 devices.
        /// </remarks>
        private const uint DefaultPoolSize = 100;

        /// <summary>
        /// The maximum value that can be used for the MaxPoolSize property.
        /// </summary>
        public const uint AbsoluteMaxPoolSize = ushort.MaxValue;

        /// <summary>
        /// Creates an instance of AmqpConnecitonPoolSettings with default properties.
        /// </summary>
        public AmqpConnectionPoolSettings()
        {
            _maxPoolSize = DefaultPoolSize;
            Pooling = false;
        }

        /// <summary>
        /// The maximum pool size.
        /// </summary>
        public uint MaxPoolSize
        {
            get => _maxPoolSize;

            set => _maxPoolSize = value > 0 && value <= AbsoluteMaxPoolSize
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>
        /// Whether or not to use connection pooling
        /// </summary>
        public bool Pooling { get; set; }

        /// <summary>
        /// Compares the properties of this instance to another's.
        /// </summary>
        /// <param name="other">The other instance to compare to.</param>
        /// <returns>True, if equal.</returns>
        public bool Equals(AmqpConnectionPoolSettings other)
        {
            if (other == null)
            {
                return false;
            }

            return ReferenceEquals(this, other)
                ? true
                : Pooling == other.Pooling
                    && MaxPoolSize == other.MaxPoolSize;
        }
    }
}
