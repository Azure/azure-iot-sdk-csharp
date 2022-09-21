// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Authentication method that uses a shared access policy key.
    /// </summary>
    internal sealed class ServiceAuthenticationWithSharedAccessPolicyKey : IAuthenticationMethod
    {
        private string _policyName;
        private string _key;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="policyName">Name of the shared access policy to use.</param>
        /// <param name="key">Key associated with the shared access policy.</param>
        public ServiceAuthenticationWithSharedAccessPolicyKey(string policyName, string key)
        {
            SetPolicyName(policyName);
            SetKey(key);
        }

        public string PolicyName
        {
            get => _policyName;
            set => SetPolicyName(value);
        }

        public string Key
        {
            get => _key;
            set => SetKey(value);
        }

        public ServiceConnectionStringBuilder Populate(ServiceConnectionStringBuilder provisioningConnectionStringBuilder)
        {
            if (provisioningConnectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(provisioningConnectionStringBuilder));
            }

            provisioningConnectionStringBuilder.SharedAccessKey = Key;
            provisioningConnectionStringBuilder.SharedAccessKeyName = PolicyName;
            provisioningConnectionStringBuilder.SharedAccessSignature = null;

            return provisioningConnectionStringBuilder;
        }

        private void SetPolicyName(string policyName)
        {
            _policyName = policyName;
        }

        private void SetKey(string key)
        {
            _key = key;
        }
    }
}
