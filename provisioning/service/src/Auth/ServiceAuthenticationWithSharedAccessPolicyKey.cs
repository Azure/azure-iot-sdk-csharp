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
            this.SetPolicyName(policyName);
            this.SetKey(key);
        }

        public string PolicyName
        {
            get { return this.policyName; }
            set { this.SetPolicyName(value);}
        }

        public string Key
        {
            get { return this.key; }
            set { this.SetKey(value); }
        }

        public ServiceConnectionStringBuilder Populate(ServiceConnectionStringBuilder provisioningConnectionStringBuilder)
        {
            if (provisioningConnectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(provisioningConnectionStringBuilder));
            }

            provisioningConnectionStringBuilder.SharedAccessKey = this.Key;
            provisioningConnectionStringBuilder.SharedAccessKeyName = this.PolicyName;
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

            this.key = key;
        }
    }
}