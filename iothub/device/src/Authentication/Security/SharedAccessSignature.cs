// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed class SharedAccessSignature
    {
        internal SharedAccessSignature(DateTimeOffset expiresOn, string keyName, string signature, string encodedAudience)
        {
            ExpiresOnUtc = expiresOn;
            if (IsExpired())
            {
                throw new IotHubClientException($"The specified SAS token has already expired - on {expiresOn}.", IotHubClientErrorCode.Unauthorized);
            }

            KeyName = keyName ?? string.Empty;
            Signature = signature;
            Audience = WebUtility.UrlDecode(encodedAudience);
        }

        internal DateTimeOffset ExpiresOnUtc { get; }

        internal string KeyName { get; }

        internal string Audience { get; }

        internal string Signature { get; }

        internal bool IsExpired()
        {
            return ExpiresOnUtc + SharedAccessSignatureConstants.MaxClockSkew < DateTime.UtcNow;
        }
    }
}