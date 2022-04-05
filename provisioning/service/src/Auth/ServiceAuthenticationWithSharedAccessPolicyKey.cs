// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Common.Service.Auth
{
    /// <summary>
    /// Authentication method that uses a shared access policy key. 
    /// </summary>
    internal sealed class ServiceAuthenticationWithSharedAccessPolicyKey : IAuthenticationMethod
    {
        string policyName;
        string key;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceAuthenticationWithSharedAccessPolicyKey"/> class.
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
            get { return policyName; }
            set { SetPolicyName(value);}
        }

        public string Key
        {
            get { return key; }
            set { SetKey(value); }
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
            if (string.IsNullOrWhiteSpace(policyName))
            {
                throw new ArgumentNullException(nameof(policyName));
            }

            this.policyName = policyName;
        }

        private void SetKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (!StringValidationHelper.IsBase64String(key))
            {
                throw new ArgumentException("Key must be Base64 encoded");
            }

            this.key = key;
        }
    }
}