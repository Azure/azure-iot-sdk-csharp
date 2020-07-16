// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class TelemetryMethodsTests
    {
        [TestMethod]
        public void GetSqmMachineId_ReturnsExpectedValue()
        {
            string actualValue = TelemetryMethods.GetSqmMachineId();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string expectedValue = null;

                // Get SQM ID from Registry if exists
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\SQMClient");
                if (key != null)
                {
                    expectedValue = key.GetValue("MachineId") as string;
                }

                Assert.AreEqual(expectedValue, actualValue);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (File.Exists("/etc/machine-id"))
                {
                    // check if actualValue is a valid OSF v4 UUID
                    // https://www.freedesktop.org/software/systemd/man/machine-id.html
                    byte[] guidByte = new byte[actualValue.Length / 2];
                    for (int i = 0; i < actualValue.Length; i += 2)
                    {
                        guidByte[i / 2] = Convert.ToByte(actualValue.Substring(i, 2), 16);
                    }

                    guidByte[6] = (byte)(((int)guidByte[6] & 0x0F) | 0x40);
                    guidByte[8] = (byte)(((int)guidByte[8] & 0x3F) | 0x80);

                    Assert.IsTrue((guidByte[6] & 0xF0) == 0x40 && (guidByte[8] & 0xC0) == 0x80);
                }
                else
                {
                    Assert.IsNull(actualValue);
                }
            }
            else
            {
                Assert.IsNull(actualValue);
            }
        }
    }
}
