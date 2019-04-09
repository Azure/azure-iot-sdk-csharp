// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Symmetric key
    /// </summary>
    public sealed class SymmetricKeyAttestation : Attestation
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="primaryKey">Symmetric primary key</param>
        /// <param name="secondaryKey">Symmetric prsecondaryimary key</param>
        [JsonConstructor]
        public SymmetricKeyAttestation(string primaryKey, string secondaryKey)
        {
            try
            {
                PrimaryKey = primaryKey;
                SecondaryKey = secondaryKey;
            }
            catch (ArgumentException e)
            {
                throw new ProvisioningServiceClientException(e);
            }
        }

        /// <summary>
        /// Gets the primary key used for attestation.
        /// </summary>
        [JsonProperty(PropertyName = "primaryKey", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PrimaryKey
        {
            get
            {
                return _primaryKey;
            }
            private set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    ParserUtils.EnsureBase64String(value);
                }
                _primaryKey = value;
            }
        }
        private string _primaryKey;

        /// <summary>
        /// Gets the secondary key used for attestation.
        /// </summary>
        [JsonProperty(PropertyName = "secondaryKey", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string SecondaryKey
        {
            get
            {
                return _secondaryKey;
            }
            private set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    ParserUtils.EnsureBase64String(value);
                }
                _secondaryKey = value;
            }
        }
        private string _secondaryKey;
    }
}
