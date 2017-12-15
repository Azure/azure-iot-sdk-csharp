// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    //TODO: merge with other Sas Token Builder in IoTHubClient
    internal static class ProvisioningSasBuilder
    {
        private const string KeyName = "registration";
        private static readonly TimeSpan TimeToLive = TimeSpan.FromDays(1);

        internal static string ExtractServiceAuthKey(SecurityProviderTpm securityProvider, string hostName, byte[] activation)
        {
            securityProvider.ActivateIdentityKey(activation);
            return BuildSasSignature(securityProvider, KeyName, hostName, TimeToLive);
        }

        private static string BuildSasSignature(SecurityProviderTpm securityProvider, string keyName, string target, TimeSpan timeToLive)
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

            byte[] signedBytes = securityProvider.Sign(Encoding.UTF8.GetBytes(string.Join("\n", fields)));
            string signature = Convert.ToBase64String(signedBytes);

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

        private static string BuildExpiresOn(TimeSpan timeToLive)
        {
            DateTime epochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            DateTime expiresOn = DateTime.UtcNow.Add(timeToLive);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(epochTime);
            long seconds = Convert.ToInt64(secondsFromBaseTime.TotalSeconds, CultureInfo.InvariantCulture);
            return Convert.ToString(seconds, CultureInfo.InvariantCulture);
        }
    }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         