// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.Azure.Devices.Common.Security
{
    /// <summary>
    /// Performs security-related operations.
    /// </summary>
    public static class SecurityHelper
    {
        /// <summary>
        /// Validate the given IoT hub host name and IoT hub name.
        /// </summary>
        /// <param name="iotHubHostName">The IoT hub host name to validate.</param>
        /// <param name="iotHubName">The IoT hub name to validate.</param>
        public static void ValidateIotHubHostName(string iotHubHostName, string iotHubName)
        {
            if (string.IsNullOrWhiteSpace(iotHubHostName))
            {
                throw new ArgumentNullException(nameof(iotHubHostName));
            }

            if (string.IsNullOrWhiteSpace(iotHubName))
            {
                throw new ArgumentNullException(nameof(iotHubName));
            }

            if (!iotHubHostName.StartsWith(string.Format(CultureInfo.InvariantCulture, "{0}.", iotHubName), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("IOT hub does not correspond to host name");
            }
        }
    }
}
