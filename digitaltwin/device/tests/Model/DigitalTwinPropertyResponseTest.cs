// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.DigitalTwin.Client.Model;
using Xunit;

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Test.Model
{
    [Trait("TestCategory", "DigitalTwin")]
    [Trait("TestCategory", "Unit")]
    public class DigitalTwinPropertyResponseTest
    {
        [Fact]
        public void TestConstructor1()
        {
            var propertyResponse = new DigitalTwinPropertyResponse(888, 500, "StatusDescription");

            Assert.Equal(888, propertyResponse.RespondVersion);
            Assert.Equal(500, propertyResponse.StatusCode);
            Assert.Equal("StatusDescription", propertyResponse.StatusDescription);
        }

        [Fact]
        public void TestConstructor2()
        {
            var propertyResponse = new DigitalTwinPropertyResponse();

            Assert.Equal(0, propertyResponse.RespondVersion);
            Assert.Equal(0, propertyResponse.StatusCode);
            Assert.Null(propertyResponse.StatusDescription);
        }

        [Fact]
        public void TestConstructorWithNullValues()
        {
            var propertyResponse = new DigitalTwinPropertyResponse(888, 500, null);

            Assert.Equal(888, propertyResponse.RespondVersion);
            Assert.Equal(500, propertyResponse.StatusCode);
            Assert.Null(propertyResponse.StatusDescription);
        }

        [Fact]
        public void TestEquals()
        {
            var c1 = new DigitalTwinPropertyResponse(888, 500, "StatusDescription");
            var c2 = new DigitalTwinPropertyResponse(888, 500, "StatusDescription");

            Assert.True(c1.GetHashCode() == c2.GetHashCode());
            Assert.True(c1 == c2);
            Assert.True(c1.Equals(c2));

            var c3 = new DigitalTwinPropertyResponse(888, 500, null);
            var c4 = new DigitalTwinPropertyResponse(888, 500, null);

            Assert.True(c3.GetHashCode() == c4.GetHashCode());
            Assert.True(c3 == c4);
            Assert.True(c3.Equals(c4));
        }

        [Fact]
        public void TestNotEquals()
        {
            var c1 = new DigitalTwinPropertyResponse(888, 500, null);
            var c2 = new DigitalTwinPropertyResponse(888, 500, "TestStatusDescription");

            Assert.True(c1.GetHashCode() != c2.GetHashCode());
            Assert.True(c1 != c2);
            Assert.True(!c1.Equals(c2));

            var c3 = new DigitalTwinPropertyResponse(888, 500, "TestStatusDescription");
            var c4 = new DigitalTwinPropertyResponse(999, 500, "TestStatusDescription");

            Assert.True(c3.GetHashCode() != c4.GetHashCode());
            Assert.True(c3 != c4);
            Assert.True(!c3.Equals(c4));
        }

        [Fact]
        public void TestObjectEquals()
        {
            var c1 = new DigitalTwinPropertyResponse(888, 500, "TestStatusDescription");
            object c2 = new DigitalTwinPropertyResponse(888, 500, "TestStatusDescription");

            Assert.True(c1.Equals(c2));
        }

        [Fact]
        public void TestObjectNotEquals()
        {
            var c1 = new DigitalTwinPropertyResponse(888, 500, "TestStatusDescription");
            var c2 = new DigitalTwinPropertyResponse(999, 500, "TestStatusDescription");
            object c3 = new object();

            Assert.True(!c1.Equals(c2));
            Assert.True(!c1.Equals(c3));
        }
    }
}
