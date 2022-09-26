// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Authentication method that uses a shared access policy key.
    /// </summary>
    internal sealed class ServiceAuthenticationWithSharedAccessPolicyKey : IAuthenticationMethod
    {
        private string _policyName;
        private string _key;

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
            provisioningConnectionStringBuilder.SharedAccessKey = Key;
            provisioningConnectionStringBuilder.SharedAccessKeyName = PolicyName;
            provisioningConnectionStringBuilder.SharedAccessSignature = null;

            return provisioningConnectionStringBuilder;
        }

        internal ServiceAuthenticationWithSharedAccessPolicyKey(string policyName, string key)
        {
            SetPolicyName(policyName);
            SetKey(key);
        }

        private void SetPolicyName(string policyName)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(policyName));
            _policyName = policyName;
        }

        private void SetKey(string key)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(key));
            _key = key;
        }
    }
}
