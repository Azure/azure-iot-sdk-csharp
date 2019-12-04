// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.IoT.DigitalTwin.Device.Model;
using Xunit;

namespace Azure.IoT.DigitalTwin.Device.Test.Model
{
    public class DigitalTwinPropertyUpdateTest
    {
        [Fact]
        public void TestConstructor1()
        {
            var propertyResponse = new DigitalTwinPropertyUpdate("propName", 3432, "propDesired", "propReported");

            Assert.Equal("propName", propertyResponse.PropertyName);
            Assert.Equal(3432, propertyResponse.DesiredVersion);
            Assert.Equal("propDesired", propertyResponse.PropertyDesired);
            Assert.Equal("propReported", propertyResponse.PropertyReported);
        }

        [Fact]
        public void TestConstructor2()
        {
            var propertyResponse = new DigitalTwinPropertyUpdate();

            Assert.Null(propertyResponse.PropertyName);
            Assert.Equal(0, propertyResponse.DesiredVersion);
            Assert.Null(propertyResponse.PropertyDesired);
            Assert.Null(propertyResponse.PropertyReported);
        }

        [Fact]
        public void TestConstructorWithNullValues()
        {
            var propertyResponse = new DigitalTwinPropertyUpdate("propName", 3432, null, null);

            Assert.Equal("propName", propertyResponse.PropertyName);
            Assert.Equal(3432, propertyResponse.DesiredVersion);
            Assert.Null(propertyResponse.PropertyDesired);
            Assert.Null(propertyResponse.PropertyReported);
        }

        [Fact]
        public void TestEquals()
        {
            var c1 = new DigitalTwinPropertyUpdate("propName", 3432, "propDesired", "propReported");
            var c2 = new DigitalTwinPropertyUpdate("propName", 3432, "propDesired", "propReported");

            Assert.True(c1.GetHashCode() == c2.GetHashCode());
            Assert.True(c1 == c2);
            Assert.True(c1.Equals(c2));

            var c3 = new DigitalTwinPropertyUpdate("propName", 3432, null, null);
            var c4 = new DigitalTwinPropertyUpdate("propName", 3432, null, null);

            Assert.True(c3.GetHashCode() == c4.GetHashCode());
            Assert.True(c3 == c4);
            Assert.True(c3.Equals(c4));
        }

        [Fact]
        public void TestNotEquals()
        {
            var c1 = new DigitalTwinPropertyUpdate("propName", 3432, "propDesired", "propReported");
            var c2 = new DigitalTwinPropertyUpdate("propName", 3432, "propDesired", null);

            Assert.True(c1.GetHashCode() != c2.GetHashCode());
            Assert.True(c1 != c2);
            Assert.True(!c1.Equals(c2));

            var c3 = new DigitalTwinPropertyUpdate("propName", 3432, "propDesired1", "propReported1");
            var c4 = new DigitalTwinPropertyUpdate("propName", 3432, "propDesired2", "propReported2");

            Assert.True(c3.GetHashCode() != c4.GetHashCode());
            Assert.True(c3 != c4);
            Assert.True(!c3.Equals(c4));
        }

        [Fact]
        public void TestObjectEquals()
        {
            var c1 = new DigitalTwinPropertyUpdate("propName", 3432, "propDesired1", "propReported1");
            object c2 = new DigitalTwinPropertyUpdate("propName", 3432, "propDesired1", "propReported1");

            Assert.True(c1.Equals(c2));
        }

        [Fact]
        public void TestObjectNotEquals()
        {
            var c1 = new DigitalTwinPropertyUpdate("propName", 3432, "propDesired1", "propReported1");
            var c2 = new DigitalTwinPropertyUpdate("propName", 34328, "propDesired1", "propReported1");
            object c3 = new object();

            Assert.True(!c1.Equals(c2));
            Assert.True(!c1.Equals(c3));
        }
    }
}
