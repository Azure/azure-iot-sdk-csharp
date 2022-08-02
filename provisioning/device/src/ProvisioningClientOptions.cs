using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Options that allow configuration of the Provisioning device client instance during initialization.
    /// </summary>
    public class ProvisioningClientOptions
    {
        /// <summary>
        /// Creates an instances of this class with the default transport settings.
        /// </summary>
        public ProvisioningClientOptions()
        {
            ProvisioningTransportSettings = new ProvisioningTransportHandlerAmqp();
        }

        /// <summary>
        /// Creates an instance of this class with the specified transport settings.
        /// </summary>
        /// <param name="transportHandler">The transport settings to use (i.e., <see cref="ProvisioningTransportHandlerAmqp"/>,
        /// <see cref="ProvisioningTransportHandlerMqtt"/>, or <see cref="ProvisioningTransportHandlerHttp"/>).</param>
        /// <exception cref="ArgumentNullException">When <paramref name="transportHandler"/> is null.</exception>
        public ProvisioningClientOptions(ProvisioningTransportHandler transportHandler)
        {
            ProvisioningTransportSettings = transportHandler ?? throw new ArgumentNullException(nameof(transportHandler));
        }

        /// <summary>
        /// The transport settings to use (i.e., <see cref="ProvisioningTransportHandlerAmqp"/>, <see cref="ProvisioningTransportHandlerMqtt"/>, or <see cref="ProvisioningTransportHandlerHttp"/>).
        /// </summary>
        public ProvisioningTransportHandler ProvisioningTransportSettings { get; }
    }
}
