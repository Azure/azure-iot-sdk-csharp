// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class TelemetryCollectionTests
    {
        [TestMethod]
        public void AddNullTelemetryNameThrows()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();

            // act
            Action testAction = () => testTelemetryCollection.Add(null, 123);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AddOrUpdateNullTelemetryNameThrows()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();

            // act
            Action testAction = () => testTelemetryCollection.AddOrUpdate(null, 123);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AddNullTelemetryValueSuccess()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();

            // act
            testTelemetryCollection.Add("abc", null);

            // assert
            testTelemetryCollection["abc"].Should().BeNull();
        }

        [TestMethod]
        public void AddOrUpdateNullTelemetryValueSuccess()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();

            // act
            testTelemetryCollection.AddOrUpdate("abc", null);

            // assert
            testTelemetryCollection["abc"].Should().BeNull();
        }

        [TestMethod]
        public void AddTelemetryValueAlreadyExistsThrows()
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
        public void AddOrUpdateTelemetryValueAlreadyExistsSuccess()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();
            testTelemetryCollection.Add("abc", 123);

            // act
            testTelemetryCollection.AddOrUpdate("abc", 1);

            // assert
            testTelemetryCollection["abc"].Should().Be(1);
        }

        [TestMethod]
        public void AddNullTelemetryCollectionThrows()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();

            // act
            Action testAction = () => testTelemetryCollection.Add(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AddOrUpdateNullTelemetryCollectionThrows()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();

            // act
            Action testAction = () => testTelemetryCollection.AddOrUpdate(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AddTelemetryCollectionAlreadyExistsThrows()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();
            testTelemetryCollection.AddOrUpdate("abc", 123);
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
        public void AddOrUpdateTelemetryCollectionAlreadyExistsSuccess()
        {
            // arrange
            var testTelemetryCollection = new TelemetryCollection();
            testTelemetryCollection.AddOrUpdate("abc", 123);
            var telemetryValues = new Dictionary<string, object>
            {
                { "qwe", 98 },
                { "abc", 2 },
            };

            // act
            testTelemetryCollection.AddOrUpdate(telemetryValues);

            // assert
            testTelemetryCollection["qwe"].Should().Be(98);
            testTelemetryCollection["abc"].Should().Be(2);
        }
    }
}
