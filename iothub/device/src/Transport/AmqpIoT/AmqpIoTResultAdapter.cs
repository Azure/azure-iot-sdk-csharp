
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal enum AmqpIoTDisposeActions
    {
        Accepted,
        Released,
        Rejected
    }

    internal static class AmqpIoTResultAdapter
    {
        internal static Outcome GetResult(AmqpIoTDisposeActions amqpIoTConstants)
        {
            switch (amqpIoTConstants)
            {
                case AmqpIoTDisposeActions.Accepted:
                    return new Accepted();
                case AmqpIoTDisposeActions.Released:
                    return new Released();
                case AmqpIoTDisposeActions.Rejected:
                    return new Rejected();
                default:
                    return null;
            }
        }
    }
}
