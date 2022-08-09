// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// General request options that are applicable, but optional, for invoke command APIs.
    /// </summary>
    public class InvokeDigitalTwinCommandOptions
    {
        /// <summary>
        /// The serialized command payload.
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// The timeout before the command request will fail if the request takes too long to reach the device.
        /// This timeout may happen if the target device is not connected to the cloud or if the cloud fails to deliver
        /// the request to the target device in time. If this value is set to 0 seconds, then the target device must be online
        /// when this command request is made.
        /// </summary>
        /// <remarks>
        /// This value is propagated to the service in terms of seconds, so this value does not have a level of
        /// precision below seconds. For example, setting this value to TimeSpan.FromMilliseconds(500) will result
        /// in this request having a timeout of 0 seconds.
        /// </remarks>
        public TimeSpan? ConnectTimeout { get; set; }

        /// <summary>
        /// The timeout before the command request will fail if the device doesn't respond to the request.
        /// This timeout may happen if the target device is slow in handling the direct method.
        /// </summary>
        /// <remarks>
        /// This value is propagated to the service in terms of seconds, so this value does not have a level of
        /// precision below seconds. For example, setting this value to TimeSpan.FromMilliseconds(500) will result
        /// in this request having a timeout of 0 seconds.
        /// </remarks>
        public TimeSpan? ResponseTimeout { get; set; }
    }
}
