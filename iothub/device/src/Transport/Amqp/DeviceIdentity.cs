// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    /// <summary>
    /// Device configurations
    /// Stores the common attributes
    /// - connection string
    /// - transport settings
    /// </summary>
    internal class DeviceIdentity : IDeviceIdentity
    {
        internal DeviceIdentity(
            IotHubConnectionInfo connectionInfo,
            IotHubClientAmqpSettings amqpTransportSettings,
            ProductInfo productInfo,
            IotHubClientOptions options)
        {
            IotHubConnectionInfo = connectionInfo;
            AmqpTransportSettings = amqpTransportSettings;
            ProductInfo = productInfo;
            Options = options;

            if (amqpTransportSettings.ClientCertificate == null)
            {
                Audience = CreateAudience(IotHubConnectionInfo);
                AuthenticationModel = connectionInfo.SharedAccessKeyName == null
                    ? AuthenticationModel.SasIndividual
                    : AuthenticationModel.SasGrouped;
            }
            else
            {
                AuthenticationModel = AuthenticationModel.X509;
            }
        }

        public IotHubConnectionInfo IotHubConnectionInfo { get; }

        public IotHubClientAmqpSettings AmqpTransportSettings { get; }

        public ProductInfo ProductInfo { get; }

        public AuthenticationModel AuthenticationModel { get; }

        public string Audience { get; }

        public IotHubClientOptions Options { get; }

        private static string CreateAudience(IotHubConnectionInfo connectionInfo)
        {
            if (connectionInfo.SharedAccessKeyName.IsNullOrWhiteSpace())
            {
                return connectionInfo.ModuleId.IsNullOrWhiteSpace()
                    ? $"{connectionInfo.HostName}/devices/{WebUtility.UrlEncode(connectionInfo.DeviceId)}"
                    : $"{connectionInfo.HostName}/devices/{WebUtility.UrlEncode(connectionInfo.DeviceId)}/modules/{WebUtility.UrlEncode(connectionInfo.ModuleId)}";
            }
            else
            {
                // this is a group shared key
                return connectionInfo.HostName;
            }
        }

        public bool IsPooling()
        {
            return (AuthenticationModel != AuthenticationModel.X509) && (AmqpTransportSettings?.ConnectionPoolSettings?.Pooling ?? false);
        }

        public override bool Equals(object obj)
        {
            return obj is DeviceIdentity identity
                && GetHashCode() == identity.GetHashCode()
                && Equals(IotHubConnectionInfo.DeviceId, identity.IotHubConnectionInfo.DeviceId)
                && Equals(IotHubConnectionInfo.HostName, identity.IotHubConnectionInfo.HostName)
                && Equals(IotHubConnectionInfo.ModuleId, identity.IotHubConnectionInfo.ModuleId)
                && Equals(AmqpTransportSettings.Protocol, identity.AmqpTransportSettings.Protocol)
                && Equals(AuthenticationModel.GetHashCode(), identity.AuthenticationModel.GetHashCode());
        }

        public override int GetHashCode()
        {
            int hashCode = UpdateHashCode(620602339, IotHubConnectionInfo.DeviceId);
            hashCode = UpdateHashCode(hashCode, IotHubConnectionInfo.HostName);
            hashCode = UpdateHashCode(hashCode, IotHubConnectionInfo.ModuleId);
            hashCode = UpdateHashCode(hashCode, AmqpTransportSettings.Protocol);
            hashCode = UpdateHashCode(hashCode, AuthenticationModel);
            return hashCode;
        }

        private static int UpdateHashCode(int hashCode, object field)
        {
            return field == null
                ? hashCode
                : hashCode * -1521134295 + field.GetHashCode();
        }
    }
}
