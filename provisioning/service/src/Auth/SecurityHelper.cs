// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.Azure.Devices.Common.Service.Auth
{
    internal static class SecurityHelper
    {
        public static void ValidateServiceHostName(string serviceHostName, string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceHostName))
            {
                throw new ArgumentNullException(nameof(serviceHostName));
            }

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            if (!serviceHostName.StartsWith(string.Format(CultureInfo.InvariantCulture, "{0}.", serviceName), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Service does not correspond to host name");
            }
        }
    }
}
