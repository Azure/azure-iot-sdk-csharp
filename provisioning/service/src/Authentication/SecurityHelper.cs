// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.Azure.Devices
{
    internal static class SecurityHelper
    {
        public static void ValidateServiceHostName(string serviceHostName, string serviceName)
        {
            if (!serviceHostName.StartsWith(serviceName.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Service does not correspond to host name");
            }
        }
    }
}
