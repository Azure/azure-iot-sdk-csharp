using System;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal interface IAmqpIoTCbsLink
    {
        void Close();
        Task<DateTime> SendTokenAsync(ICbsTokenProvider tokenProvider, Uri namespaceAddress, string audience, string resource, string[] requiredClaims, TimeSpan timeout);
    }
}