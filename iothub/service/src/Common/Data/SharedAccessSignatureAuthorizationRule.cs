// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Common.Data
{
    public sealed class SharedAccessSignatureAuthorizationRule : IEquatable<SharedAccessSignatureAuthorizationRule>
    {
        private string _primaryKey;
        private string _secondaryKey;

        [JsonProperty(PropertyName = "keyName")]
        public string KeyName { get; set; }

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

        [JsonProperty(PropertyName = "rights")]
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
            return obj is SharedAccessSignatureAuthorizationRule
                ? Equals(obj)
                : throw new InvalidOperationException($"{nameof(obj)} should be an instance of {nameof(SharedAccessSignatureAuthorizationRule)} for comparison");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Globalization", "CA1307:Specify StringComparison",
            Justification = "string.GetHashCode(StringComparison) is not supported for .net standard 2.0")]
        // https://docs.microsoft.com/en-us/dotnet/api/system.string.gethashcode?view=net-5.0#System_String_GetHashCode_System_StringComparison_
        public int GetHashCode(SharedAccessSignatureAuthorizationRule rule)
        {
            if (rule == null)
            {
                return 0;
            }

            int hashKeyName = rule.KeyName == null ? 0 : rule.KeyName.GetHashCode();

            int hashPrimaryKey = rule.PrimaryKey == null ? 0 : rule.PrimaryKey.GetHashCode();

            int hashSecondaryKey = rule.SecondaryKey == null ? 0 : rule.SecondaryKey.GetHashCode();

            int hashRights = rule.Rights.GetHashCode();

            return hashKeyName ^ hashPrimaryKey ^ hashSecondaryKey ^ hashRights;
        }

        public override int GetHashCode()
        {
            return GetHashCode(this);
        }
    }
}
