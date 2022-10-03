﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses a X.509 certificate
    /// </summary>
    public sealed class DeviceAuthenticationWithX509Certificate : IAuthenticationMethod
    {
        private string _deviceId;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <remarks>
        /// The <paramref name="certificate"/> managed resource should be disposed by the user.
        /// This class doesn't dispose it since the user might want to reuse it.
        /// </remarks>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="certificate">X.509 certificate.</param>
        /// <param name="chainCertificates">Certificates in the device certificate chain.</param>
        /// <exception cref="ArgumentException">When <paramref name="certificate"/> is null.</exception>
        public DeviceAuthenticationWithX509Certificate(
            string deviceId,
            X509Certificate2 certificate,
            X509Certificate2Collection chainCertificates = null)
        {
            SetDeviceId(deviceId);
            Certificate = certificate
                ?? throw new ArgumentException("No certificate was found. To use certificate authentication certificate must be present.", nameof(certificate));
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
        /// The X.509 certificate associated with this device.
        /// The private key should be available in the <see cref="X509Certificate2"/> object,
        /// or should be available in the certificate store of the system where the client will be authenticated from.
        /// </summary>
        public X509Certificate2 Certificate { get; }

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
                throw new InvalidOperationException("Device Id cannot be null or white space.");
            }

            _deviceId = deviceId;
        }
    }
}
