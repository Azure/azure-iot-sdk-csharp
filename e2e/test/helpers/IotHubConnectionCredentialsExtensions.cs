// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal static class IotHubConnectionCredentialsExtensions
    {
        public static string GetIotHubConnectionString(this IotHubConnectionCredentials iotHubConnectionCredentials)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendKeyValuePairIfNotEmpty(IotHubConnectionStringConstants.HostNamePropertyName, iotHubConnectionCredentials.IotHubHostName);
            stringBuilder.AppendKeyValuePairIfNotEmpty(IotHubConnectionStringConstants.DeviceIdPropertyName, iotHubConnectionCredentials.DeviceId);
            stringBuilder.AppendKeyValuePairIfNotEmpty(IotHubConnectionStringConstants.ModuleIdPropertyName, iotHubConnectionCredentials.ModuleId);
            stringBuilder.AppendKeyValuePairIfNotEmpty(IotHubConnectionStringConstants.SharedAccessKeyNamePropertyName, iotHubConnectionCredentials.SharedAccessKeyName);
            stringBuilder.AppendKeyValuePairIfNotEmpty(IotHubConnectionStringConstants.SharedAccessKeyPropertyName, iotHubConnectionCredentials.SharedAccessKey);
            stringBuilder.AppendKeyValuePairIfNotEmpty(IotHubConnectionStringConstants.SharedAccessSignaturePropertyName, iotHubConnectionCredentials.SharedAccessSignature);
            stringBuilder.AppendKeyValuePairIfNotEmpty(IotHubConnectionStringConstants.GatewayHostNamePropertyName, iotHubConnectionCredentials.GatewayHostName);
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            return stringBuilder.ToString();
        }

        private static void AppendKeyValuePairIfNotEmpty(this StringBuilder builder, string name, object value)
        {
            if (value != null)
            {
                builder.Append(name);
                builder.Append(IotHubConnectionStringConstants.ValuePairSeparator);
                builder.Append(value);
                builder.Append(IotHubConnectionStringConstants.ValuePairDelimiter);
            }
        }
    }
}
