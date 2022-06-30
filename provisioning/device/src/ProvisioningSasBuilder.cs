// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Devices.Authentication;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal static class ProvisioningSasBuilder
    {
        private const string KeyName = "registration";
        private static readonly TimeSpan s_timeToLive = TimeSpan.FromDays(1);

        internal static string ExtractServiceAuthKey(AuthenticationProviderTpm authenticationProvider, string hostName, byte[] activation)
        {
            authenticationProvider.ActivateIdentityKey(activation);
            return BuildSasSignature(authenticationProvider, KeyName, hostName, s_timeToLive);
        }

        private static string BuildSasSignature(AuthenticationProviderTpm authenticationProvider, string keyName, string target, TimeSpan timeToLive)
        {
            string expiresOn = BuildExpiresOn(timeToLive);
            string audience = WebUtility.UrlEncode(target);
            var fields = new List<string>
            {
                audience,
                expiresOn
            };

            // Example string to be signed:
            // dh://myiothub.azure-devices-provisioning.net/a/b/c?myvalue1=a
            // <Value for ExpiresOn>

            byte[] signedBytes = authenticationProvider.Sign(Encoding.UTF8.GetBytes(string.Join("\n", fields)));
            string signature = Convert.ToBase64String(signedBytes);

            // Example returned string:
            // SharedAccessSignature sr=ENCODED(dh://myiothub.azure-devices.net/a/b/c?myvalue1=a)&sig=<Signature>&se=<ExpiresOnValue>[&skn=<KeyName>]

            var buffer = new StringBuilder();
            buffer.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0} {1}={2}&{3}={4}&{5}={6}",
                "SharedAccessSignature",
                "sr",
                audience,
                "sig",
                WebUtility.UrlEncode(signature),
                "se",
                WebUtility.UrlEncode(expiresOn));

            if (!string.IsNullOrEmpty(keyName))
            {
                buffer.AppendFormat(CultureInfo.InvariantCulture, "&{0}={1}", "skn", WebUtility.UrlEncode(keyName));
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Construct a SAS signature using HMAC hash.
        /// </summary>
        /// <param name="keyName">The name of the key</param>
        /// <param name="key">The primary/secondary symmetric key to hash</param>
        /// <param name="target">The path to target</param>
        /// <param name="timeToLive">The time before the returned signature expires</param>
        /// <returns>The sas signature derived from the provided symmetric key</returns>
        public static string BuildSasSignature(string keyName, string key, string target, TimeSpan timeToLive)
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

            string signature = HmacSign(string.Join("\n", fields), key);

            // Example returned string:
            // SharedAccessSignature sr=ENCODED(dh://myiothub.azure-devices.net/a/b/c?myvalue1=a)&sig=<Signature>&se=<ExpiresOnValue>[&skn=<KeyName>]

            var buffer = new StringBuilder();
            buffer.AppendFormat(CultureInfo.InvariantCulture, "{0} {1}={2}&{3}={4}&{5}={6}",
                "SharedAccessSignature",
                "sr", audience,
                "sig", WebUtility.UrlEncode(signature),
                "se", WebUtility.UrlEncode(expiresOn));

            if (!string.IsNullOrEmpty(keyName))
            {
                buffer.AppendFormat(CultureInfo.InvariantCulture, "&{0}={1}",
                    "skn", WebUtility.UrlEncode(keyName));
            }

            return buffer.ToString();
        }

        public static string BuildExpiresOn(TimeSpan timeToLive)
        {
            var epochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            DateTime expiresOn = DateTime.UtcNow.Add(timeToLive);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(epochTime);
            long seconds = Convert.ToInt64(secondsFromBaseTime.TotalSeconds, CultureInfo.InvariantCulture);
            return Convert.ToString(seconds, CultureInfo.InvariantCulture);
        }

        private static string HmacSign(string requestString, string key)
        {
            using var hmac = new HMACSHA256(Convert.FromBase64String(key));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(requestString)));
        }
    }
}
