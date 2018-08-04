// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Api.Test
{
    using Microsoft.Azure.Devices;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Unit")]
    public class ExportImportDeviceTests
    {
        [TestMethod]
        public void ExportImportDeviceTakingDeviceWithEtagWithoutQuotes()
        {
            // Arrange
            var exportimportDevice = new ExportImportDevice(new Device("device") { ETag = "MA=="} , ImportMode.Create);
            exportimportDevice.TwinETag = "MA==";

            // nothing to Act on

            // Assert
            Assert.IsTrue(exportimportDevice.ETag == "MA==", "ETag was not set correctly");
            Assert.IsTrue(exportimportDevice.TwinETag == "MA==", "Twin ETag was not set correctly");
        }

        [TestMethod]
        public void ExportImportDeviceTakingDeviceWithEtagWithQuotes()
        {
            // Arrange
            var exportimportDevice = new ExportImportDevice(new Device("device") { ETag = "\"MA==\"" }, ImportMode.Create);
            exportimportDevice.TwinETag = "\"MA==\"";

            // nothing to Act on

            // Assert
            Assert.IsTrue(exportimportDevice.ETag == "MA==", "ETag was not set correctly");
            Assert.IsTrue(exportimportDevice.TwinETag == "MA==", "Twin ETag was not set correctly");
        }

        [TestMethod]
        public void ExportImportDeviceTakingDeviceWithEtagWithDoubleQuotes()
        {
            // Arrange
            var exportimportDevice = new ExportImportDevice(new Device("device") { ETag = "\"\"MA==\"\"" }, ImportMode.Create);
            exportimportDevice.TwinETag = "\"\"MA==\"\"";

            // nothing to Act on

            // Assert
            Assert.IsTrue(exportimportDevice.ETag == "\"MA==\"", "ETag was not set correctly");
            Assert.IsTrue(exportimportDevice.TwinETag == "\"MA==\"", "Twin ETag was not set correctly");
        }

        [TestMethod]
        public void ExportImportDeviceTakingDeviceWithEtagWithStartingDoubleQuotes()
        {
            // Arrange
            var exportimportDevice = new ExportImportDevice(new Device("device") { ETag = "\"\"MA==" }, ImportMode.Create);
            exportimportDevice.TwinETag = "\"\"MA==";

            // nothing to Act on

            // Assert
            Assert.IsTrue(exportimportDevice.ETag == "\"MA==", "ETag was not set correctly");
            Assert.IsTrue(exportimportDevice.TwinETag == "\"MA==", "Twin ETag was not set correctly");
        }

        [TestMethod]
        public void ExportImportDeviceTakingDeviceWithEtagWithTrailingDoubleQuotes()
        {
            // Arrange
            var exportimportDevice = new ExportImportDevice(new Device("device") { ETag = "MA==\"\"" }, ImportMode.Create);
            exportimportDevice.TwinETag = "MA==\"\"";

            // nothing to Act on

            // Assert
            Assert.IsTrue(exportimportDevice.ETag == "MA==\"", "ETag was not set correctly");
            Assert.IsTrue(exportimportDevice.TwinETag == "MA==\"", "Twin ETag was not set correctly");
        }

        [TestMethod]
        public void ExportImportDeviceTakingDeviceWithNullEtag()
        {
            // Arrange
            var exportimportDevice = new ExportImportDevice(new Device("device"), ImportMode.Create);

            // nothing to Act on

            // Assert
            Assert.IsTrue(exportimportDevice.ETag == null, "ETag was not set correctly");
            Assert.IsTrue(exportimportDevice.TwinETag == null, "Twin ETag was not set correctly");
        }

        [TestMethod]
        public void ExportImportDeviceTakingDeviceWithEmptyEtag()
        {
            // Arrange
            var exportimportDevice = new ExportImportDevice(new Device("device") { ETag =  string.Empty}, ImportMode.Create);
            exportimportDevice.TwinETag = string.Empty;

            // nothing to Act on

            // Assert
            Assert.IsTrue(exportimportDevice.ETag == string.Empty, "ETag was not set correctly");
            Assert.IsTrue(exportimportDevice.TwinETag == string.Empty, "Twin ETag was not set correctly");
        }

        //[TestCategory("CIT")]
        //[TestMethod]
        //public void ExportImportDeviceTakingDeviceWithEmptyEtag()
        //{
        //    // Arrange
        //    var exportimportDevice = new ExportImportDevice(new Device("device") { ETag = string.Empty }, ImportMode.Create);

        //    // nothing to Act on

        //    // Assert
        //    Assert.IsTrue(exportimportDevice.ETag == string.Empty, "ETag was not set correctly");
        //}
    }
}
