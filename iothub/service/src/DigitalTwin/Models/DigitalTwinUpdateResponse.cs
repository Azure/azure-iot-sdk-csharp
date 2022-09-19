// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The service response to a digital twin update operation.
    /// </summary>
    public class DigitalTwinUpdateResponse
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="eTag">Weak ETag of the modified resource.</param>
        /// <param name="location">URI of the digital twin.</param>
        internal DigitalTwinUpdateResponse(string eTag = default, string location = default)
        {
            ETag = new ETag(eTag);
            Location = location;
        }

        /// <summary>
        /// Gets the weak ETag of the modified resource.
        /// </summary>
        /// <seealso href="https://www.rfc-editor.org/rfc/rfc7232#section-3.2" />
        /// <seealso href="https://www.rfc-editor.org/rfc/rfc7232#section-2.1" />
        public ETag ETag { get; }

        /// <summary>
        /// Gets the URI of the digital twin.
        /// </summary>
        public string Location { get; }
    }
}
