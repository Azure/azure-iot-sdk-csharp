﻿// Copyright (c) Microsoft. All rights reserved.
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
        public IotHubConnectionString IotHubConnectionString { get; }
        public AmqpTransportSettings AmqpTransportSettings { get; }
        public ProductInfo ProductInfo { get; }
        public AuthenticationModel AuthenticationModel { get; }
        public string Audience { get; }
        public ClientOptions Options { get; }

        internal DeviceIdentity(
            IotHubConnectionString iotHubConnectionString,
            AmqpTransportSettings amqpTransportSettings,
            ProductInfo productInfo,
            ClientOptions options)
        {
            IotHubConnectionString = iotHubConnectionString;
            AmqpTransportSettings = amqpTransportSettings;
            ProductInfo = productInfo;
            Options = options;

            if (amqpTransportSettings.ClientCertificate == null)
            {
                Audience = CreateAudience(IotHubConnectionString);
                AuthenticationModel = iotHubConnectionString.SharedAccessKeyName == null
                    ? AuthenticationModel.SasIndividual
                    : AuthenticationModel.SasGrouped;
            }
            else
            {
                AuthenticationModel = AuthenticationModel.X509;
            }
        }

        private static string CreateAudience(IotHubConnectionString connectionString)
        {
            if (connectionString.SharedAccessKeyName.IsNullOrWhiteSpace())
            {
                return connectionString.ModuleId.IsNullOrWhiteSpace()
                    ? $"{connectionString.HostName}/devices/{WebUtility.UrlEncode(connectionString.DeviceId)}"
                    : $"{connectionString.HostName}/devices/{WebUtility.UrlEncode(connectionString.DeviceId)}/modules/{WebUtility.UrlEncode(connectionString.ModuleId)}";
            }
            else
            {
                // this is a group shared key
                return connectionString.HostName;
            }
        }

        public bool IsPooling()
        {
            return (AuthenticationModel != AuthenticationModel.X509) && (AmqpTransportSettings?.AmqpConnectionPoolSettings?.Pooling ?? false);
        }

        public override bool Equals(object obj)
        {
            return obj is DeviceIdentity identity
                && GetHashCode() == identity.GetHashCode()
                && Equals(IotHubConnectionString.DeviceId, identity.IotHubConnectionString.DeviceId)
                && Equals(IotHubConnectionString.HostName, identity.IotHubConnectionString.HostName)
                && Equals(IotHubConnectionString.ModuleId, identity.IotHubConnectionString.ModuleId)
                && Equals(AmqpTransportSettings.GetTransportType(), identity.AmqpTransportSettings.GetTransportType())
                && Equals(AuthenticationModel.GetHashCode(), identity.AuthenticationModel.GetHashCode());
        }

        public override int GetHashCode()
        {
            int hashCode = UpdateHashCode(620602339, IotHubConnectionString.DeviceId);
            hashCode = UpdateHashCode(hashCode, IotHubConnectionString.HostName);
            hashCode = UpdateHashCode(hashCode, IotHubConnectionString.ModuleId);
            hashCode = UpdateHashCode(hashCode, AmqpTransportSettings.GetTransportType());
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
