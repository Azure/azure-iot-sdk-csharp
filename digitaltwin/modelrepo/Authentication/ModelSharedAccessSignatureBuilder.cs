// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Net;

namespace Microsoft.Azure.Devices.Common.Authorization
{
    public class ModelSharedAccessSignatureBuilder : SharedAccessSignatureBuilder
    {
        public string RepositoryId { get; set; }

        public override string ToSignature()
        {
            return BuildSignatureForModelRepo(KeyName, Key, hostName, RepositoryId).ToString();
        }

        protected StringBuilder BuildSignatureForModelRepo(string keyName, string key, string hostName, string repositoryId)
        {
            string expiresOn = BuildExpiresOn(TimeToLive);
            string hostNameEncoded = WebUtility.UrlEncode(hostName);
            string repositoryIdEncoded = WebUtility.UrlEncode(repositoryId);
            var fields = new List<string>
            {
                repositoryIdEncoded,
                hostNameEncoded,
                expiresOn,
            };

            // Example string to be signed:
            // dh://myiothub.azure-devices.net/a/b/c?myvalue1=a
            // <Value for ExpiresOn>

            string signature = Sign(string.Join("\n", fields).ToLower(), key);

            // Example returned string:
            // SharedAccessSignature sr=ENCODED(dh://myiothub.azure-devices.net/a/b/c?myvalue1=a)&sig=<Signature>&se=<ExpiresOnValue>[&skn=<KeyName>]&rid=<RepositoryId>

            var buffer = new StringBuilder();
            buffer.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0} {1}={2}&{3}={4}&{5}={6}&{7}={8}",
                SharedAccessSignatureConstants.SharedAccessSignature,
                SharedAccessSignatureConstants.AudienceFieldName,
                hostNameEncoded,
                SharedAccessSignatureConstants.SignatureFieldName,
                WebUtility.UrlEncode(signature),
                SharedAccessSignatureConstants.ExpiryFieldName,
                WebUtility.UrlEncode(expiresOn),
                ModelSharedAccessSignatureConstants.RepositoryIdFieldName,
                repositoryIdEncoded);

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
    }
}
