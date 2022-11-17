// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Api.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class ExportImportDeviceTests
    {
        [TestMethod]
        [DataRow("MA==")]
        [DataRow("\"MA==\"")]
        [DataRow("")]
        public void ExportImportDeviceTakingDeviceWithEtag(string eTag)
        {
            // arrange
            var exportimportDevice = new ExportImportDevice(new Device("device") { ETag = new ETag(eTag) }, ImportMode.Create)
            {
                TwinETag = new ETag(eTag),
            };

            // assert
            exportimportDevice.ETag.ToString().Should().Be(eTag);
            exportimportDevice.TwinETag.ToString().Should().Be(eTag);
        }
    }
}
