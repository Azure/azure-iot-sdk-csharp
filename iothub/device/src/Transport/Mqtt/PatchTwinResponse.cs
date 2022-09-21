// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// The service's response to a patch twin request.
    /// </summary>
    internal class PatchTwinResponse
    {
        /// <summary>
        /// The status the service responded with.
        /// </summary>
        /// <remarks>
        /// 204 indicates a successful patch twin request.
        /// </remarks>
        internal int Status { get; set; }

        /// <summary>
        /// The new version of the twin after the patch.
        /// </summary>
        internal long Version { get; set; }

        /// <summary>
        /// The error message if the request failed.
        /// </summary>
        internal string Message { get; set; }
    }
}
