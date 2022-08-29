// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Common.Security;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Authentication method that uses a shared access policy token.
    /// </summary>
    public sealed class ServiceAuthenticationWithSharedAccessPolicyToken : IAuthenticationMethod
    {
        private string _policyName;
        private string _token;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="policyName">Name of the shared access policy to use.</param>
        /// <param name="token">Token associated with the shared access policy.</param>
        public ServiceAuthenticationWithSharedAccessPolicyToken(string policyName, string token)
        {
            SetPolicyName(policyName);
            SetToken(token);
        }

        /// <summary>
        /// The name of the policy.
        /// </summary>
        public string PolicyName
        {
            get => _policyName;
            set => SetPolicyName(value);
        }

        /// <summary>
        /// The SAS token.
        /// </summary>
        public string Token
        {
            get => _token;
            set => SetToken(value);
        }

        /// <summary>
        /// Updates the specified connection string builder with policy name and token.
        /// </summary>
        /// <param name="iotHubConnectionStringBuilder">The connection string builder to update.</param>
        /// <returns>The populated connection string builder object.</returns>
        public IotHubConnectionStringBuilder Populate(IotHubConnectionStringBuilder iotHubConnectionStringBuilder)
        {
            if (iotHubConnectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(iotHubConnectionStringBuilder));
            }

            iotHubConnectionStringBuilder.SharedAccessKeyName = PolicyName;
            iotHubConnectionStringBuilder.SharedAccessSignature = Token;
            iotHubConnectionStringBuilder.SharedAccessKey = null;

            return iotHubConnectionStringBuilder;
        }

        private void SetPolicyName(string policyName)
        {
            if (string.IsNullOrWhiteSpace(policyName))
            {
                throw new ArgumentNullException(nameof(policyName));
            }

            _policyName = policyName;
        }

        private void SetToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (!token.StartsWith(SharedAccessSignatureConstants.SharedAccessSignature, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Token must be of type SharedAccessSignature");
            }

            _token = token;
        }
    }
}
