using System;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class TwinCollectionTests
    {
        [TestMethod]
        public void TwinCollection_PropertyNameIncludesADot_ThrowsException()
        {
            // arrange
            string propName = "Test.Property";
            string propValue = Guid.NewGuid().ToString();
            var twinCollection = new TwinCollection();

            // act & assert
            Assert.ThrowsException<ArgumentException>(() => twinCollection[propName] = propValue);
        }

        [TestMethod]
        public void TwinCollection_PropertyNameIncludesADollarSign_ThrowsException()
        {
            // arrange
            string propName = "$TestProperty";
            string propValue = Guid.NewGuid().ToString();
            var twinCollection = new TwinCollection();

            // act & assert
            Assert.ThrowsException<ArgumentException>(() => twinCollection[propName] = propValue);
        }

        [TestMethod]
        public void TwinCollection_PropertyNameWithValidName_SavesInfo()
        {
            // arrange
            string propName = "testProperty";
            string propValue = Guid.NewGuid().ToString();
            var twinCollection = new TwinCollection {[propName] = propValue};

            // act & assert

            // assert
            Assert.AreEqual(propValue, twinCollection[propName]?.ToString(), "Twin property value should be the same.");
        }
    }
}
