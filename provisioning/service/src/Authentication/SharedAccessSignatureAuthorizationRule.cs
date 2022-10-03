// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal sealed class SharedAccessSignatureAuthorizationRule : IEquatable<SharedAccessSignatureAuthorizationRule>
    {
        private string _primaryKey;
        private string _secondaryKey;

        public string KeyName { get; set; }

        public string PrimaryKey
        {
            get => _primaryKey;
            set => _primaryKey = value;
        }

        public string SecondaryKey
        {
            get => _secondaryKey;
            set => _secondaryKey = value;
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

        public override int GetHashCode()
        {
            return (_primaryKey + _secondaryKey).GetHashCode();
        }
    }
}
