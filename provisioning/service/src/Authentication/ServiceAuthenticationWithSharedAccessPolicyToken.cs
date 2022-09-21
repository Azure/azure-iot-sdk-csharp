// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Authentication method that uses a shared access policy token.
    /// </summary>
    internal sealed class ServiceAuthenticationWithSharedAccessPolicyToken : IAuthenticationMethod
    {
        private string _policyName;
        private string _token;

        /// <summary>
        /// Creates an instance of the <see cref="ServiceAuthenticationWithSharedAccessPolicyToken"/> class.
        /// </summary>
        /// <param name="policyName">Name of the shared access policy to use.</param>
        /// <param name="token">Token associated with the shared access policy.</param>
        public ServiceAuthenticationWithSharedAccessPolicyToken(string policyName, string token)
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
            Argument.AssertNotNull(provisioningConnectionStringBuilder, nameof(provisioningConnectionStringBuilder));

            provisioningConnectionStringBuilder.SharedAccessKeyName = PolicyName;
            provisioningConnectionStringBuilder.SharedAccessSignature = Token;
            provisioningConnectionStringBuilder.SharedAccessKey = null;

            return provisioningConnectionStringBuilder;
        }

        private void SetPolicyName(string policyName)
        {
            Argument.AssertNotNullOrWhiteSpace(policyName, nameof(policyName));

            _policyName = policyName;
        }

        private void SetToken(string token)
        {
            Argument.AssertNotNullOrWhiteSpace(token, nameof(token));

            if (!token.StartsWith(SharedAccessSignatureConstants.SharedAccessSignature, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Token must be of type SharedAccessSignature");
            }

            _token = token;
        }
    }
}
