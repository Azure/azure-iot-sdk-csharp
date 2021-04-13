// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public interface IPayloadConvention
    {
        /// <summary>
        ///
        /// </summary>
        public ISerializer PayloadSerializer { get; }

        /// <summary>
        ///
        /// </summary>
        public IContentEncoder PayloadEncoder { get; }

        /// <summary>
        /// Returns the byte array for the convention based message
        /// </summary>
        /// <param name="objectToSendWithConvention"></param>
        /// <returns></returns>
        public byte[] GetObjectBytes(object objectToSendWithConvention);
    }
}
