// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Azure.Devices
{
    internal static class SecurityHelper
    {
        internal static void ValidateServiceHostName(string serviceHostName, string serviceName)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(serviceHostName));
            Debug.Assert(!string.IsNullOrWhiteSpace(serviceName));
            if (!serviceHostName.StartsWith(serviceName.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Service does not correspond to host name");
            }
        }
    }
}
