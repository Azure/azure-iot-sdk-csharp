// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using Microsoft.Azure.DigitalTwin.Model.Service;

namespace Microsoft.Azure.Devices.Common.Authorization
{
    public class ModelSharedAccessSignatureBuilder : SharedAccessSignatureBuilder
    {
        public string RepositoryId { get; set; }

        public virtual string ToSignature()
        {
            return BuildSignature(KeyName, Key, HostName, RepositoryId, TimeToLive).ToString();
        }

        protected override StringBuilder BuildSignature()
        {
            string expiresOn = BuildExpiresOn(TimeToLive);
            string audience = WebUtility.UrlEncode(Hostname);
            List<string> fields = new List<string>();
            fields.Add(Audience);
            fields.Add(ExpiresOn);

            // Example string to be signed:
            // dh://myiothub.azure-devices.net/a/b/c?myvalue1=a
            // <Value for ExpiresOn>

            string signature = Sign(string.Join("\n", fields), key);

            // Example returned string:
            // SharedAccessSignature sr=ENCODED(dh://myiothub.azure-devices.net/a/b/c?myvalue1=a)&sig=<Signature>&se=<ExpiresOnValue>[&skn=<KeyName>]&rid=<repositoryId>

            var buffer = new StringBuilder();
            buffer.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0} {1}={2}&{3}={4}&{5}={6}&{7}={8}",
                SharedAccessSignatureConstants.SharedAccessSignature,
                SharedAccessSignatureConstants.AudienceFieldName,
                Audience,
                SharedAccessSignatureConstants.SignatureFieldName,
                WebUtility.UrlEncode(signature),
                SharedAccessSignatureConstants.ExpiryFieldName,
                WebUtility.UrlEncode(expiresOn),
                SharedAccessSignatureConstants.repositoryIdFiledName,
                repositoryId);

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
