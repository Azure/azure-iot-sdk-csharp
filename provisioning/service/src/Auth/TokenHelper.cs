// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Microsoft.Azure.Devices.Provisioning.Service.Auth
{
    internal static class TokenHelper
    {
        /// <summary>
        /// Determines if the given token expiry date time is close to expiry. The date and time is
        /// considered close to expiry if it has less than 10 minutes relative to the current time.
        /// </summary>
        /// <param name="expiry">The token expiration date and time.</param>
        /// <returns>True if the token expiry has less than 10 minutes relative to the current time, otherwise false.</returns>
        public static bool IsCloseToExpiry(DateTimeOffset expiry)
        {
            TimeSpan timeToExpiry = expiry - DateTimeOffset.UtcNow;
            return timeToExpiry.TotalMinutes < 10;
        }
    }
}
