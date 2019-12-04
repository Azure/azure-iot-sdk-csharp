// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Azure.IoT.DigitalTwin.Device.Model;
using Xunit;

namespace Azure.IoT.DigitalTwin.Device.Test.Model
{
    public class DigitalTwinPropertyReportTest
    {
        [Fact]
        public void TestConstructor1()
        {
            var propReport = new DigitalTwinPropertyReport("TestPropName", "TestPropVal");

            Assert.Equal("TestPropName", propReport.Name);
            Assert.Equal("TestPropVal", propReport.Value);
        }

        [Fact]
        public void TestConstructor2()
        {
            var propReport = new DigitalTwinPropertyReport();

            Assert.Null(propReport.Name);
            Assert.Null(propReport.Value);
        }

        [Fact]
        public void TestConstructor3()
        {
            var resp = new DigitalTwinPropertyResponse();
            var propReport = new DigitalTwinPropertyReport("TestPropName", "TestPropVal", resp);

            Assert.Equal("TestPropName", propReport.Name);
            Assert.Equal("TestPropVal", propReport.Value);
            Assert.Equal(resp, propReport.DigitalTwinPropertyResponse);
        }

        [Fact]
        public void TestEquals()
        {
            var c1 = new DigitalTwinPropertyReport("TestPropName1", "TestPropVal1");
            var c2 = new DigitalTwinPropertyReport("TestPropName1", "TestPropVal1");

            Assert.True(c1 == c2 && c1.Equals(c2));
            Assert.True(c1 == c2 && c1.GetHashCode() == c2.GetHashCode());
        }

        [Fact]
        public void TestNotEquals()
        {
            var c1 = new DigitalTwinPropertyReport("TestPropName1", "TestPropVal1");
            var c2 = new DigitalTwinPropertyReport("TestPropName1", "TestPropVal2");

            Assert.True(c1 != c2 && !c1.Equals(c2));
            Assert.True(c1 != c2 && c1.GetHashCode() != c2.GetHashCode());
        }

        [Fact]
        public void TestObjectEquals()
        {
            var c1 = new DigitalTwinPropertyReport("TestPropName1", "TestPropVal1");
            object c2 = new DigitalTwinPropertyReport("TestPropName1", "TestPropVal1");

            Assert.True(c1.Equals(c2));
        }

        [Fact]
        public void TestObjectNotEquals()
        {
            var c1 = new DigitalTwinPropertyReport("TestPropName1", "TestPropVal1");
            var c2 = new DigitalTwinPropertyReport("TestPropName1", "TestPropVal2");
            object c3 = new object();

            Assert.True(!c1.Equals(c2));
            Assert.True(!c1.Equals(c3));
        }

        [Fact]
        public void TestValidateNullName()
        {
            var propReport = new DigitalTwinPropertyReport(null, "TestPropVal1");

            Exception ex = Assert.Throws<ArgumentNullException>(() => propReport.Validate());
        }

        [Fact]
        public void TestValidatePass()
        {
            var propReport = new DigitalTwinPropertyReport("TestPropName1", "TestPropVal1");
            propReport.Validate();
        }
    }
}
