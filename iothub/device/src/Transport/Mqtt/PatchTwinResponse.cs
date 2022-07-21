// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// Data class that contains the service's response to a patch twin request.
    /// </summary>
    internal class PatchTwinResponse
    {
        /// <summary>
        /// Construct a new instance of this class.
        /// </summary>
        /// <param name="status">The status the service responded with. 204 indicates a successful patch twin request.</param>
        /// <param name="version">The new version of the twin after the patch.</param>
        internal PatchTwinResponse(int status, int version)
        {
            Status = status;
            Version = version;
        }

        /// <summary>
        /// The status the service responded with. 204 indicates a successful patch twin request.
        /// </summary>
        internal int Status { get; set; }

        /// <summary>
        /// The new version of the twin after the patch.
        /// </summary>
        internal int Version { get; set; }
    }
}
