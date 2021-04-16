// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public abstract class IPayloadConvention
    {
        /// <summary>
        ///
        /// </summary>
        public abstract ISerializer PayloadSerializer { get; }

        /// <summary>
        ///
        /// </summary>
        public abstract IContentEncoder PayloadEncoder { get; }

        /// <summary>
        /// Returns the byte array for the convention based message
        /// </summary>
        /// <param name="objectToSendWithConvention"></param>
        /// <returns></returns>
        public abstract byte[] GetObjectBytes(object objectToSendWithConvention);
    }
}
