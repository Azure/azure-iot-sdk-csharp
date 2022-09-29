// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

namespace Microsoft.Azure.Devices
{
    internal sealed class SharedAccessSignatureParser
    {
        /// <summary>
        /// Parses a shared access signature string representation into a <see cref="SharedAccessSignature"/>.
        /// </summary>
        /// <param name="iotHubName">The IoT hub name.</param>
        /// <param name="rawToken">The string representation of the SAS token to parse.</param>
        /// <returns>The shared access signature instance that represents the passed in raw token.</returns>
        internal static SharedAccessSignature Parse(string iotHubName, string rawToken)
        {
            Argument.AssertNotNullOrWhiteSpace(iotHubName, nameof(iotHubName));
            Argument.AssertNotNullOrWhiteSpace(rawToken, nameof(rawToken));

            IDictionary<string, string> parsedFields = ExtractFieldValues(rawToken);

            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.SignatureFieldName, out string signature))
            {
                throw new FormatException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Missing field: {0}",
                    SharedAccessSignatureConstants.SignatureFieldName));
            }

            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.ExpiryFieldName, out string expiry))
            {
                throw new FormatException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Missing field: {0}",
                    SharedAccessSignatureConstants.ExpiryFieldName));
            }

            // KeyName (skn) is optional.
            parsedFields.TryGetValue(SharedAccessSignatureConstants.KeyNameFieldName, out string keyName);

            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.AudienceFieldName, out string encodedAudience))
            {
                throw new FormatException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Missing field: {0}",
                    SharedAccessSignatureConstants.AudienceFieldName));
            }

            return new SharedAccessSignature(
                iotHubName,
                SharedAccessSignatureConstants.EpochTime + TimeSpan.FromSeconds(double.Parse(expiry, CultureInfo.InvariantCulture)),
                keyName,
                signature,
                encodedAudience);
        }

        /// <summary>
        /// Validates whether a string token is a valid SAS token.
        /// </summary>
        /// <param name="rawSignature">The string representation of the SAS token to parse.</param>
        /// <returns>True if the passed in raw signature is a valid SAS token. False otherwise.</returns>
        internal static bool IsSharedAccessSignature(string rawSignature)
        {
            if (string.IsNullOrWhiteSpace(rawSignature))
            {
                return false;
            }

            IDictionary<string, string> parsedFields = ExtractFieldValues(rawSignature);
            bool isSharedAccessSignature = parsedFields.TryGetValue(SharedAccessSignatureConstants.SignatureFieldName, out _);

            return isSharedAccessSignature;
        }

        private static IDictionary<string, string> ExtractFieldValues(string sharedAccessSignature)
        {
            string[] lines = sharedAccessSignature.Split();

            if (!StringComparer.Ordinal.Equals(
                    lines[0].Trim(),
                    SharedAccessSignatureConstants.SharedAccessSignature)
                || lines.Length != 2)
            {
                throw new FormatException("Malformed signature");
            }

            IDictionary<string, string> parsedFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] fields = lines[1].Trim().Split(new string[] { SharedAccessSignatureConstants.PairSeparator }, StringSplitOptions.None);

            foreach (string field in fields)
            {
                if (!string.IsNullOrEmpty(field))
                {
                    string[] fieldParts = field.Split(new string[] { SharedAccessSignatureConstants.KeyValueSeparator }, StringSplitOptions.None);
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
