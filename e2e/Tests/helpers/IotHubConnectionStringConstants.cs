// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal static class IotHubConnectionStringConstants
    {
        public const char ValuePairDelimiter = ';';
        public const char ValuePairSeparator = '=';
        public const string HostNamePropertyName = "HostName";
        public const string GatewayHostNamePropertyName = "GatewayHostName";
        public const string DeviceIdPropertyName = "DeviceId";
        public const string ModuleIdPropertyName = "ModuleId";
        public const string SharedAccessKeyNamePropertyName = "SharedAccessKeyName";
        public const string SharedAccessKeyPropertyName = "SharedAccessKey";
        public const string SharedAccessSignaturePropertyName = "SharedAccessSignature";
    }
}
