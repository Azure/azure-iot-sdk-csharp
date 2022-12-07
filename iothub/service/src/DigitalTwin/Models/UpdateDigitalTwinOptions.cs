// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// General request options that are applicable, but optional, for update digital twin operations.
    /// </summary>
    public class UpdateDigitalTwinOptions
    {
        /// <summary>
        /// A string representing a weak ETag for the entity that this request performs an operation against, as per RFC7232.
        /// </summary>
        /// <remarks>
        /// The request's operation is performed only if this ETag matches the value maintained by the server,
        /// indicating that the entity has not been modified since it was last retrieved.
        /// <para>
        /// To perform the operation unconditionally, let it retain the default of <see cref="ETag.All"/>.
        /// </para>
        /// <para>
        /// For more information about this property, see <see href="https://tools.ietf.org/html/rfc7232#section-3.2">RFC 7232</see>.
        /// </para>
        /// </remarks>
        public ETag IfMatch { get; set; } = ETag.All;
    }
}
