// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed class SharedAccessSignatureParser
    {
        /// <summary>
        /// Parse the supplied shared access signature token
        /// </summary>
        /// <param name="rawToken">The shared access signature token</param>
        /// <returns>The shared access signature instance that represents the passed in raw token.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rawToken"/> is null, empty or whitespace.</exception>
        /// <exception cref="FormatException">Thrown if the supplied shared access signature doesn't contain the expected fields.</exception>
        internal static SharedAccessSignature Parse(string rawToken)
        {
            Argument.AssertNotNullOrWhiteSpace(rawToken, nameof(rawToken));

            IDictionary<string, string> parsedFields = ExtractFieldValues(rawToken);

            // sig
            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.SignatureFieldName, out string signature))
            {
                throw new FormatException($"Missing field: {SharedAccessSignatureConstants.SignatureFieldName}");
            }

            // se
            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.ExpiryFieldName, out string expiry))
            {
                throw new FormatException($"Missing field: {SharedAccessSignatureConstants.ExpiryFieldName}");
            }

            // skn (optional)
            parsedFields.TryGetValue(SharedAccessSignatureConstants.KeyNameFieldName, out string keyName);

            // sr
            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.AudienceFieldName, out string encodedAudience))
            {
                throw new FormatException($"Missing field: {SharedAccessSignatureConstants.AudienceFieldName}");
            }

            return new SharedAccessSignature(
                SharedAccessSignatureConstants.EpochTime + TimeSpan.FromSeconds(double.Parse(expiry, CultureInfo.InvariantCulture)),
                keyName,
                signature,
                encodedAudience);
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