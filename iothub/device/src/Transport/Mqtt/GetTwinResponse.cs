// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// The service's response to a get twin request.
    /// </summary>
    /// <remarks>
    /// May not contain a twin object if the service did not respond with a twin.
    /// </remarks>
    internal class GetTwinResponse
    {
        /// <summary>
        /// The status code that the service responded to the get twin request with.
        /// </summary>
        /// <remarks>
        /// 200 indicates a successful request.
        /// </remarks>
        internal int Status { get; set; }

        /// <summary>
        /// The twin that the service responded to the get twin request with.
        /// </summary>
        /// <remarks>
        /// This value is null if the get twin request failed.
        /// </remarks>
        internal Twin Twin { get; set; }
    }
}
