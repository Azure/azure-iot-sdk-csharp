// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client.Transport.AmqpIoT;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class MessageConverterTests
    {
        [TestMethod]
        public void UpdateMessageHeaderAndPropertiesTest()
        {
            byte[] bytes = { 1, 2, 3, 4 };
            string messageId = Guid.NewGuid().ToString();
            string correlationId = Guid.NewGuid().ToString();
            string contentType = "application/json";
            string contentEncoding = "UTF-8";
            string to = "d1";
            var enqueuedTime = new DateTime(2018, 4, 5, 04, 05, 06, DateTimeKind.Utc);
            byte deliveryCount = 10;
            string messageSchema = "testSchema";
            string connectionDeviceId = "connD1";
            string connectionModuleId = "connM1";

            using (AmqpIoTMessage amqpIoTMessage =
                 new AmqpIoTMessage(new Amqp.Framing.Data { Value = new ArraySegment<byte>(bytes) }))
            {
                amqpIoTMessage.SetMessageId(messageId);
                amqpIoTMessage.SetCorrelationId(correlationId);
                amqpIoTMessage.SetContentType(contentType);
                amqpIoTMessage.SetContentEncoding(contentEncoding);
                amqpIoTMessage.SetTo(to);

                amqpIoTMessage.SetMessageAnnotations(MessageSystemPropertyNames.EnqueuedTime, enqueuedTime);
                amqpIoTMessage.SetMessageAnnotations(MessageSystemPropertyNames.DeliveryCount, deliveryCount);
                amqpIoTMessage.SetMessageAnnotations(MessageSystemPropertyNames.ConnectionDeviceId, connectionDeviceId);
                amqpIoTMessage.SetMessageAnnotations(MessageSystemPropertyNames.ConnectionModuleId, connectionModuleId);

                amqpIoTMessage.SetApplicationProperty(MessageSystemPropertyNames.MessageSchema, messageSchema);
            
                amqpIoTMessage.SetApplicationProperty("Prop1", "Value1");
                amqpIoTMessage.SetApplicationProperty("Prop2", "Value2");

                var message = new Message(bytes);

                AmqpIoTMessage.UpdateMessageHeaderAndProperties(amqpIoTMessage, message);

                Assert.AreEqual(messageId, message.MessageId);
                Assert.AreEqual(correlationId, message.CorrelationId);
                Assert.AreEqual(contentType, message.ContentType);
                Assert.AreEqual(contentEncoding, message.ContentEncoding);
                Assert.AreEqual(to, message.To);

                Assert.AreEqual(enqueuedTime, message.EnqueuedTimeUtc);
                Assert.AreEqual(deliveryCount, message.DeliveryCount);
                Assert.AreEqual(connectionDeviceId, message.ConnectionDeviceId);
                Assert.AreEqual(connectionModuleId, message.ConnectionModuleId);

                Assert.AreEqual(messageSchema, message.MessageSchema);
            
                Assert.AreEqual("Value1", message.Properties["Prop1"]);
                Assert.AreEqual("Value2", message.Properties["Prop2"]);
            }
        }
    }
}
