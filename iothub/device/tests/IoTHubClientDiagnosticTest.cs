// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.Azure.Devices.Client.Test
{
    using System;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Devices.Client.Transport.AmqpIot;
    using Microsoft.Azure.Devices.Client.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Unit")]
    public class IoTHubClientDiagnosticTest
    {
        [TestMethod]
        public void IoTHubClientDiagnostic_AddDiagnosticInfoIfNecessary_Test()
        {
            Message message = CreateMessage();
            const int DiagPercentageWithoutDiagnostic = 0;
            const int DiagPercentageWithDiagnostic = 100;

            int messageCount = 0;
            IotHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, DiagPercentageWithoutDiagnostic, ref messageCount);

            Assert.IsFalse(message.SystemProperties.ContainsKey(MessageSystemPropertyNames.DiagId));
            Assert.IsFalse(message.SystemProperties.ContainsKey(MessageSystemPropertyNames.DiagCorrelationContext));

            message = CreateMessage();
            messageCount = 0;
            IotHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, DiagPercentageWithDiagnostic, ref messageCount);
            Assert.IsTrue(message.SystemProperties.ContainsKey(MessageSystemPropertyNames.DiagId));
            Assert.IsTrue(message.SystemProperties.ContainsKey(MessageSystemPropertyNames.DiagCorrelationContext));
        }

        [TestMethod]
        public void IoTHubClientDiagnostic_CopyDiagnosticPropertiesToAmqpAnnotations_Test()
        {
            Message message = CreateMessage();
            const int DiagPercentageWithDiagnostic = 100;
            int messageCount = 0;
            IotHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, DiagPercentageWithDiagnostic, ref messageCount);
            AmqpMessage amqpMessage = AmqpIotMessageConverter.MessageToAmqpMessage(message);

            if (amqpMessage.MessageAnnotations.Map.TryGetValue("Diagnostic-Id", out string diagId))
            {
                Assert.IsTrue(message.SystemProperties[MessageSystemPropertyNames.DiagId] as string == diagId);
            }
            else
            {
                throw new AssertFailedException("Diagnostic-Id mismatch");
            }

            if (amqpMessage.MessageAnnotations.Map.TryGetValue("Correlation-Context", out string diagCorrelationContext))
            {
                Assert.IsTrue(message.SystemProperties[MessageSystemPropertyNames.DiagCorrelationContext] as string == diagCorrelationContext);
            }
            else
            {
                throw new AssertFailedException("Correlation-Context mismatch");
            }
        }

        [TestMethod]
        public void IoTHubClientDiagnostic_DiagId_Test()
        {
            Message message = CreateMessage();
            const int DiagPercentageWithDiagnostic = 100;
            int messageCount = 0;
            IotHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, DiagPercentageWithDiagnostic, ref messageCount);
            string diagId = message.SystemProperties[MessageSystemPropertyNames.DiagId].ToString();
            var r = new Regex("^[a-zA-Z0-9]{8}$");
            Assert.IsTrue(r.IsMatch(diagId));
        }

        [TestMethod]
        public void IoTHubClientDiagnostic_CorrelationContext_Test()
        {
            Message message = CreateMessage();
            const int DiagPercentageWithDiagnostic = 100;
            int messageCount = 0;
            IotHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, DiagPercentageWithDiagnostic, ref messageCount);

            string diagctx = message.SystemProperties[MessageSystemPropertyNames.DiagCorrelationContext].ToString();

            var r = new Regex("^creationtimeutc=\\d*\\.\\d*$");
            Assert.IsTrue(r.IsMatch(diagctx));

            NameValueCollection properties = HttpUtility.ParseQueryString(diagctx.Replace(",", "&"));
            string second = properties["creationtimeutc"];
            double epochTime = double.Parse(second, CultureInfo.InvariantCulture);
            DateTime creationTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epochTime);

            Assert.IsTrue((DateTime.UtcNow - creationTime).TotalSeconds < 60);
        }

        [TestMethod]
        public void IoTHubClientDiagnostic_SamplingPercentage_Test()
        {
            int percentage = 0;
            int count = 0;
            int messageCount = 0;
            for (int i = 1; i <= 100; i++)
            {
                Message message = CreateMessage();
                IotHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, percentage, ref messageCount);
                if (IotHubClientDiagnostic.HasDiagnosticProperties(message))
                {
                    count++;
                }
            }
            Assert.AreEqual(count, 0);

            count = 0;
            percentage = 10;
            messageCount = 0;
            for (int i = 1; i <= 100; i++)
            {
                Message message = CreateMessage();
                IotHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, percentage, ref messageCount);
                if (IotHubClientDiagnostic.HasDiagnosticProperties(message))
                {
                    Assert.IsTrue(i % 10 == 1);
                    count++;
                }
            }
            Assert.AreEqual(count, 10);

            count = 0;
            percentage = 20;
            messageCount = 0;
            for (int i = 1; i <= 100; i++)
            {
                Message message = CreateMessage();
                IotHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, percentage, ref messageCount);
                if (IotHubClientDiagnostic.HasDiagnosticProperties(message))
                {
                    Assert.IsTrue(i % 5 == 1);
                    count++;
                }
            }
            Assert.AreEqual(count, 20);

            count = 0;
            percentage = 50;
            messageCount = 0;
            for (int i = 1; i <= 100; i++)
            {
                Message message = CreateMessage();
                IotHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, percentage, ref messageCount);
                if (IotHubClientDiagnostic.HasDiagnosticProperties(message))
                {
                    Assert.IsTrue(i % 2 == 1);
                    count++;
                }
            }
            Assert.AreEqual(count, 50);

            count = 0;
            percentage = 70;
            messageCount = 0;
            for (int i = 1; i <= 100; i++)
            {
                Message message = CreateMessage();
                IotHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, percentage, ref messageCount);
                if (IotHubClientDiagnostic.HasDiagnosticProperties(message))
                {
                    count++;
                }
            }
            Assert.AreEqual(count, 70);

            count = 0;
            percentage = 100;
            messageCount = 0;
            for (int i = 1; i <= 100; i++)
            {
                Message message = CreateMessage();
                IotHubClientDiagnostic.AddDiagnosticInfoIfNecessary(message, percentage, ref messageCount);
                if (IotHubClientDiagnostic.HasDiagnosticProperties(message))
                {
                    count++;
                }
            }
            Assert.AreEqual(count, 100);
        }

        private Message CreateMessage()
        {
            const string MessageBody = "My Message";
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(MessageBody));
            var message = new Message(memoryStream);
            return message;
        }
    }
}
