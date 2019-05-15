using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal interface IAmqpIoTAuthenticationRefresher
    {
        void Dispose();
        Task InitLoopAsync(TimeSpan timeout);
        void StopLoop();
    }
}