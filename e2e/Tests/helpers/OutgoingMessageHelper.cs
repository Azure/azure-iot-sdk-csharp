// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal static class OutgoingMessageHelper
    {
        public static OutgoingMessage ComposeOutgoingTestMessage(out string payload, out string p1Value)
        {
            payload = Guid.NewGuid().ToString();
            p1Value = Guid.NewGuid().ToString();
            string messageId = Guid.NewGuid().ToString();
            string userId = Guid.NewGuid().ToString();

            var message = new OutgoingMessage(payload)
            {
                MessageId = messageId,
                UserId = userId,
                Properties = { ["property1"] = p1Value }
            };

            return message;
        }
    }
}
