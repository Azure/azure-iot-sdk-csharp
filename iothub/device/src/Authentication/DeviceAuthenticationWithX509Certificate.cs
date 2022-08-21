// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses a X.509 certificate
    /// </summary>
    public sealed class DeviceAuthenticationWithX509Certificate : IAuthenticationMethod, IDisposable
    {
        private string _deviceId;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAuthenticationWithX509Certificate"/> class.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="certificate">X.509 Certificate.</param>
        /// <param name="chainCertificates">Certificates in the device certificate chain.</param>
        public DeviceAuthenticationWithX509Certificate(
            string deviceId,
            X509Certificate2 certificate,
            X509Certificate2Collection chainCertificates = null)
        {
            SetDeviceId(deviceId);
            Certificate = certificate;
            ChainCertificates = chainCertificates;
        }

        /// <summary>
        /// Gets or sets the device identifier.
        /// </summary>
        public string DeviceId
        {
            get => _deviceId;
            set => SetDeviceId(value);
        }

        /// <summary>
        /// Gets or sets the X.509 certificate associated with this device.
        /// The private key should be available in the <see cref="X509Certificate2"/> object,
        /// or should be available in the certificate store of the system where the client will be authenticated from.
        /// </summary>
        public X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// Full chain of certificates from the one used to sign the device certificate to the one uploaded to the
        /// service. Private keys are not required for these certificates.
        /// This is only supported on AMQP_Tcp_Only and Mqtt_Tcp_Only
        /// </summary>
        public X509Certificate2Collection ChainCertificates { get; }

        /// <summary>
        /// Populates a supplied instance based on the properties of the current instance.
        /// </summary>
        /// <param name="iotHubConnectionCredentials">Instance to populate.</param>
        /// <returns>The populated <see cref="IotHubConnectionCredentials"/> instance.</returns>
        public IotHubConnectionCredentials Populate(IotHubConnectionCredentials iotHubConnectionCredentials)
        {
            if (iotHubConnectionCredentials == null)
            {
                throw new ArgumentNullException(nameof(iotHubConnectionCredentials));
            }

            iotHubConnectionCredentials.DeviceId = DeviceId;
            iotHubConnectionCredentials.Certificate = Certificate;
            iotHubConnectionCredentials.ChainCertificates = ChainCertificates;
            iotHubConnectionCredentials.SharedAccessSignature = null;
            iotHubConnectionCredentials.SharedAccessKey = null;
            iotHubConnectionCredentials.SharedAccessKeyName = null;

            return iotHubConnectionCredentials;
        }

        private void SetDeviceId(string deviceId)
        {
            if (deviceId.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            _deviceId = deviceId;
        }

        /// <summary>
        /// The <see cref="Certificate"/> managed resource should be disposed by the user.
        /// This library intentionally does not dispose it here since the user might want to
        /// reuse the certificate instance elsewhere for some other operation.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
