// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common.Security
{
    /// <summary>
    /// Security-related constants.
    /// </summary>
    public static class SecurityConstants
    {
        /// <summary>
        /// The scheme name for bearer token auth.
        /// </summary>
        public const string BearerTokenScheme = "Bearer";

        /// <summary>
        /// The scheme name to use for certificate authentication handler.
        /// </summary>
        public const string CertificateScheme = "Certificate";

        /// <summary>
        /// The claim type for the authentication scheme.
        /// </summary>
        public const string AuthenticationScheme = "Scheme";

        /// <summary>
        /// The role capable of invoking ResourceProvider from CSM.
        /// </summary>
        public const string ResourceProviderClientAccessRole = "ResourceProviderClientAccess";

        /// <summary>
        /// The role capable of invoking ResourceProvider from ACIS.
        /// </summary>
        public const string ResourceProviderAdminAccessRole = "ResourceProviderAdminAccess";

        /// <summary>
        /// For use when giving permissions to both resource provider roles.
        /// </summary>
        public const string ResourceProviderFullAccessRole = ResourceProviderAdminAccessRole + "," + ResourceProviderClientAccessRole;

        /// <summary>
        /// Authentication failed message.
        /// </summary>
        public const string NonSecureConnection = "Connection does not use secure sockets (i.e. HTTPS)";

        /// <summary>
        /// Authentication failed message.
        /// </summary>
        public const string AuthenticationFailed = "Authentication failed for the request";

        /// <summary>
        /// Authorization failed message.
        /// </summary>
        public const string AuthorizationFailed = "Authorization failed for the request";

        /// <summary>
        /// Missing certificate message.
        /// </summary>
        public const string MissingCertificate = "Missing client certificate";

        /// <summary>
        /// Default size of device gateway and device keys.
        /// </summary>
        public const int DefaultKeyLengthInBytes = 32;

        /// <summary>
        /// Minimum size of device gateway and device keys.
        /// </summary>
        public const int MinKeyLengthInBytes = 16;

        /// <summary>
        /// Maximum size of device gateway and device keys.
        /// </summary>
        public const int MaxKeyLengthInBytes = 64;

        /// <summary>
        /// The name of the authentication challenge response header.
        /// </summary>
        public const string WwwAuthenticateHeader = "WWW-Authenticate";

        /// <summary>
        /// Default owner SAS key name.
        /// </summary>
        public const string DefaultOwnerSaSKeyName = "iothubowner";

        /// <summary>
        /// Default service SAS key name.
        /// </summary>
        public const string DefaultServiceSaSKeyName = "service";

        /// <summary>
        /// Default device SAS key name.
        /// </summary>
        public const string DefaultDeviceSaSKeyName = "device";

        /// <summary>
        /// Default device registry read key name.
        /// </summary>
        public const string DefaultRegistryReadSaSKeyName = "registryRead";

        /// <summary>
        /// Default device registry ReadWrite key name.
        /// </summary>
        public const string DefaultRegistryReadWriteSaSKeyName = "registryReadWrite";

        /// <summary>
        /// Admin SAS key name.
        /// </summary>
        public const string AdminSaSKeyName = "admin";

        /// <summary>
        /// SaS Key length.
        /// </summary>
        public const int SaSKeyLength = 32;

        /// <summary>
        /// Maximum SAS key name length.
        /// </summary>
        public const int SasKeyNameMaxLength = 64;

        // Shared access key constants

        /// <summary>
        /// Shared access key.
        /// </summary>
        public const string SharedAccessKey = "SharedAccessKey";

        /// <summary>
        /// Shared access key field name.
        /// </summary>
        public const string SharedAccessKeyFieldName = "sk";

        /// <summary>
        /// Shared access key full field name.
        /// </summary>
        public const string SharedAccessKeyFullFieldName = SharedAccessKey + " " + SharedAccessKeyFieldName + "=";
    }
}
