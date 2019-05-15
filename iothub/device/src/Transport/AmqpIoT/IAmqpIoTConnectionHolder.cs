using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Transport.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal interface IAmqpIoTConnectionHolder
    {
        event EventHandler OnConnectionDisconnected;

        AmqpIoTUnit CreateAmqpUnit(DeviceIdentity deviceIdentity, Func<MethodRequestInternal, Task> methodHandler, Action<AmqpIoTMessage> twinMessageListener, Func<string, Message, Task> eventListener);
        void Dispose();
        int GetNumberOfUnits();
    }
}