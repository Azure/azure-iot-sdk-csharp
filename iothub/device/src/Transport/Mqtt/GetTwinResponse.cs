// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// Data class that contains the service's response to a get twin request. May not contain a twin object if
    /// the service did not respond with a twin.
    /// </summary>
    internal class GetTwinResponse
    {
        /// <summary>
        /// Constructor with no twin. Used when the get twin request fails and the twin could not be retrieved.
        /// </summary>
        /// <param name="status">
        /// The status code the service responded with. Should be a value other than 200 since the service only
        /// returns 200 if the request was successful and the twin was retrieved.
        /// </param>
        internal GetTwinResponse(int status)
        {
            Status = status;
        }

        /// <summary>
        /// Constructor with the retrieved twin. Used when the get twin request succeeds and the twin was retrieved.
        /// </summary>
        /// <param name="status">The status code the service responded with. Should be 200 in this case.</param>
        /// <param name="twin">The twin returned by the service. Should not be null in this case.</param>
        internal GetTwinResponse(int status, Twin twin)
        {
            Status = status;
            Twin = twin;
        }

        /// <summary>
        /// The status code that the service responded to the get twin request with. 200 indicates a successful request.
        /// </summary>
        internal int Status { get; set; }

        /// <summary>
        /// The twin that the service responded to the get twin request with. This value is null if the get twin request failed.
        /// </summary>
        internal Twin Twin { get; set; }
    }
}
