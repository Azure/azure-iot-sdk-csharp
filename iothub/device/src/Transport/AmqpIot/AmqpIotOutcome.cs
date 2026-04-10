// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotOutcome
    {
        private readonly Outcome _outcome;

        public AmqpIotOutcome(Outcome outcome)
        {
            _outcome = outcome;
        }

        public void ThrowIfNotAccepted()
        {
            if (_outcome.DescriptorCode != Accepted.Code)
            {
                throw AmqpIotErrorAdapter.GetExceptionFromOutcome(_outcome);
            }
        }

        public void ThrowIfError()
        {
            if (_outcome.DescriptorCode != Accepted.Code)
            {
                if (_outcome.DescriptorCode == Rejected.Code)
                {
                    var rejected = (Rejected)_outcome;

                    // Special treatment for NotFound amqp rejected error code in case of DisposeMessage
                    if (rejected.Error != null && rejected.Error.Condition.Equals(AmqpErrorCode.NotFound))
                    {
                        var error = new Error
                        {
                            Condition = AmqpIotErrorAdapter.MessageLockLostError,
                        };
                        throw AmqpIotErrorAdapter.ToIotHubClientContract(error);
                    }
                }

                throw AmqpIotErrorAdapter.GetExceptionFromOutcome(_outcome);
            }
        }
    }
}
