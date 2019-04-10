namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpConnectionManager
    {
        IAmqpConnectionHolder AllocateAmqpConnectionHolder(DeviceIdentity deviceIdentity);
    }
}
