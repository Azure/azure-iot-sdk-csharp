// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    internal enum PropertyValueType
    {
        Null,
        Byte, SByte, Char, Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double, Decimal, // Numeric types
        Boolean, Guid, String, Uri, DateTime, DateTimeOffset, TimeSpan,
        Stream,
        Unknown,
    }
}
