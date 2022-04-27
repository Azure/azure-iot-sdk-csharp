// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class TelemetryCollectionTests
    {
        [TestMethod]
        public void TelemetryCollection_Add_NullTelemetryNameThrows()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();

            // act
            Action testAction = () => testTelemetryCollection.Add(null, 123);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void TelemetryCollection_AddOrUpdate_NullTelemetryNameThrows()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();

            // act
            Action testAction = () => testTelemetryCollection.Add(null, 123);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void TelemetryCollection_Add_NullTelemetryValueSuccess()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();

            // act
            testTelemetryCollection.Add("abc", null);

            // assert
            testTelemetryCollection["abc"].Should().BeNull();
        }

        [TestMethod]
        public void TelemetryCollection_AddOrUpdate_NullTelemetryValueSuccess()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();

            // act
            testTelemetryCollection.Add("abc", null);

            // assert
            testTelemetryCollection["abc"].Should().BeNull();
        }

        [TestMethod]
        public void TelemetryCollection_Add_TelemetryValueAlreadyExistsThrows()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();
            testTelemetryCollection.Add("abc", 123);

            // act
            Action testAction = () => testTelemetryCollection.Add("abc", 1);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TelemetryCollection_AddOrUpdate_TelemetryValueAlreadyExistsSuccess()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();
            testTelemetryCollection.Add("abc", 123);

            // act
            testTelemetryCollection.Add("abc", 1);

            // assert
            testTelemetryCollection["abc"].Should().Be(1);
        }

        [TestMethod]
        public void TelemetryCollection_Add_NullTelemetryCollectionThrows()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();

            // act
            Action testAction = () => testTelemetryCollection.Add(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void TelemetryCollection_AddOrUpdate_NullTelemetryCollectionThrows()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();

            // act
            Action testAction = () => testTelemetryCollection.Add(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void TelemetryCollection_Add_TelemetryCollectionAlreadyExistsThrows()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();
            testTelemetryCollection.Add("abc", 123);
            var telemetryValues = new Dictionary<string, object>
            {
                { "qwe", 98 },
                { "abc", 2 },
            };

            // act
            Action testAction = () => testTelemetryCollection.Add(telemetryValues);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TelemetryCollection_AddOrUpdate_TelemetryCollectionAlreadyExistsSuccess()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();
            testTelemetryCollection.Add("abc", 123);
            var telemetryValues = new Dictionary<string, object>
            {
                { "qwe", 98 },
                { "abc", 2 },
            };

            // act
            testTelemetryCollection.Add(telemetryValues);

            // assert
            testTelemetryCollection["qwe"].Should().Be(98);
            testTelemetryCollection["abc"].Should().Be(2);
        }
    }
}
