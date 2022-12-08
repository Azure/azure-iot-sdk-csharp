// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        public void ExportImportDevice_DeviceWithEtag(string eTag)
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

        [TestMethod]
        public void ExportImportDevice_Ctor_DeviceNotNull()
        {
            Action act = () => _ = new ExportImportDevice((Device)null, ImportMode.Create);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void ExportImportDevice_Ctor_DeviceIdNotEmptyOrWhiteSpace()
        {
            Action act = () => _ = new ExportImportDevice(new Device(" \t\r\n"), ImportMode.Create);
            act.Should().Throw<ArgumentException>();
        }
    }
}
