// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.Azure.Devices.Client.Test.Mqtt
{
    using DotNetty.Codecs.Mqtt.Packets;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class UtilTest
    {
        [TestMethod]
        public void TestPopulateMessagePropertiesFromPacket_NormalMessage()
        {
            var message = new Message();
            var publishPacket = new PublishPacket(QualityOfService.AtMostOnce, false, false)
            {
                PacketId = 0,
                TopicName = "devices/d10/messages/devicebound/%24.cid=Corrid1&%24.mid=MessageId1&Prop1=Value1&Prop2=Value2&Prop3=Value3/"
            };

            Util.PopulateMessagePropertiesFromPacket(message, publishPacket);
            Assert.AreEqual(3, message.Properties.Count);
            Assert.AreEqual("Value1", message.Properties["Prop1"]);
            Assert.AreEqual("Value2", message.Properties["Prop2"]);
            Assert.AreEqual("Value3", message.Properties["Prop3"]);

            Assert.AreEqual(3, message.SystemProperties.Count);
            Assert.AreEqual("Corrid1", message.SystemProperties["correlation-id"]);
            Assert.AreEqual("MessageId1", message.SystemProperties["message-id"]);
        }

        [TestMethod]
        public void TestPopulateMessagePropertiesFromPacket_ModuleEndpointMessage()
        {
            var message = new Message();
            var publishPacket = new PublishPacket(QualityOfService.AtMostOnce, false, false)
            {
                PacketId = 0,
                TopicName = "devices/d10/modules/m3/endpoints/in2/%24.cid=Corrid1&%24.mid=MessageId1&Prop1=Value1&Prop2=Value2&Prop3=Value3/"
            };

            Util.PopulateMessagePropertiesFromPacket(message, publishPacket);
            Assert.AreEqual(3, message.Properties.Count);
            Assert.AreEqual("Value1", message.Properties["Prop1"]);
            Assert.AreEqual("Value2", message.Properties["Prop2"]);
            Assert.AreEqual("Value3", message.Properties["Prop3"]);

            Assert.AreEqual(3, message.SystemProperties.Count);
            Assert.AreEqual("Corrid1", message.SystemProperties["correlation-id"]);
            Assert.AreEqual("MessageId1", message.SystemProperties["message-id"]);
        }
    }
}