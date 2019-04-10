using Microsoft.Azure.Amqp;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpUnitManager
    {
        IAmqpUnit CreateAmqpUnit(
            DeviceIdentity deviceIdentity,
            Func<MethodRequestInternal, Task> methodHandler,
            Action<AmqpMessage> twinMessageListener,
            Func<string, Message, Task> eventListener,
            Action<bool> onUnitDisconnected);
    }
}
