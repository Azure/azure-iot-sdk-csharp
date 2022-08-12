// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Azure.Devices.Common.Security
{
    /// <summary>
    /// Builds Shared Access Signature (SAS) tokens.
    /// </summary>
    public sealed class SharedAccessSignatureBuilder
    {
        private string _key;

        /// <summary>
        /// Initializes a new instance of <see cref="SharedAccessSignatureBuilder"/> class.
        /// </summary>
        public SharedAccessSignatureBuilder()
        {
            TimeToLive = TimeSpan.FromMinutes(20);
        }

        /// <summary>
        /// The shared access policy name.
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// The shared access key value.
        /// </summary>
        public string Key
        {
            get => _key;
            set => _key = value;
        }

        /// <summary>
        /// The resource Id being accessed.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// The time the token expires.
        /// </summary>
        public TimeSpan TimeToLive { get; set; }

        /// <summary>
        /// Build a SAS token.
        /// </summary>
        /// <returns>SAS token.</returns>
        public string ToSignature()
        {
            return BuildSignature(KeyName, Key, Target, TimeToLive);
        }

        private static string BuildSignature(string keyName, string key, string target, TimeSpan timeToLive)
        {
            string expiresOn = BuildExpiresOn(timeToLive);
            string audience = WebUtility.UrlEncode(target);
            var fields = new List<string>
            {
                audience,
                expiresOn
            };

            // Example string to be signed:
            // dh://myiothub.azure-devices.net/a/b/c?myvalue1=a
            // <Value for ExpiresOn>

            string signature = Sign(string.Join("\n", fields), key);

            // Example returned string:
            // SharedAccessSignature sr=ENCODED(dh://myiothub.azure-devices.net/a/b/c?myvalue1=a)&sig=<Signature>&se=<ExpiresOnValue>[&skn=<KeyName>]

            var buffer = new StringBuilder();
            buffer.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0} {1}={2}&{3}={4}&{5}={6}",
                SharedAccessSignatureConstants.SharedAccessSignature,
                SharedAccessSignatureConstants.AudienceFieldName,
                audience,
                SharedAccessSignatureConstants.SignatureFieldName,
                WebUtility.UrlEncode(signature),
                SharedAccessSignatureConstants.ExpiryFieldName,
                WebUtility.UrlEncode(expiresOn));

            if (!string.IsNullOrEmpty(keyName))
            {
                buffer.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "&{0}={1}",
                    SharedAccessSignatureConstants.KeyNameFieldName,
                    WebUtility.UrlEncode(keyName));
            }

            return buffer.ToString();
        }

        private static string BuildExpiresOn(TimeSpan timeToLive)
        {
            DateTime expiresOn = DateTime.UtcNow.Add(timeToLive);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            long seconds = Convert.ToInt64(secondsFromBaseTime.TotalSeconds, CultureInfo.InvariantCulture);
            return Convert.ToString(seconds, CultureInfo.InvariantCulture);
        }

        private static string Sign(string requestString, string key)
        {
            using var hmac = new HMACSHA256(Convert.FromBase64String(key));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(requestString)));
        }
    }
}
