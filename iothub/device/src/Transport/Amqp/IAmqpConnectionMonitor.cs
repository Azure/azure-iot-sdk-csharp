namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpConnectionMonitor
    {
        void OnConnectionIdle(IAmqpConnectionHolder amqpConnectionHolder, DeviceIdentity deviceIdentity);
    }
}