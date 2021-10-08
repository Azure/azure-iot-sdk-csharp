// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal static class ImportExportDevicesHelpers
    {
        /// <summary>
        /// Makes a stream compatible for writing to a storage blob of serialized, newline-delimited rows of the specified devices
        /// </summary>
        /// <param name="devices">The devices to serialize</param>
        public static Stream BuildDevicesStream(IReadOnlyList<ExportImportDevice> devices)
        {
            var devicesFileSb = new StringBuilder();

            foreach (ExportImportDevice device in devices)
            {
                devicesFileSb.AppendLine(JsonConvert.SerializeObject(device));
            }

            byte[] devicesFileInBytes = Encoding.Default.GetBytes(devicesFileSb.ToString());
            return new MemoryStream(devicesFileInBytes);
        }
    }
}
