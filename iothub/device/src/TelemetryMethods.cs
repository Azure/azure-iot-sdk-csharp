// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Win32;

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

                    if (Logging.IsEnabled)
                        Logging.Error(null, ex, nameof(TelemetryMethods));
                }
            }

            return null;
        }
    }
}
