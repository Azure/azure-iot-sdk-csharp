// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class TelemetryMethodsTests
    {
        [TestMethod]
        public void GetSqmMachineId_ReturnsExpectedValue()
        {
            var actualValue = TelemetryMethods.GetSqmMachineId();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string expectedValue = null;

                // Get SQM ID from Registry if exists
                var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\SQMClient");
                if (key != null)
                {
                    expectedValue = key.GetValue("MachineId") as string;
                }

                Assert.AreEqual(expectedValue, actualValue);
            }
            else
            {
                // GetSqmMachineId() should always return null for all other platforms
                Assert.IsNull(actualValue);
            }
        }
    }
}
