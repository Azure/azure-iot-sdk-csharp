// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Devices.Client.Utilities;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Builds Shared Access Signature (SAS) tokens.
    /// </summary>
    internal class SharedAccessSignatureBuilder
    {
        private string _key;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        internal SharedAccessSignatureBuilder()
        {
            TimeToLive = TimeSpan.FromMinutes(60);
        }

        /// <summary>
        /// The shared access policy name.
        /// </summary>
        internal string KeyName { get; set; }

        /// <summary>
        /// The shared access key value.
        /// </summary>
        internal string Key
        {
            get => _key;

            set
            {
                StringValidationHelper.EnsureBase64String(value, "Key");
                _key = value;
            }
        }

        /// <summary>
        /// The resource Id being accessed.
        /// </summary>
        internal string Target { get; set; }

        /// <summary>
        /// The time the token expires.
        /// </summary>
        internal TimeSpan TimeToLive { get; set; }

        /// <summary>
        /// Build a SAS token.
        /// </summary>
        /// <returns>SAS token.</returns>
        internal string ToSignature()
        {
            return BuildSignature(KeyName, Key, Target, TimeToLive, null, null, null);
        }

        internal static string BuildExpiresOn(TimeSpan timeToLive, DateTime startTime = default)
        {
            DateTime expiresOn = startTime == default
                ? DateTime.UtcNow.Add(timeToLive)
                : startTime.Add(timeToLive);

            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            long seconds = Convert.ToInt64(secondsFromBaseTime.TotalSeconds, CultureInfo.InvariantCulture);
            return Convert.ToString(seconds, CultureInfo.InvariantCulture);
        }

        internal static string BuildAudience(string iotHub, string deviceId, string moduleId)
        {
            // DeviceId and ModuleId need to be double encoded.
            string audience = WebUtility.UrlEncode(
                "{0}/devices/{1}/modules/{2}".FormatInvariant(
                    iotHub,
                    WebUtility.UrlEncode(deviceId),
                    WebUtility.UrlEncode(moduleId)));

            return audience;
        }

        internal static string BuildSignature(
            string keyName,
            string key,
            string target,
            TimeSpan timeToLive,
            string audience,
            string signature,
            string expiry)
        {
            string expiresOn = expiry ?? BuildExpiresOn(timeToLive);
            audience ??= WebUtility.UrlEncode(target);
            var fields = new List<string>
            {
                audience,
                expiresOn,
            };

            // Example string to be signed:
            // dh://myiothub.azure-devices.net/a/b/c?myvalue1=a
            // <Value for ExpiresOnUtc>

            signature ??= Sign(string.Join("\n", fields), key);

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

            if (!string.IsNullOrWhiteSpace(keyName))
            {
                buffer.AppendFormat(CultureInfo.InvariantCulture, "&{0}={1}",
                    SharedAccessSignatureConstants.KeyNameFieldName, WebUtility.UrlEncode(keyName));
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Sign the request string with a key.
        /// </summary>
        /// <param name="requestString">The request string input to sign.</param>
        /// <param name="key">The secret key used for encryption.</param>
        /// <returns>The signed request string.</returns>
        protected static string Sign(string requestString, string key)
        {
            using var algorithm = new HMACSHA256(Convert.FromBase64String(key));
            return Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(requestString)));
        }
    }
}
