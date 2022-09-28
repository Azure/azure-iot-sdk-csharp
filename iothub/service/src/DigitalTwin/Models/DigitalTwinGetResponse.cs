// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The service response to a get digital twin request.
    /// </summary>
    /// <typeparam name="T">The type of the digital twin.</typeparam>
    public class DigitalTwinGetResponse<T>
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <remarks>
        /// This class can be inherited from and set by unit tests for mocking purposes.
        /// </remarks>
        /// <param name="eTag">Weak ETag of the modified resource.</param>
        /// <param name="digitalTwin">The deserialized digital twin.</param>
        protected internal DigitalTwinGetResponse(T digitalTwin, ETag eTag = default)
        {
            DigitalTwin = digitalTwin;
            ETag = eTag;
        }

        /// <summary>
        /// Gets the deserialized digital twin.
        /// </summary>
        public T DigitalTwin { get; protected internal set; }

        /// <summary>
        /// Gets the weak ETag of the modified resource.
        /// </summary>
        public ETag ETag { get; protected internal set; }
    }
}
