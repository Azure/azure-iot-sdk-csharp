// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Common.Data
{
    /// <summary>
    /// A shared access signature based authorization rule for authenticating requests against an IoT hub.
    /// </summary>
    internal sealed class SharedAccessSignatureAuthorizationRule : IEquatable<SharedAccessSignatureAuthorizationRule>
    {
        private string _primaryKey;
        private string _secondaryKey;

        [JsonProperty(PropertyName = "keyName")]
        public string KeyName { get; set; }

        /// <summary>
        /// The primary key associated with the shared access policy.
        /// </summary>
        [JsonProperty(PropertyName = "primaryKey")]
        public string PrimaryKey
        {
            get => _primaryKey;

            set
            {
                StringValidationHelper.EnsureNullOrBase64String(value, "PrimaryKey");
                _primaryKey = value;
            }
        }

        /// <summary>
        /// The secondary key associated with the shared access policy.
        /// </summary>
        [JsonProperty(PropertyName = "secondaryKey")]
        public string SecondaryKey
        {
            get => _secondaryKey;

            set
            {
                StringValidationHelper.EnsureNullOrBase64String(value, "SecondaryKey");
                _secondaryKey = value;
            }
        }

        /// <summary>
        /// The name of the shared access policy that will be used to grant permission to IoT hub endpoints.
        /// </summary>
        [JsonProperty(PropertyName = "rights")]
        public AccessRights Rights { get; set; }

        public bool Equals(SharedAccessSignatureAuthorizationRule other)
        {
            if (other == null)
            {
                return false;
            }

            bool areEqual = string.Equals(KeyName, other.KeyName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(PrimaryKey, other.PrimaryKey, StringComparison.Ordinal)
                && string.Equals(SecondaryKey, other.SecondaryKey, StringComparison.Ordinal)
                && Equals(Rights, other.Rights);

            return areEqual;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as SharedAccessSignatureAuthorizationRule);
        }

        /// <summary>
        /// Gets a hash code for a given object.
        /// </summary>
        /// <returns>A hash code for the given object.</returns>
        public static int GetHashCode(SharedAccessSignatureAuthorizationRule rule)
        {
            if (rule == null)
            {
                return 0;
            }

            int hashKeyName;
            int hashPrimaryKey;
            int hashSecondaryKey;
            int hashRights;

#if NETSTANDARD2_0_OR_GREATER || NET472
            hashKeyName = rule.KeyName == null ? 0 : rule.KeyName.GetHashCode();

            hashPrimaryKey = rule.PrimaryKey == null ? 0 : rule.PrimaryKey.GetHashCode();

            hashSecondaryKey = rule.SecondaryKey == null ? 0 : rule.SecondaryKey.GetHashCode();

            hashRights = rule.Rights.GetHashCode();
#else
            hashKeyName = rule.KeyName == null ? 0 : rule.KeyName.GetHashCode(StringComparison.InvariantCultureIgnoreCase);

            hashPrimaryKey = rule.PrimaryKey == null ? 0 : rule.PrimaryKey.GetHashCode(StringComparison.InvariantCultureIgnoreCase);

            hashSecondaryKey = rule.SecondaryKey == null ? 0 : rule.SecondaryKey.GetHashCode(StringComparison.InvariantCultureIgnoreCase);

            hashRights = rule.Rights.GetHashCode();
#endif

            return hashKeyName ^ hashPrimaryKey ^ hashSecondaryKey ^ hashRights;
        }

        /// <summary>
        /// Gets a hash code for the current object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return GetHashCode(this);
        }
    }
}
