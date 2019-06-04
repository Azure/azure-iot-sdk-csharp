// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTOutcome
    {
        private Outcome Outcome;

        public AmqpIoTOutcome(Outcome outcome)
        {
            Outcome = outcome;
        }

        public void ThrowIfNotAccepted()
        {
            if (Outcome.DescriptorCode != Accepted.Code)
            {
                throw AmqpIoTErrorAdapter.GetExceptionFromOutcome(Outcome);
            }
        }

        public void ThrowIfError()
        {
            if (Outcome.DescriptorCode != Accepted.Code)
            {
                if (Outcome.DescriptorCode == Rejected.Code)
                {
                    var rejected = (Rejected)Outcome;

                    // Special treatment for NotFound amqp rejected error code in case of DisposeMessage 
                    if (rejected.Error != null && rejected.Error.Condition.Equals(AmqpErrorCode.NotFound))
                    {
                        Error error = new Error
                        {

                            Condition = AmqpIoTErrorAdapter.MessageLockLostError
                        };
                        throw AmqpIoTErrorAdapter.ToIotHubClientContract(error);
                    }
                }

                throw AmqpIoTErrorAdapter.GetExceptionFromOutcome(Outcome);
            }
        }
    }
}
