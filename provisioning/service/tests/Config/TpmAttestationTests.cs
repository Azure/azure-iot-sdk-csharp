﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class TpmAttestationTests
    {
        private const string Key = "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj" +
           "2gUScTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjYO7KPVt3dyKhZS3dkcvfBisB" +
           "hP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD6l4sGBwFCnKRdln4XpM03zLpoHFao8zOwt8l/uP3qUIxmC" +
           "Yv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI6zQFOKF/rwsfBtFeWlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7" +
           "dLIVPnlgZcBhgy1SSDQMQ==";

        [TestMethod]
        public void TpmAttestationThrowsOnNullEndorsementKey()
        {
            // arrange
            string endorsementKey = null;

            // act - assert
            Action act = () => _ = new TpmAttestation(endorsementKey);
            var error = act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void TpmAttestationThrowsOnEmptyEndorsementKey()
        {
            // arrange
            string endorsementKey = "";

            // act - assert
            Action act = () => _ = new TpmAttestation(endorsementKey);
            var error = act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TpmAttestationSucceedOnNullStorageRootKey()
        {
            // arrange
            string endorsementKey = Key;
            string storageRootKey = null;

            // act - assert
            var tpmAttestation = new TpmAttestation(endorsementKey, storageRootKey);

            // assert
            Assert.AreEqual(endorsementKey, tpmAttestation.EndorsementKey);
            Assert.IsNull(tpmAttestation.StorageRootKey);
        }

        [TestMethod]
        public void TpmAttestationSucceedOnEmptyStorageRootKey()
        {
            // arrange
            string endorsementKey = Key;
            string storageRootKey = "";

            // act - assert
            var tpmAttestation = new TpmAttestation(endorsementKey, storageRootKey);

            // assert
            Assert.AreEqual(endorsementKey, tpmAttestation.EndorsementKey);
            Assert.AreEqual(storageRootKey, tpmAttestation.StorageRootKey);
        }

        [TestMethod]
        public void TpmAttestationSucceedOnValidEndorsementKey()
        {
            // arrange
            string endorsementKey = Key;

            // act
            var tpmAttestation = new TpmAttestation(endorsementKey);

            // assert
            Assert.AreEqual(endorsementKey, tpmAttestation.EndorsementKey);
            Assert.IsNull(tpmAttestation.StorageRootKey);
        }

        [TestMethod]
        public void TpmAttestationSucceedOnValidEndorsementKeyAndStorageRootKey()
        {
            // arrange
            string endorsementKey = Key;
            string storageRootKey = Key;

            // act
            var tpmAttestation = new TpmAttestation(endorsementKey, storageRootKey);

            // assert
            Assert.AreEqual(endorsementKey, tpmAttestation.EndorsementKey);
            Assert.AreEqual(storageRootKey, tpmAttestation.StorageRootKey);
        }

        [TestMethod]
        public void TpmAttestationSucceedOnSerialization()
        {
            // arrange
            string endorsementKey = Key;
            string storageRootKey = Key;
            string expectedJson =
                "{" +
                "  \"endorsementKey\":\"" + endorsementKey + "\"," +
                "  \"storageRootKey\":\"" + storageRootKey + "\"" +
                "}";
            var tpmAttestation = new TpmAttestation(endorsementKey, storageRootKey);

            // act
            string json = JsonConvert.SerializeObject(tpmAttestation);

            // assert
            TestAssert.AreEqualJson(expectedJson, json);
        }

        [TestMethod]
        public void TpmAttestationSucceedOnSerializationWithoutStorageRootKey()
        {
            // arrange
            string endorsementKey = Key;
            string expectedJson =
                "{" +
                "  \"endorsementKey\":\"" + endorsementKey + "\"" +
                "}";
            var tpmAttestation = new TpmAttestation(endorsementKey);

            // act
            string json = JsonConvert.SerializeObject(tpmAttestation);

            // assert
            TestAssert.AreEqualJson(expectedJson, json);
        }

        [TestMethod]
        public void TpmAttestationSucceedOnSerializationWithEmptyStorageRootKey()
        {
            // arrange
            string endorsementKey = Key;
            string storageRootKey = "";
            string expectedJson =
                "{" +
                "  \"endorsementKey\":\"" + endorsementKey + "\"," +
                "  \"storageRootKey\":\"" + storageRootKey + "\"" +
                "}";
            var tpmAttestation = new TpmAttestation(endorsementKey, storageRootKey);

            // act
            string json = JsonConvert.SerializeObject(tpmAttestation);

            // assert
            TestAssert.AreEqualJson(expectedJson, json);
        }

        [TestMethod]
        public void TpmAttestationSucceedOnDeserialization()
        {
            // arrange
            string endorsementKey = Key;
            string storageRootKey = Key;
            string json =
                "{" +
                "  \"endorsementKey\":\"" + endorsementKey + "\"," +
                "  \"storageRootKey\":\"" + storageRootKey + "\"" +
                "}";

            // act
            TpmAttestation tpmAttestation = JsonConvert.DeserializeObject<TpmAttestation>(json);

            // assert
            Assert.AreEqual(endorsementKey, tpmAttestation.EndorsementKey);
            Assert.AreEqual(storageRootKey, tpmAttestation.StorageRootKey);
        }

        [TestMethod]
        public void TpmAttestationThrowsOnDeserializationWithoutEndorsementKey()
        {
            // arrange
            string storageRootKey = Key;
            string json =
                "{" +
                "  \"storageRootKey\":\"" + storageRootKey + "\"" +
                "}";

            // act - assert
            Action act = () => JsonConvert.DeserializeObject<TpmAttestation>(json);
            var error = act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TpmAttestationThrowsOnDeserializationWithEmptyEndorsementKey()
        {
            // arrange
            string storageRootKey = Key;
            string json =
                "{" +
                "  \"endorsementKey\":\"\"," +
                "  \"storageRootKey\":\"" + storageRootKey + "\"" +
                "}";

            // act - assert
            Action act = () => JsonConvert.DeserializeObject<TpmAttestation>(json);
            var error = act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TpmAttestationSucceedOnDeserializationWithoutStorageRootKey()
        {
            // arrange
            string endorsementKey = Key;
            string json =
                "{" +
                "  \"endorsementKey\":\"" + endorsementKey + "\"," +
                "}";

            // act
            TpmAttestation tpmAttestation = JsonConvert.DeserializeObject<TpmAttestation>(json);

            // assert
            Assert.AreEqual(endorsementKey, tpmAttestation.EndorsementKey);
        }

        [TestMethod]
        public void TpmAttestationSucceedOnDeserializationWithEmptyStorageRootKey()
        {
            // arrange
            string endorsementKey = Key;
            string storageRootKey = "";
            string json =
                "{" +
                "  \"endorsementKey\":\"" + endorsementKey + "\"," +
                "  \"storageRootKey\":\"" + storageRootKey + "\"" +
                "}";

            // act
            TpmAttestation tpmAttestation = JsonConvert.DeserializeObject<TpmAttestation>(json);

            // assert
            Assert.AreEqual(endorsementKey, tpmAttestation.EndorsementKey);
            Assert.AreEqual(storageRootKey, tpmAttestation.StorageRootKey);
        }
    }
}
