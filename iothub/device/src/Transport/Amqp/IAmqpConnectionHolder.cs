using Microsoft.Azure.Amqp;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpConnectionHolder
    {
        #region EventHandler
        event EventHandler OnConnectionDisconnected;
        #endregion

        #region Manage Unit
        IAmqpUnit CreateAmqpUnit(
            DeviceIdentity deviceIdentity,
            Func<MethodRequestInternal, Task> methodHandler, 
            Action<AmqpMessage> desiredPropertyListener, 
            Func<string, Message, Task> messageListener);
        int GetNumberOfUnits();
        #endregion
    }
}
