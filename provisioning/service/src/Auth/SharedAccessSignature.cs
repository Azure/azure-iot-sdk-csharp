// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
#if WINDOWS_UWP
    using PCLCrypto;
#else
using System.Security.Cryptography;
#endif
using System.Text;
using System.Net;

namespace Microsoft.Azure.Devices.Common.Service.Auth
{    
    internal sealed class SharedAccessSignature : ISharedAccessSignatureCredential
    {
        private readonly string _shareAccessSignatureName;
        private readonly string _signature;
        private readonly string _audience;
        private readonly string _encodedAudience;
        private readonly string _expiry;
        private readonly string _keyName;

        private SharedAccessSignature(string shareAccessSignatureName, DateTime expiresOn, string expiry, string keyName, string signature, string encodedAudience)
        {
            if (string.IsNullOrWhiteSpace(shareAccessSignatureName))
            {
                throw new ArgumentNullException(nameof(shareAccessSignatureName));
            }

            ExpiresOn = expiresOn;

            if (IsExpired())
            {
                throw new UnauthorizedAccessException("The specified SAS token is expired");
            }

            _shareAccessSignatureName = shareAccessSignatureName;
            _signature = signature;
            _audience = WebUtility.UrlDecode(encodedAudience);
            _encodedAudience = encodedAudience;
            _expiry = expiry;
            _keyName = keyName ?? string.Empty;
        }

        public string ShareAccessSignatureName
        {
            get
            {
                return _shareAccessSignatureName;
            }
        }

        public DateTime ExpiresOn
        {
            get;
            private set;
        }

        public string KeyName
        {
            get
            {
                return _keyName;
            }
        }

        public string Audience
        {
            get
            {
                return _audience;
            }
        }

        public string Signature
        {
            get
            {
                return _signature;
            }
        }

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

            string signature;
            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.SignatureFieldName, out signature))
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Missing field: {0}", SharedAccessSignatureConstants.SignatureFieldName));
            }

            string expiry;
            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.ExpiryFieldName, out expiry))
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Missing field: {0}", SharedAccessSignatureConstants.ExpiryFieldName));
            }

            // KeyName (skn) is optional .
            string keyName;
            parsedFields.TryGetValue(SharedAccessSignatureConstants.KeyNameFieldName, out keyName);

            string encodedAudience;
            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.AudienceFieldName, out encodedAudience))
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
            return ExpiresOn + SharedAccessSignatureConstants.MaxClockSkew < DateTime.UtcNow;
        }

        public DateTime ExpiryTime()
        {
            return ExpiresOn + SharedAccessSignatureConstants.MaxClockSkew;
        }

        public void Authenticate(SharedAccessSignatureAuthorizationRule sasAuthorizationRule)
        {
            if (IsExpired())
            {
                throw new UnauthorizedAccessException("The specified SAS token is expired.");
            }

            if (sasAuthorizationRule.PrimaryKey != null)
            {
                string primareyKeyComputedSignature = ComputeSignature(Convert.FromBase64String(sasAuthorizationRule.PrimaryKey));
                if (string.Equals(_signature, primareyKeyComputedSignature))
                {
                    return;
                }
            }

            if (sasAuthorizationRule.SecondaryKey != null)
            {
                string secondaryKeyComputedSignature = ComputeSignature(Convert.FromBase64String(sasAuthorizationRule.SecondaryKey));
                if (string.Equals(_signature, secondaryKeyComputedSignature))
                {
                    return;
                }
            }

            throw new UnauthorizedAccessException("The specified SAS token has an invalid signature. It does not match either the primary or secondary key.");
        }

        public void Authorize(string serviceHostName)
        {
            SecurityHelper.ValidateServiceHostName(serviceHostName, ShareAccessSignatureName);
        }

        public void Authorize(Uri targetAddress)
        {
            if (targetAddress == null)
            {
                throw new ArgumentNullException(nameof(targetAddress));
            }
            
            string target = targetAddress.Host + targetAddress.AbsolutePath;

            if (!target.StartsWith(_audience.TrimEnd(new char[] { '/' }), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Invalid target audience");
            }
        }

        public string ComputeSignature(byte[] key)
        {
#if WINDOWS_UWP
            var fields = new List<string>();
            fields.Add(_encodedAudience);
            fields.Add(_expiry);
            string value = string.Join("\n", fields);
            var algorithm = WinRTCrypto.MacAlgorithmProvider.OpenAlgorithm(MacAlgorithm.HmacSha256);
            var hash = algorithm.CreateHash(key);
            hash.Append(Encoding.UTF8.GetBytes(value));
            var mac = hash.GetValueAndReset();
            return Convert.ToBase64String(mac);
#else
            List<string> fields = new List<string>();
            fields.Add(_encodedAudience);
            fields.Add(_expiry);

            using (var hmac = new HMACSHA256(key))
            {
                string value = string.Join("\n", fields);
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
            }
#endif
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
                if (field != string.Empty)
                {
                    string[] fieldParts = field.Split(new string[]{ SharedAccessSignatureConstants.KeyValueSeparator }, StringSplitOptions.None);
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
