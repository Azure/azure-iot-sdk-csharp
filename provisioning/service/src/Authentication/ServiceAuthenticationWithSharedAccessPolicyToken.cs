// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Authentication method that uses a shared access policy token.
    /// </summary>
    internal sealed class ServiceAuthenticationWithSharedAccessPolicyToken : IAuthenticationMethod
    {
        private string _policyName;
        private string _token;

        internal ServiceAuthenticationWithSharedAccessPolicyToken(string policyName, string token)
        {
            SetPolicyName(policyName);
            SetToken(token);
        }

        public string PolicyName
        {
            get => _policyName;
            set => SetPolicyName(value);
        }

        public string Token
        {
            get => _token;
            set => SetToken(value);
        }

        public ServiceConnectionStringBuilder Populate(ServiceConnectionStringBuilder provisioningConnectionStringBuilder)
        {
            Debug.Assert(provisioningConnectionStringBuilder != null, $"{nameof(provisioningConnectionStringBuilder)} cannot be null. Validate parameters upstream.");

            provisioningConnectionStringBuilder.SharedAccessKeyName = PolicyName;
            provisioningConnectionStringBuilder.SharedAccessSignature = Token;
            provisioningConnectionStringBuilder.SharedAccessKey = null;

            return provisioningConnectionStringBuilder;
        }

        private void SetPolicyName(string policyName)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(policyName), $"{nameof(policyName)} cannot be null or white space.");

            _policyName = policyName;
        }

        private void SetToken(string token)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(token), $"{nameof(token)} cannot be null or white space.");

            if (!token.StartsWith(SharedAccessSignatureConstants.SharedAccessSignature, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Token must be of type SharedAccessSignature");
            }

            _token = token;
        }
    }
}
