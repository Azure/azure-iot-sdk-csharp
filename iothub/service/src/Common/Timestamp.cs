// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.Common
{
    // This is meant to replace DateTime when it is primarily used to determine elapsed time. DateTime is vulnerable to clock jump when
    // system wall clock is reset. Stopwatch can be used in similar scenario but it is not optimized for memory foot-print.
    //
    // This class is immune to clock jump with the following two exceptions:
    //  - When multi-processor machine has a bug in BIOS/HAL that returns inconsistent clock tick for different processor.
    //  - When the machine does not support high frequency CPU tick.
    internal struct Timestamp : IComparable<Timestamp>, IEquatable<Timestamp>
    {
        private static readonly double s_tickFrequency = 10000000.0 / Stopwatch.Frequency;
        private readonly long _timestamp;

        public Timestamp(long timestamp)
        {
            _timestamp = timestamp;
        }

        private static long ConvertRawTicksToTicks(long rawTicks)
        {
            if (Stopwatch.IsHighResolution)
            {
                double elapsedTicks = rawTicks * s_tickFrequency;
                return (long)elapsedTicks;
            }

            return rawTicks;
        }

        public bool Equals(Timestamp other)
        {
            return _timestamp == other._timestamp;
        }

        public override int GetHashCode()
        {
            return _timestamp.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Timestamp)
            {
                return Equals((Timestamp)obj);
            }

            return false;
        }

        public int CompareTo(Timestamp other)
        {
            return _timestamp.CompareTo(other._timestamp);
        }
    }
}
