// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses an X.509 certificate
    /// </summary>
    public sealed class ClientAuthenticationWithX509Certificate : IAuthenticationMethod
    {
        private string _deviceId;
        private string _moduleId;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <remarks>
        /// The <paramref name="clientCertificate"/> managed resource should be disposed by the user.
        /// This class doesn't dispose it since the user might want to reuse it.
        /// </remarks>
        /// <param name="clientCertificate">X.509 certificate.</param>
        /// <param name="certificateChain">Certificates in the device certificate chain.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="moduleId">Module identifier.</param>
        /// <exception cref="ArgumentException">When <paramref name="clientCertificate"/> or <paramref name="certificateChain"/> is null.</exception>
        public ClientAuthenticationWithX509Certificate(
            X509Certificate2 clientCertificate,
            X509Certificate2Collection certificateChain,
            string deviceId,
            string moduleId = default)
        {
            DeviceId = deviceId;
            ModuleId = moduleId;
            ClientCertificate = clientCertificate
                ?? throw new ArgumentException("No certificate was found. To use certificate authentication certificate must be present.", nameof(clientCertificate));
            CertificateChain = certificateChain
                ?? throw new ArgumentException("No certificate chain was found.", nameof(certificateChain));
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <remarks>
        /// The <paramref name="certificate"/> managed resource should be disposed by the user.
        /// This class doesn't dispose it since the user might want to reuse it.
        /// </remarks>
        /// <param name="certificate">X.509 certificate.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="moduleId">Module identifier.</param>
        /// <exception cref="ArgumentException">When <paramref name="certificate"/> is null.</exception>
        public ClientAuthenticationWithX509Certificate(
            X509Certificate2 certificate,
            string deviceId,
            string moduleId = default)
        {
            DeviceId = deviceId;
            ModuleId = moduleId;
            ClientCertificate = certificate
                ?? throw new ArgumentException("No certificate was found. To use certificate authentication certificate must be present.", nameof(certificate));
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
        /// Gets or sets the module identifier.
        /// </summary>
        public string ModuleId
        {
            get => _moduleId;
            set => SetModuleId(value);
        }

        /// <summary>
        /// The X.509 certificate associated with this device.
        /// The private key should be available in the <see cref="X509Certificate2"/> object,
        /// or should be available in the certificate store of the system where the client will be authenticated from.
        /// </summary>
        public X509Certificate2 ClientCertificate { get; }

        /// <summary>
        /// Full chain of certificates from the one used to sign the device certificate to the one uploaded to the
        /// service. Private keys are not required for these certificates.
        /// This is only supported on AMQP_Tcp_Only and Mqtt_Tcp_Only
        /// </summary>
        public X509Certificate2Collection CertificateChain { get; }

        /// <summary>
        /// Populates a supplied instance based on the properties of the current instance.
        /// </summary>
        /// <param name="iotHubConnectionCredentials">Instance to populate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="iotHubConnectionCredentials"/> is null.</exception>
        public void Populate(ref IotHubConnectionCredentials iotHubConnectionCredentials)
        {
            Argument.AssertNotNull(iotHubConnectionCredentials, nameof(iotHubConnectionCredentials));

            iotHubConnectionCredentials.DeviceId = DeviceId;
            iotHubConnectionCredentials.ModuleId = ModuleId;
            iotHubConnectionCredentials.ClientCertificate = ClientCertificate;
            iotHubConnectionCredentials.CertificateChain = CertificateChain;
            iotHubConnectionCredentials.SharedAccessSignature = null;
            iotHubConnectionCredentials.SharedAccessKey = null;
            iotHubConnectionCredentials.SharedAccessKeyName = null;
        }

        private void SetDeviceId(string deviceId)
        {
            if (deviceId.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("Device Id cannot be null or white space.");
            }

            _deviceId = deviceId;
        }

        private void SetModuleId(string moduleId)
        {
            // The module Id is optional so we only check whether it is whitespace or not here.
            if (moduleId != null
                && moduleId.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("Module Id cannot be white space.");
            }

            _moduleId = moduleId;
        }
    }
}
