// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed class SharedAccessSignature
    {
        private SharedAccessSignature(string iotHubName, DateTime expiresOn, string keyName, string signature, string encodedAudience)
        {
            if (string.IsNullOrWhiteSpace(iotHubName))
            {
                throw new ArgumentNullException(nameof(iotHubName));
            }

            ExpiresOn = expiresOn;
            if (IsExpired())
            {
                throw new UnauthorizedAccessException($"The specified SAS token has already expired - on {expiresOn}.");
            }

            IotHubName = iotHubName;
            KeyName = keyName ?? string.Empty;
            Signature = signature;
            Audience = WebUtility.UrlDecode(encodedAudience);
        }

        public string IotHubName { get; }

        public DateTime ExpiresOn { get; private set; }

        public string KeyName { get; private set; }

        public string Audience { get; private set; }

        public string Signature { get; private set; }

        public static SharedAccessSignature Parse(string iotHubName, string rawToken)
        {
            if (string.IsNullOrWhiteSpace(iotHubName))
            {
                throw new ArgumentNullException(nameof(iotHubName));
            }

            if (string.IsNullOrWhiteSpace(rawToken))
            {
                throw new ArgumentNullException(nameof(rawToken));
            }

            IDictionary<string, string> parsedFields = ExtractFieldValues(rawToken);

            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.SignatureFieldName, out string signature))
            {
                throw new FormatException($"Missing field: {SharedAccessSignatureConstants.SignatureFieldName}");
            }

            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.ExpiryFieldName, out string expiry))
            {
                throw new FormatException($"Missing field: {SharedAccessSignatureConstants.ExpiryFieldName}");
            }

            // KeyName (skn) is optional.
            parsedFields.TryGetValue(SharedAccessSignatureConstants.KeyNameFieldName, out string keyName);

            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.AudienceFieldName, out string encodedAudience))
            {
                throw new FormatException($"Missing field: {SharedAccessSignatureConstants.AudienceFieldName}");
            }

            return new SharedAccessSignature(
                iotHubName,
                SharedAccessSignatureConstants.EpochTime + TimeSpan.FromSeconds(double.Parse(expiry, CultureInfo.InvariantCulture)),
                keyName,
                signature,
                encodedAudience);
        }

        public static bool IsSharedAccessSignature(string rawSignature)
        {
            if (string.IsNullOrWhiteSpace(rawSignature))
            {
                return false;
            }

            try
            {
                IDictionary<string, string> parsedFields = ExtractFieldValues(rawSignature);
                bool isSharedAccessSignature = parsedFields.TryGetValue(SharedAccessSignatureConstants.SignatureFieldName, out string signature);
                return isSharedAccessSignature;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public bool IsExpired()
        {
            return ExpiresOn + SharedAccessSignatureConstants.MaxClockSkew < DateTime.UtcNow;
        }

        private static IDictionary<string, string> ExtractFieldValues(string sharedAccessSignature)
        {
            string[] lines = sharedAccessSignature.Split();

            if (lines.Length != 2
                || !StringComparer.Ordinal.Equals(SharedAccessSignatureConstants.SharedAccessSignature, lines[0].Trim()))
            {
                throw new FormatException("Malformed signature");
            }

            IDictionary<string, string> parsedFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] fields = lines[1].Trim().Split(SharedAccessSignatureConstants.PairSeparator);

            foreach (string field in fields)
            {
                if (!string.IsNullOrEmpty(field))
                {
                    string[] fieldParts = field.Split(SharedAccessSignatureConstants.KeyValueSeparator);
                    if (fieldParts.Length < 2)
                    {
                        throw new FormatException("Malformed signature");
                    }

                    if (StringComparer.OrdinalIgnoreCase.Equals(SharedAccessSignatureConstants.AudienceFieldName, fieldParts[0]))
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
