// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed class SharedAccessSignatureAuthorizationRule : IEquatable<SharedAccessSignatureAuthorizationRule>
    {
        private string _primaryKey;
        private string _secondaryKey;

        public string KeyName { get; set; }

        public string PrimaryKey
        {
            get => _primaryKey;

            set
            {
                StringValidationHelper.EnsureNullOrBase64String(value, "PrimaryKey");
                _primaryKey = value;
            }
        }

        public string SecondaryKey
        {
            get => _secondaryKey;

            set
            {
                StringValidationHelper.EnsureNullOrBase64String(value, "SecondaryKey");
                _secondaryKey = value;
            }
        }

        public AccessRights Rights { get; set; }

        public bool Equals(SharedAccessSignatureAuthorizationRule other)
        {
            if (other == null)
            {
                return false;
            }

            bool equals = string.Equals(KeyName, other.KeyName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(PrimaryKey, other.PrimaryKey, StringComparison.Ordinal) &&
                string.Equals(SecondaryKey, other.SecondaryKey, StringComparison.Ordinal) &&
                Equals(Rights, other.Rights);

            return equals;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SharedAccessSignatureAuthorizationRule);
        }

        private static int GetHashCode(SharedAccessSignatureAuthorizationRule rule)
        {
            if (rule == null)
            {
                return 0;
            }

            int hashKeyName, hashPrimaryKey, hashSecondaryKey, hashRights;

#if NETSTANDARD2_0 || NET472
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

        public override int GetHashCode()
        {
            return GetHashCode(this);
        }
    }
}
