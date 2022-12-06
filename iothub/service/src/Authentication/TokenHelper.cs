// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices
{
    internal static class TokenHelper
    {
        /// <summary>
        /// Determines if the given token expiry date time is close to expiry. The date and time is
        /// considered close to expiry if it has less than 10 minutes relative to the current time.
        /// </summary>
        /// <param name="expiry">The token expiration date and time.</param>
        /// <param name="buffer">The default buffer to determnine if we're "near" expiry. Defaults to 10 minutes.</param>
        /// <returns>True if the token expiry has less than 10 minutes relative to the current time, otherwise false.</returns>
        internal static bool IsCloseToExpiry(DateTimeOffset expiry, TimeSpan? buffer = default)
        {
            buffer ??= TimeSpan.FromMinutes(10);
            TimeSpan timeToExpiry = expiry - DateTimeOffset.UtcNow;
            return timeToExpiry < buffer;
        }
    }
}
