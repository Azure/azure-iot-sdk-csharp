using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.DigitalTwin.Model.Service
{
    internal static class CommonExtensionMethods
    {
        private const char KeyValuePairDelimiter = ';';
        private const char KeyValuePairSeparator = '=';

        public static IDictionary<string, string> ToDictionary(this string keyValuePairString, char kvpDelimiter, char kvpSeparator)
        {
            if (string.IsNullOrWhiteSpace(keyValuePairString))
            {
                throw new ArgumentException("Missing Token");
            }

            IEnumerable<string[]> parts = keyValuePairString.Split(kvpDelimiter).
                Select((part) => part.Split(new char[] { kvpSeparator }, 2));

            if (parts.Any((part) => part.Length != 2))
            {
                throw new FormatException("Incorrect Token");
            }

            IDictionary<string, string> map = parts.ToDictionary((kvp) => kvp[0], (kvp) => kvp[1], StringComparer.OrdinalIgnoreCase);

            return map;
        }

        public static void AppendKeyValuePairIfNotEmpty(this StringBuilder builder, string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                builder.Append(name);
                builder.Append(KeyValuePairSeparator);
                builder.Append(value);
                builder.Append(KeyValuePairDelimiter);
            }
        }
    }
}
