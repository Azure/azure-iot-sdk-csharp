// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if ENABLE_MODULES_SDK
namespace Microsoft.Azure.Devices.Common.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception thrown when the list of input modules is too large for an operation 
    /// </summary>
    /// 
#if !WINDOWS_UWP
    [Serializable]
#endif
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

#if !WINDOWS_UWP && !NETSTANDARD1_3
        TooManyModulesOnDeviceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
#endif