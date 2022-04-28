// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The interface that defines the structure of a writable property response.
    /// </summary>
    /// <remarks>
    /// This interface is used to allow extension to use a different set of attributes for serialization.
    /// For example our default implementation found in <see cref="NewtonsoftJsonWritablePropertyResponse"/> is based on <see cref="Newtonsoft.Json"/> serializer attributes.
    /// </remarks>
    public interface IWritablePropertyResponse
    {
        /// <summary>
        /// The unserialized property value.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// The acknowledgment code, usually an HTTP Status Code e.g. 200, 400.
        /// </summary>
        public int AckCode { get; set; }

        /// <summary>
        /// The acknowledgment version, as supplied in the property update request.
        /// </summary>
        public long AckVersion { get; set; }

        /// <summary>
        /// The acknowledgment description, an optional, human-readable message about the result of the property update.
        /// </summary>
        public string AckDescription { get; set; }
    }
}
