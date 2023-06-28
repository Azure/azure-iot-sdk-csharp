﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Parameters to execute a direct method on a device or module.
    /// </summary>
    public class DirectMethodServiceRequest
    {
        /// <summary>
        /// Initialize an instance of this class.
        /// </summary>
        /// <param name="methodName">The method name to run.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="methodName"/> is null.</exception>
        /// <exception cref="ArgumentException">When <paramref name="methodName"/> is empty or white space.</exception>
        public DirectMethodServiceRequest(string methodName)
        {
            Argument.AssertNotNullOrWhiteSpace(methodName, nameof(methodName));
            MethodName= methodName;
        }

        /// <summary>
        /// The method name to run.
        /// </summary>
        [JsonProperty("methodName")]
        public string MethodName { get; }

        /// <summary>
        /// The payload to have serialized and send as JSON.
        /// </summary>
        [JsonProperty("payload")]
        public byte[] Payload { get; set; }

        /// <summary>
        /// The amount of time given to the service to connect to the device.
        /// </summary>
        /// <remarks>
        /// A timeout may occur if this value is set to zero and the target device is not connected to
        /// the cloud.
        /// If the value is greater than zero, it may also occur if the cloud fails to deliver the request to
        /// the target device.
        /// <para>
        /// This value is propagated to the service in terms of seconds, so this value does not have a level of
        /// precision below seconds. For example, a value of <c>TimeSpan.FromMilliseconds(500)</c> will be
        /// interpreted as 0 seconds (using <c>ConnectTimeout.TotalSeconds</c>).
        /// </para>
        /// </remarks>
        [JsonIgnore]
        public TimeSpan? ConnectionTimeout { get; set; }

        /// <summary>
        /// The amount of time given to the device to process and respond to the command request.
        /// </summary>
        /// <remarks>
        /// This timeout may happen if the target device is slow in handling the direct method.
        /// <para>
        /// This value is propagated to the service in terms of seconds, so this value does not have a level of
        /// precision below seconds. For example, setting this value to TimeSpan.FromMilliseconds(500) will result
        /// in this request having a timeout of 0 seconds.
        /// </para>
        /// </remarks>
        [JsonIgnore]
        public TimeSpan? ResponseTimeout { get; set; }

        [JsonProperty("responseTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        internal int? ResponseTimeoutInSeconds => (int?)ResponseTimeout?.TotalSeconds ?? null;

        [JsonProperty("connectTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        internal int? ConnectionTimeoutInSeconds => (int?)ConnectionTimeout?.TotalSeconds ?? null;
    }
}
