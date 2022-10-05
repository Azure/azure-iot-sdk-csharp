// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    }
}
