// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Common;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Authentication method that uses a shared access policy key.
    /// </summary>
    public sealed class ServiceAuthenticationWithSharedAccessPolicyKey : IAuthenticationMethod
    {
        private string _policyName;
        private string _key;

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

        /// <summary>
        /// The shared access policy name.
        /// </summary>
        public string PolicyName
        {
            get => _policyName;
            set => SetPolicyName(value);
        }

        /// <summary>
        /// The shared access key value.
        /// </summary>
        public string Key
        {
            get => _key;
            set => SetKey(value);
        }

        /// <summary>
        /// Populates the builder with values needed to authenticate with shared access policy key.
        /// </summary>
        /// <param name="iotHubConnectionStringBuilder">The connection build object to populate.</param>
        /// <returns>The populated connection string builder object.</returns>
        public IotHubConnectionStringBuilder Populate(IotHubConnectionStringBuilder iotHubConnectionStringBuilder)
        {
            if (iotHubConnectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(iotHubConnectionStringBuilder));
            }

            iotHubConnectionStringBuilder.SharedAccessKey = Key;
            iotHubConnectionStringBuilder.SharedAccessKeyName = PolicyName;
            iotHubConnectionStringBuilder.SharedAccessSignature = null;

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

            _key = key;
        }
    }
}
