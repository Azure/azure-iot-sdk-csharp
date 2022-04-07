// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed class SharedAccessSignature : ISharedAccessSignatureCredential
    {
        private readonly string _encodedAudience;
        private readonly string _expiry;

        private SharedAccessSignature(string iotHubName, DateTime expiresOn, string expiry, string keyName, string signature, string encodedAudience)
        {
            if (string.IsNullOrWhiteSpace(iotHubName))
            {
                throw new ArgumentNullException(nameof(iotHubName));
            }

            ExpiresOn = expiresOn;

            if (IsExpired())
            {
                throw new UnauthorizedAccessException($"The specified SAS token is expired on {ExpiresOn}.");
            }

            IotHubName = iotHubName;
            Signature = signature;
            Audience = WebUtility.UrlDecode(encodedAudience);
            _encodedAudience = encodedAudience;
            _expiry = expiry;
            KeyName = keyName ?? string.Empty;
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
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Missing field: {0}", SharedAccessSignatureConstants.SignatureFieldName));
            }

            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.ExpiryFieldName, out string expiry))
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Missing field: {0}", SharedAccessSignatureConstants.ExpiryFieldName));
            }

            // KeyName (skn) is optional.
            parsedFields.TryGetValue(SharedAccessSignatureConstants.KeyNameFieldName, out string keyName);

            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.AudienceFieldName, out string encodedAudience))
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Missing field: {0}", SharedAccessSignatureConstants.AudienceFieldName));
            }

            return new SharedAccessSignature(iotHubName, SharedAccessSignatureConstants.EpochTime + TimeSpan.FromSeconds(double.Parse(expiry, CultureInfo.InvariantCulture)), expiry, keyName, signature, encodedAudience);
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

        public string ComputeSignature(byte[] key)
        {
            var fields = new List<string>
            {
                _encodedAudience,
                _expiry,
            };
            string value = string.Join("\n", fields);
            return Sign(key, value);
        }

        internal static string Sign(byte[] key, string value)
        {
            using var algorithm = new HMACSHA256(key);
            return Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }

        private static IDictionary<string, string> ExtractFieldValues(string sharedAccessSignature)
        {
            string[] lines = sharedAccessSignature.Split();

            if (!string.Equals(lines[0].Trim(), SharedAccessSignatureConstants.SharedAccessSignature, StringComparison.Ordinal) || lines.Length != 2)
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
