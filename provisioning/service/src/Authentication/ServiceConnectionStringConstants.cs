// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal static class ServiceConnectionStringConstants
    {
        internal const char ValuePairDelimiter = ';';
        internal const char ValuePairSeparator = '=';
        internal const string HostNameSeparator = ".";

        internal const string HostNamePropertyName = "HostName";
        internal const string SharedAccessKeyNamePropertyName = "SharedAccessKeyName";
        internal const string SharedAccessKeyPropertyName = "SharedAccessKey";
        internal const string SharedAccessSignaturePropertyName = "SharedAccessSignature";
    }
}
