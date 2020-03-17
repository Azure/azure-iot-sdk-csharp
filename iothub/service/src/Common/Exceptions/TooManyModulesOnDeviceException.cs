﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception thrown when the list of input modules is too large for an operation
    /// </summary>
    ///
    [Serializable]
    public sealed class TooManyModulesOnDeviceException : IotHubException
    {
        /// <summary>
        /// ctor which takes an error message
        /// </summary>
        /// <param name="message"></param>
        public TooManyModulesOnDeviceException(string message)
            : this(message, string.Empty)
        {
        }

        /// <summary>
        /// ctor which takes an error message and a tracking id
        /// </summary>
        /// <param name="message"></param>
        /// <param name="trackingId"></param>
        public TooManyModulesOnDeviceException(string message, string trackingId)
            : base(message, trackingId)
        {
        }

        /// <summary>
        /// ctor which takes an error message alongwith an inner exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public TooManyModulesOnDeviceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private TooManyModulesOnDeviceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
