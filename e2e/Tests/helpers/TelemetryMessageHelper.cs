// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal class TelemetryMessageHelper
    {
        public static TelemetryMessage ComposeTestMessage(out string payload, out string p1Value)
        {
            return ComposeTestMessageOfSpecifiedSize(0, out payload, out p1Value);
        }

        public static TelemetryMessage ComposeTestMessageOfSpecifiedSize(int messageSize, out string payload, out string p1Value)
        {
            if (messageSize == 0)
            {
                messageSize = 1;
            }

            string messageId = Guid.NewGuid().ToString();
            payload = $"{Guid.NewGuid()}_{new string('*', messageSize)}";
            p1Value = Guid.NewGuid().ToString();
            string userId = Guid.NewGuid().ToString();

            VerboseTestLogger.WriteLine($"{nameof(ComposeTestMessageOfSpecifiedSize)}: messageId='{messageId}' payload='{payload}' p1Value='{p1Value}' messageSize= exceeds '{messageSize}'.");
            var message = new TelemetryMessage(payload)
            {
                MessageId = messageId,
            };
            message.Properties.Add("property1", p1Value);
            message.Properties.Add("property2", null);

            return message;
        }
    }
}
