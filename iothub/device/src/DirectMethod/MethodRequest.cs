// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The data structure that represents a method request.
    /// </summary>
    public sealed class MethodRequest
    {
        /// <summary>
        /// Creates an instance of this class with without any method data
        /// and extra time for device to connect and send a response.
        /// </summary>
        /// <param name="name">The method name.</param>
        public MethodRequest(string name) : this(name, null, null, null)
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="name">The method name.</param>
        /// <param name="data">The method data.</param>
        public MethodRequest(string name, byte[] data) : this(name, data, null, null)
        {
        }

        /// <summary>
        /// Creates an instance of this class without any method data.
        /// </summary>
        /// <param name="name">The method name.</param>
        /// <param name="responseTimeout">The method timeout value.</param>
        /// <param name="connectionTimeout">The device connection timeout value.</param>
        public MethodRequest(string name, TimeSpan? responseTimeout, TimeSpan? connectionTimeout)
            : this(name, null, responseTimeout, connectionTimeout)
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="name">The method name.</param>
        /// <param name="data">The method data.</param>
        /// <param name="responseTimeout">The method timeout value.</param>
        /// <param name="connectionTimeout">The device connection timeout value.</param>
        public MethodRequest(string name, byte[] data, TimeSpan? responseTimeout, TimeSpan? connectionTimeout)
        {
            Name = name;
            Data = data;
            ResponseTimeout = responseTimeout;
            ConnectionTimeout = connectionTimeout;
        }

        /// <summary>
        /// The method name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The method data.
        /// </summary>
        [SuppressMessage(
            "Performance",
            "CA1819:Properties should not return arrays",
            Justification = "Cannot change property types on public classes.")]
        public byte[] Data { get; private set; }

        /// <summary>
        /// The method response timeout value. This value is the amount of time that IoT hub service must await for
        /// completion of a direct method execution on a device. The minimum and maximum values are 5 and 300 seconds.
        /// Note: This value is relevant only when invoking methods from one edge module to another.
        /// </summary>
        public TimeSpan? ResponseTimeout { get; private set; }

        /// <summary>
        /// The device connection timeout value. This value is the amount of time upon invocation of a direct method that
        /// IoT hub service must await for a disconnected device to come online.
        /// The default value is 0, meaning that devices must already be online upon invocation of a direct method.
        /// The maximum value for connectTimeoutInSeconds is 300 seconds.
        /// Note: This value is relevant only when invoking methods from one edge module to another.
        /// </summary>
        public TimeSpan? ConnectionTimeout { get; private set; }

        /// <summary>
        /// The method data in Json format.
        /// </summary>
        public string DataAsJson => (Data == null || Data.Length == 0)
            ? null
            : Encoding.UTF8.GetString(Data);
    }
}
