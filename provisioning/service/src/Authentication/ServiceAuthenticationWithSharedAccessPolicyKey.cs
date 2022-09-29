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

        internal ServiceAuthenticationWithSharedAccessPolicyKey(string policyName, string key)
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
            Debug.Assert(provisioningConnectionStringBuilder != null, $"{nameof(provisioningConnectionStringBuilder)} cannot be null. Validate parameters upstream.");

            provisioningConnectionStringBuilder.SharedAccessKey = Key;
            provisioningConnectionStringBuilder.SharedAccessKeyName = PolicyName;
            provisioningConnectionStringBuilder.SharedAccessSignature = null;

            return provisioningConnectionStringBuilder;
        }

        private void SetPolicyName(string policyName)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(policyName), $"{nameof(policyName)} cannot be null or white space.");

            _policyName = policyName;
        }

        private void SetKey(string key)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(key), $"{nameof(key)} cannot be null or white space.");

            _key = key;
        }
    }
}
