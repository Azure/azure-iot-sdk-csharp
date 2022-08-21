// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    internal static class SharedAccessSignatureConstants
    {
        public const int MaxKeyNameLength = 256;
        public const int MaxKeyLength = 256;
        public const string SharedAccessSignature = "SharedAccessSignature";
        public const string AudienceFieldName = "sr";
        public const string SignatureFieldName = "sig";
        public const string KeyNameFieldName = "skn";
        public const string ExpiryFieldName = "se";
        public const string SignedResourceFullFieldName = SharedAccessSignature + " " + AudienceFieldName;
        public const char KeyValueSeparator = '=';
        public const char PairSeparator = '&';

        public const int DefaultSasRenewalBufferPercentage = 15;
        public static readonly TimeSpan DefaultSasTimeToLive = TimeSpan.FromHours(1);

        public static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        public static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);
    }
}
