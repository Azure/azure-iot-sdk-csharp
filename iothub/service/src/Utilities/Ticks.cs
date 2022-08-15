// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Common
{
    internal static class Ticks
    {
        public static int ToMilliseconds(long ticks)
        {
            return checked((int)(ticks / TimeSpan.TicksPerMillisecond));
        }

        public static long FromTimeSpan(TimeSpan duration)
        {
            return duration.Ticks;
        }
    }
}
