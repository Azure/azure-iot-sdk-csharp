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
    public class ModelSharedAccessSignature : SharedAccessSignature
    {
        internal string _repositoryId;

        private ModelSharedAccessSignature(string shareAccessSignatureName, DateTime expiresOn, string expiry, string keyName, string signature, string encodedAudience, string repositoryId) : base(shareAccessSignatureName, expiresOn, expiry, keyName, signature, encodedAudience)
        {
            _repositoryId = repositoryId;
        }

        public string RepositoryId
        {
            get
            {
                return _repositoryId;
            }
        }
        public override SharedAccessSignature Parse(string shareAccessSignatureName, string rawToken)
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

            string repositoryId;
            if (!parsedFields.TryGetValue(SharedAccessSignatureConstants.repositoryIdFiledName, out repositoryId))
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, "Missing field: {0}", SharedAccessSignatureConstants.repositoryIdFiledName));
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
                expiry, keyName, signature, repositoryId, encodedAudience);
        }
    }
}
