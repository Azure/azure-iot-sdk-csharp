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
        /// <remarks>
        /// This class can be inherited from and set by unit tests for mocking purposes.
        /// </remarks>
        /// <param name="eTag">Weak ETag of the modified resource.</param>
        /// <param name="location">URI of the digital twin.</param>
        protected internal DigitalTwinUpdateResponse(ETag eTag = default, string location = default)
        {
            ETag = eTag;
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
        /// Marked internal as it was added to the service for completeness with guidance and there is no known user use case.
        /// </summary>
        internal string Location { get; }
    }
}
