using Microsoft.Azure.Amqp;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpSessionHolder : IStatusReportor, IDisposable
    {
        Task CloseAsync(TimeSpan timeout);
        void Close();

        Task<SendingAmqpLink> OpenSendingAmqpLinkAsync(
            byte? senderSettleMode,
            byte? receiverSettleMode,
            string deviceTemplate,
            string moduleTemplate,
            string linkSuffix,
            string CorrelationId,
            TimeSpan timeout
        );

        Task<ReceivingAmqpLink> OpenReceivingAmqpLinkAsync(
            byte? senderSettleMode,
            byte? receiverSettleMode,
            string deviceTemplate,
            string moduleTemplate,
            string linkSuffix,
            string CorrelationId,
            TimeSpan timeout
        );
    }
}
