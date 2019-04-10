// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static class MessageReceive
    {
        private static TestLogging s_log = TestLogging.GetInstance();

        public static Message ComposeC2DTestMessage(out string payload, out string messageId, out string p1Value)
        {
            payload = Guid.NewGuid().ToString();
            messageId = Guid.NewGuid().ToString();
            p1Value = Guid.NewGuid().ToString();

            s_log.WriteLine($"{nameof(ComposeC2DTestMessage)}: payload='{payload}' messageId='{messageId}' p1Value='{p1Value}'");

            return new Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                Properties = { ["property1"] = p1Value }
            };
        }

        public static async Task VerifyReceivedC2DMessageAsync(Client.TransportType transport, DeviceClient dc, string payload, string p1Value)
        {
            bool received;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            Client.Message receivedMessage;
            do
            {
                receivedMessage = await dc.ReceiveAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                received = MatchMessage(receivedMessage, payload, p1Value);
            }
            while (receivedMessage != null && !received);

            Assert.IsTrue(received, $"No message was recevied with payload: {payload}");

            if (sw.Elapsed.TotalMilliseconds > FaultInjection.RecoveryTimeMilliseconds)
            {
                throw new TimeoutException("Test is running longer than expected.");
            }

            sw.Stop();
           
        }

        private static bool MatchMessage(Client.Message message, string payload, string p1Value)
        {
            if (message != null)
            {
                s_log.WriteLine($"{nameof(MatchMessage)}: {message} with expected payload = {payload} and property[property1] = {p1Value}");
                string messageData = Encoding.ASCII.GetString(message.GetBytes());
                if (Equals(payload, messageData) && message.Properties.Count == 1)
                {
                    var prop = message.Properties.Single();
                    return Equals("property1", prop.Key) && Equals(p1Value, prop.Value);

                }
            }
            return false;
        }
    }
}
