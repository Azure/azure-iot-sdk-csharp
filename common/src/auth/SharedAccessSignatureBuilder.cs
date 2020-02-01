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
    public class SharedAccessSignatureBuilder
    {
        public SharedAccessSignatureBuilder()
        {
            TimeToLive = TimeSpan.FromMinutes(20);
        }

        public string KeyName { get; set; }

        public string Key { get; set; }

        public string hostName { get; set; }

        public TimeSpan TimeToLive { get; set; }

        public virtual string ToSignature()
        {
            return BuildSignature(KeyName, Key, hostName).ToString();
        }

        protected StringBuilder BuildSignature(string keyName, string key, string hostName)
        {
            string expiresOn = BuildExpiresOn(TimeToLive);
            //string expiresOn = WebUtility.UrlDecode("1580511818");
            string audience = WebUtility.UrlEncode(hostName);
            //string audience = WebUtility.UrlEncode("72f988bf-86f1-41af-91ab-2d7cd011db47");
            // Change later
            string repositoryId = WebUtility.UrlEncode("8594dc7436a54c4492216728a1c01ed6");
            var fields = new List<string>
            {
                repositoryId,
                audience,
                expiresOn,
            };

            // Example string to be signed:
            // dh://myiothub.azure-devices.net/a/b/c?myvalue1=a
            // <Value for ExpiresOn>

            string signature = Sign(string.Join("\n", fields).ToLower(), key);

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

            if (!string.IsNullOrEmpty(KeyName))
            {
                buffer.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "&{0}={1}",
                    SharedAccessSignatureConstants.KeyNameFieldName,
                    WebUtility.UrlEncode(keyName));
            }

            return buffer;
        }

        protected string BuildExpiresOn(TimeSpan timeToLive)
        {
            DateTime expiresOn = DateTime.UtcNow.Add(timeToLive);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(SharedAccessSignatureConstants.EpochTime);
            long seconds = Convert.ToInt64(secondsFromBaseTime.TotalSeconds, CultureInfo.InvariantCulture);
            return Convert.ToString(seconds, CultureInfo.InvariantCulture);
        }

        protected string Sign(string requestString, string key)
        {
            using (var hmac = new HMACSHA256(Convert.FromBase64String(key)))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(requestString)));
            }
        }
    }
}
