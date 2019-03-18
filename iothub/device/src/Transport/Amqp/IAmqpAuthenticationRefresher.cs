using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpAuthenticationRefresher : IDisposable
    {
        Task InitLoopAsync(TimeSpan timeout);
        void StopLoop();
    }
}
