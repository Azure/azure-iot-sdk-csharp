// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests.Transport.Mqtt
{
    [TestClass]
    [TestCategory("Unit")]
    public class UrlEncodedDictionarySerializerTests
    {
        [TestMethod]
        public void UrlEncodedDictionarySerializer_Deserialize_Serialize()
        {
            // arrange and act
            const string propertiesSegment = "%24.to=%2Fdevices%2FdelMe%2Fmessages%2FdeviceBound&%24.ct=text%2Fplain%3B%20charset%3DUTF-8&%24.ce=utf-8";
            Dictionary<string, string> properties = UrlEncodedDictionarySerializer.Deserialize(propertiesSegment, 0);

            // assert
            properties.Count.Should().Be(3);
            properties.ContainsKey("$.to").Should().BeTrue();
            properties.ContainsKey("$.ct").Should().BeTrue();
            properties.ContainsKey("$.ce").Should().BeTrue();
            properties.ContainsKey("$.tm").Should().BeFalse();
            properties["$.to"].Should().Be("/devices/delMe/messages/deviceBound");
            properties["$.ct"].Should().Be("text/plain; charset=UTF-8");
            properties["$.ce"].Should().Be("utf-8");

            // act
            string mergedProperties = UrlEncodedDictionarySerializer.Serialize(properties);
            
            // assert
            mergedProperties.Should().Be(propertiesSegment);
        }

        [TestMethod]
        public void UrlEncodedDictionarySerializer_Deserialize_ArgumentNull_Throws() 
        {
            Action act = () => UrlEncodedDictionarySerializer.Deserialize(null, 0, null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void UrlEncodedDictionarySerializer_Deserialize_InvalidStartIndex_DoesNotThrow()
        {
            //act
            const string propertiesSegment = "%24.to=%2Fdevices%2FdelMe%2Fmessages%2FdeviceBound&%24.ct=text%2Fplain%3B%20charset%3DUTF-8&%24.ce=utf-8";
            Dictionary<string, string> properties = UrlEncodedDictionarySerializer.Deserialize(propertiesSegment, 300);

            // assert
            properties.Count.Should().Be(0);
        }

        [TestMethod]
        public void UrlEncodedDictionarySerializer_Serialize_Empty_DoesNotThrows()
        {
            string mergedProperties = UrlEncodedDictionarySerializer.Serialize(new Dictionary<string, string>());
            mergedProperties.Should().BeEmpty();

            mergedProperties = UrlEncodedDictionarySerializer.Serialize(new Dictionary<string, string>() { { "key", null } });
            mergedProperties.Should().Be("key");

            mergedProperties = UrlEncodedDictionarySerializer.Serialize(new Dictionary<string, string>() { { "key", "value" } });
            mergedProperties.Should().Be("key=value");
        }
    }
}
