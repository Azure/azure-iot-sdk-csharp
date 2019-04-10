using System;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal enum Status
    {
        Disposed, Open, Closed
    }

    internal interface IStatusMonitor
    {
        void OnStatusChange(IStatusReportor statusReportor, Status status);
    }

    internal interface IStatusReportor
    {
        void AddStatusMonitor(IStatusMonitor statusMonitor);
        void DetachStatusMonitor(IStatusMonitor statusMonitor);
    }

}
