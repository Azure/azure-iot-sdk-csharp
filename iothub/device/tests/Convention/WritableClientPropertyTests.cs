// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class WritableClientPropertyTests
    {
        private const string IntPropertyName = "intPropertyName";
        private const string ObjectPropertyName = "objectPropertyName";

        private const int IntPropertyValue = 12345678;
        private static readonly CustomClientProperty s_objectPropertyValue = new CustomClientProperty { Id = 123, Name = "testName" };

        [TestMethod]
        public void WritableClientProperty_TryGetValueShouldReturnTrueIfValueCanBeDeserialized()
        {
            // arrange
            var writableClientProperty = new WritableClientProperty
            {
                PropertyName = IntPropertyName,
                Value = IntPropertyValue,
                Convention = DefaultPayloadConvention.Instance,
            };

            // act
            bool isValueRetrieved = writableClientProperty.TryGetValue(out int intPropertyValue);

            // assert
            isValueRetrieved.Should().BeTrue();
            intPropertyValue.Should().Be(IntPropertyValue);
        }

        [TestMethod]
        public void WritableClientProperty_TryGetValueShouldReturnFalseIfValueCouldNotBeDeserialized()
        {
            // arrange
            var writableClientProperty = new WritableClientProperty
            {
                PropertyName = ObjectPropertyName,
                Value = s_objectPropertyValue,
                Convention = DefaultPayloadConvention.Instance,
            };

            // act
            bool isValueRetrieved = writableClientProperty.TryGetValue(out int intPropertyValue);

            // assert
            isValueRetrieved.Should().BeFalse();
            intPropertyValue.Should().Be(default);
        }
    }
}
