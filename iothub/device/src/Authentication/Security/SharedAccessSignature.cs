// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed class SharedAccessSignature
    {
        internal SharedAccessSignature(DateTime expiresOn, string keyName, string signature, string encodedAudience)
        {
            ExpiresOn = expiresOn;
            if (IsExpired())
            {
                throw new UnauthorizedAccessException($"The specified SAS token has already expired - on {expiresOn}.");
            }

            KeyName = keyName ?? string.Empty;
            Signature = signature;
            Audience = WebUtility.UrlDecode(encodedAudience);
        }

        internal DateTime ExpiresOn { get; }

        internal string KeyName { get; }

        internal string Audience { get; }

        internal string Signature { get; }

        internal bool IsExpired()
        {
            return ExpiresOn + SharedAccessSignatureConstants.MaxClockSkew < DateTime.UtcNow;
        }
    }
}