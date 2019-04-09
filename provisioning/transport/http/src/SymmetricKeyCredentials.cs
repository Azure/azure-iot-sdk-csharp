// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class SymmetricKeyCredentials : ServiceClientCredentials
    {
        private const string SASHeaderName = "SharedAccessSignature";
        private const string Registration = "registration";
        private readonly string SymmetricKey;
        private volatile string _sasToken;

        public SymmetricKeyCredentials(string symmetricKey): base()
        {
            SymmetricKey = symmetricKey;
        }

        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string audience = request.RequestUri.AbsolutePath.Trim('/');
            var segments = audience.Split('/');

            _sasToken = BuildSasSignature(Registration, this.SymmetricKey, string.Concat(segments[0], '/', segments[1], '/', segments[2]), TimeSpan.FromDays(1));
            SetAuthorizationHeader(request, _sasToken);

            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }

        private static void SetAuthorizationHeader(HttpRequestMessage request, string sasToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(SASHeaderName, sasToken.Substring(SASHeaderName.Length+1));
        }

        //TODO: merge with other Sas Token Builder in ProvisioningSasBuilder and in IotHubClient
        public static string BuildSasSignature(string keyName, string key, string target, TimeSpan timeToLive)
        {
            string expiresOn = ProvisioningSasBuilder.BuildExpiresOn(timeToLive);
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

        private static string Sign(string requestString, string key)
        {
            using (var hmac = new HMACSHA256(Convert.FromBase64String(key)))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(requestString)));
            }
        }
    }
}
