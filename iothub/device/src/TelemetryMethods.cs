// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;

namespace Microsoft.Azure.Devices.Client
{
    internal static partial class TelemetryMethods
    {
        public static string GetSqmMachineId()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\SQMClient");
                    if (key != null)
                    {
                        return key.GetValue("MachineId") as string;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Assert(false, ex.Message);
                    if (Logging.IsEnabled) Logging.Error(null, ex, nameof(TelemetryMethods));
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    // get HMAC-SHA256 of the ADHS application ID, keyed by the machine ID
                    using var sr = new StreamReader("/etc/machine-id");
                    string key = sr.ReadLine();
                    if (!string.IsNullOrEmpty(key))
                    {
                        // ADHS application ID = 586e0ed989c946a794e5de337644d308
                        return GetUUIDv4("586e0ed989c946a794e5de337644d308", key);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Assert(false, ex.Message);
                    if (Logging.IsEnabled) Logging.Error(null, ex, nameof(TelemetryMethods));
                }
            }

            return null;
        }

        private static byte[] ToByteArray(string hexString)
        {
            byte[] retval = new byte[hexString.Length / 2];
            for (int i = 0; i < hexString.Length; i += 2)
            {
                retval[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return retval;
        }

        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                hex.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", b);
            }

            return hex.ToString();
        }

        private static string GetUUIDv4(string message, string secret)
        {
            byte[] keyByte = ToByteArray(secret);
            byte[] messageBytes = ToByteArray(message);

            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                // compute HMAC-SHA256
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);

                // discard trailing 16 bytes
                byte[] hashmessage16 = new byte[16];
                for (int i = 0; i < 16; i++)
                {
                    hashmessage16[i] = hashmessage[i];
                }

                // convert to a valid OSF v4 UUID
                // https://www.freedesktop.org/software/systemd/man/machine-id.html
                hashmessage16[6] = (byte)(((int)hashmessage16[6] & 0x0F) | 0x40);
                hashmessage16[8] = (byte)(((int)hashmessage16[8] & 0x3F) | 0x80);

                var guid = new Guid(ByteArrayToString(hashmessage16));

                return guid.ToString().ToUpperInvariant();
            }
        }
    }
}
