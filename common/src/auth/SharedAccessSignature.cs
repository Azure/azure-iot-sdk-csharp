// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Net;

namespace Microsoft.Azure.Devices.Common.Authorization
{
    public class SharedAccessSignature
    {
        public SharedAccessSignature(string sharedAccessSignatureName, DateTime expiresOn, string expiry, string keyName, string signature, string encodedAudience)
        {
            if (string.IsNullOrWhiteSpace(sharedAccessSignatureName))
            {
                throw new ArgumentNullException(nameof(sharedAccessSignatureName));
            }

            ExpiresOnUtc = expiresOn;

            if (IsExpired())
            {
                throw new UnauthorizedAccessException("The specified SAS token is expired");
            }
        }

        public static readonly string[] KVSeparatorArray = new string[] { SharedAccessSignatureConstants.KeyValueSeparator };
        public static readonly string[] PairSeparatorArray = new string[] { SharedAccessSignatureConstants.PairSeparator };

    public DateTime ExpiresOnUtc { get; }

        public static SharedAccessSignature Parse(string shareAccessSignatureName, string rawToken)
        {
            if (string.IsNullOrWhiteSpace(shareAccessSignatureName))
            {
                throw new ArgumentNullException(nameof(shareAccessSignatureName));
            }

            if (string.IsNullOrWhiteSpace(rawToken))
            {
                throw new ArgumentNullException(nameof(rawToken));
            }

            IDictionary<string, string> parsedFields = ExtractFieldValues(rawToken);

            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.SignatureFieldName, out string signature))
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Missing field: {0}", SharedAccessSignatureConstants.SignatureFieldName));
            }

            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.ExpiryFieldName, out string expiry))
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Missing field: {0}", SharedAccessSignatureConstants.ExpiryFieldName));
            }

            // KeyName (skn) is optional .
            parsedFields.TryGetValue(SharedAccessSignatureConstants.KeyNameFieldName, out string keyName);

            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.AudienceFieldName, out string encodedAudience))
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Missing field: {0}", SharedAccessSignatureConstants.AudienceFieldName));
            }

            return new SharedAccessSignature(
                shareAccessSignatureName,
                SharedAccessSignatureConstants.EpochTime + TimeSpan.FromSeconds(double.Parse(expiry, CultureInfo.InvariantCulture)),
                expiry, keyName, signature, encodedAudience);
        }

        public static bool IsSharedAccessSignature(string rawSignature)
        {
            if (string.IsNullOrWhiteSpace(rawSignature))
            {
                return false;
            }

            IDictionary<string, string> parsedFields = ExtractFieldValues(rawSignature);
            string signature;
            bool isSharedAccessSignature = parsedFields.TryGetValue(SharedAccessSignatureConstants.SignatureFieldName, out signature);

            return isSharedAccessSignature;
        }

        public bool IsExpired()
        {
            return ExpiresOnUtc + SharedAccessSignatureConstants.MaxClockSkew < DateTime.UtcNow;
        }

        protected static IDictionary<string, string> ExtractFieldValues(string sharedAccessSignature)
        {
            string[] lines = sharedAccessSignature.Split();

            if (!string.Equals(lines[0].Trim(), SharedAccessSignatureConstants.SharedAccessSignature, StringComparison.Ordinal) || lines.Length != 2)
            {
                throw new FormatException("Malformed signature");
            }

            IDictionary<string, string> parsedFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] fields = lines[1].Trim().Split(PairSeparatorArray, StringSplitOptions.None);

            foreach (string field in fields)
            {
                if (!string.IsNullOrEmpty(field))
                {
                    string[] fieldParts = field.Split(KVSeparatorArray, StringSplitOptions.None);
                    if (string.Equals(fieldParts[0], SharedAccessSignatureConstants.AudienceFieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        // We need to preserve the casing of the escape characters in the audience,
                        // so defer decoding the URL until later.
                        parsedFields.Add(fieldParts[0], fieldParts[1]);
                    }
                    else
                    {
                        parsedFields.Add(fieldParts[0], WebUtility.UrlDecode(fieldParts[1]));
                    }
                }
            }

            return parsedFields;
        }
    }
}
