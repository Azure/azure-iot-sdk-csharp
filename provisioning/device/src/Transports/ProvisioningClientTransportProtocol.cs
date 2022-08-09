namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The protocol over which a transport (i.e., MQTT, AMQP) communicates.
    /// </summary>
    public enum ProvisioningClientTransportProtocol
    {
        /// <summary>
        /// Communicate over TCP using the default port of the transport.
        /// </summary>
        /// <remarks>
        /// For MQTT, this port is 8883.
        /// For AMQP, this port is 5671.
        /// </remarks>
        Tcp,

        /// <summary>
        /// Communicate over web socket using port 443.
        /// </summary>
        WebSocket,
    }
}
