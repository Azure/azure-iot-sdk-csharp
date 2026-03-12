// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices
{
    internal static class SharedAccessSignatureConstants
    {
        internal const int MaxKeyNameLength = 256;
        internal const int MaxKeyLength = 256;
        internal const string SharedAccessSignature = "SharedAccessSignature";
        internal const string AudienceFieldName = "sr";
        internal const string SignatureFieldName = "sig";
        internal const string KeyNameFieldName = "skn";
        internal const string ExpiryFieldName = "se";
        internal const string SignedResourceFullFieldName = SharedAccessSignature + " " + AudienceFieldName;
        internal const string KeyValueSeparator = "=";
        internal const string PairSeparator = "&";
        internal static readonly DateTime EpochTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        internal static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);
    }
}
