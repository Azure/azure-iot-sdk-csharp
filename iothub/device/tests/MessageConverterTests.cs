// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    public class MessageConverterTests
    {
        [TestCategory("CIT")]
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

            using (AmqpMessage amqpMessage =
                AmqpMessage.Create(new Amqp.Framing.Data { Value = new ArraySegment<byte>(bytes) }))
            {
                amqpMessage.Properties.MessageId = messageId;
                amqpMessage.Properties.CorrelationId = correlationId;
                amqpMessage.Properties.ContentType = contentType;
                amqpMessage.Properties.ContentEncoding = contentEncoding;
                amqpMessage.Properties.To = to;

                amqpMessage.MessageAnnotations.Map[MessageSystemPropertyNames.EnqueuedTime] = enqueuedTime;
                amqpMessage.MessageAnnotations.Map[MessageSystemPropertyNames.DeliveryCount] = deliveryCount;
                amqpMessage.MessageAnnotations.Map[MessageSystemPropertyNames.ConnectionDeviceId] = connectionDeviceId;
                amqpMessage.MessageAnnotations.Map[MessageSystemPropertyNames.ConnectionModuleId] = connectionModuleId;

                amqpMessage.ApplicationProperties.Map[MessageSystemPropertyNames.MessageSchema] = messageSchema;
            
                amqpMessage.ApplicationProperties.Map["Prop1"] = "Value1";
                amqpMessage.ApplicationProperties.Map["Prop2"] = "Value2";

                var message = new Message(bytes);

                MessageConverter.UpdateMessageHeaderAndProperties(amqpMessage, message);

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