// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Azure.Devices
{
    internal static class SecurityHelper
    {
        internal static void ValidateServiceHostName(string serviceHostName, string shareAccessSignatureName)
        {
            if (!serviceHostName.StartsWith(shareAccessSignatureName.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Service does not correspond to host name");
            }
        }
    }
}
