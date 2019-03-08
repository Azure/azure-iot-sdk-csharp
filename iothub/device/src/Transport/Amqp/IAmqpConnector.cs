using Microsoft.Azure.Amqp;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpConnector : IDisposable
    {
        Task<AmqpConnection> OpenConnectionAsync(TimeSpan timeout);
    }
}
