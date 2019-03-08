// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Base class of DeviceClientEndpointIdentity
    /// Stores the common attributes
    /// - connection string 
    /// - transport settings 
    /// </summary>
    internal class DeviceIdentity
    {
        internal IotHubConnectionString IotHubConnectionString { get; }
        internal AmqpTransportSettings AmqpTransportSettings { get; }
        internal ProductInfo ProductInfo { get; }
        internal AuthenticationModel AuthenticationModel { get; }
        internal DeviceIdentity(IotHubConnectionString iotHubConnectionString, AmqpTransportSettings amqpTransportSettings, ProductInfo productInfo)
        {
            IotHubConnectionString = iotHubConnectionString;
            AmqpTransportSettings = amqpTransportSettings;
            ProductInfo = productInfo;
            if (amqpTransportSettings.ClientCertificate == null)
            {
                if (iotHubConnectionString.SharedAccessKeyName == null)
                {
                    AuthenticationModel = AuthenticationModel.SasIndividual;
                }
                else
                {
                    AuthenticationModel = AuthenticationModel.SasGrouped;
                }
            }
            else
            {
                AuthenticationModel = AuthenticationModel.X509;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is DeviceIdentity identity
                && GetHashCode() == identity.GetHashCode()
                && Equals(IotHubConnectionString.DeviceId, identity.IotHubConnectionString.DeviceId)
                && Equals(IotHubConnectionString.HostName, identity.IotHubConnectionString.HostName)
                && Equals(AmqpTransportSettings.GetTransportType(), identity.AmqpTransportSettings.GetTransportType())
                && Equals(AuthenticationModel.GetHashCode(), identity.AuthenticationModel.GetHashCode());
        }

        public override int GetHashCode()
        {
            int hashCode = UpdateHashCode(620602339, IotHubConnectionString.DeviceId);
            hashCode = UpdateHashCode(hashCode, IotHubConnectionString.HostName);
            hashCode = UpdateHashCode(hashCode, AmqpTransportSettings.GetTransportType());
            hashCode = UpdateHashCode(hashCode, AuthenticationModel);
            return hashCode;
        }

        private int UpdateHashCode(int hashCode, object field)
        {
            if (field == null)
            {
                return hashCode;
            }
            else
            {
                return hashCode * -1521134295 + field.GetHashCode();
            }

        }
    }
}