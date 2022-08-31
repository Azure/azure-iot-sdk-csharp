// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;
using Microsoft.Azure.Devices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Api.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class ExportImportDeviceTests
    {
        [TestMethod]
        public void ExportImportDeviceTakingDeviceWithEtagWithoutQuotes()
        {
            // Arrange
            var exportimportDevice = new ExportImportDevice(new Device("device") { ETag = new ETag("MA==") }, ImportMode.Create);
            exportimportDevice.TwinETag = new ETag("MA==");

            // nothing to Act on

            // Assert
            Assert.IsTrue(exportimportDevice.ETag.ToString() == "MA==", "ETag was not set correctly");
            Assert.IsTrue(exportimportDevice.TwinETag.ToString() == "MA==", "Twin ETag was not set correctly");
        }

        [TestMethod]
        public void ExportImportDeviceTakingDeviceWithEtagWithQuotes()
        {
            // Arrange
            var exportimportDevice = new ExportImportDevice(new Device("device") { ETag = new ETag("\"MA==\"") }, ImportMode.Create);
            exportimportDevice.TwinETag = new ETag("\"MA==\"");

            // nothing to Act on

            // Assert
            Assert.IsTrue(exportimportDevice.ETag.ToString() == "\"MA==\"", "ETag was not set correctly");
            Assert.IsTrue(exportimportDevice.TwinETag.ToString() == "\"MA==\"", "Twin ETag was not set correctly");
        }

        [TestMethod]
        public void ExportImportDeviceTakingDeviceWithNullEtag()
        {
            // Arrange
            var exportimportDevice = new ExportImportDevice(new Device("device"), ImportMode.Create);

            // nothing to Act on

            // Assert
            Assert.IsTrue(exportimportDevice.ETag.ToString() == "", "ETag was not set correctly");
            Assert.IsTrue(exportimportDevice.TwinETag.ToString() == "", "Twin ETag was not set correctly");
        }

        [TestMethod]
        public void ExportImportDeviceTakingDeviceWithEmptyEtag()
        {
            // Arrange
            var exportimportDevice = new ExportImportDevice(new Device("device") { ETag = new ETag(string.Empty) }, ImportMode.Create);
            exportimportDevice.TwinETag = new ETag(string.Empty);

            // nothing to Act on

            // Assert
            Assert.IsTrue(exportimportDevice.ETag.ToString() == string.Empty, "ETag was not set correctly");
            Assert.IsTrue(exportimportDevice.TwinETag.ToString() == string.Empty, "Twin ETag was not set correctly");
        }
    }
}
