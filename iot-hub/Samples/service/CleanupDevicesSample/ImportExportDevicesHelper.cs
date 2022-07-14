// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Azure.Devices.Samples
{
    internal static class ImportExportDevicesHelpers
    {
        private static readonly char[] s_newlines = new char[]
        {
            '\r',
            '\n',
        };

        /// <summary>
        /// Makes a stream compatible for writing to a storage blob of serialized, newline-delimited rows of the specified devices
        /// </summary>
        /// <param name="devices">The devices to serialize</param>
        internal static Stream BuildDevicesStream(IReadOnlyList<ExportImportDevice> devices)
        {
            var devicesFileSb = new StringBuilder();

            foreach (ExportImportDevice device in devices)
            {
                devicesFileSb.AppendLine(JsonConvert.SerializeObject(device));
            }

            byte[] devicesFileInBytes = Encoding.Default.GetBytes(devicesFileSb.ToString());
            return new MemoryStream(devicesFileInBytes);
        }

        /// <summary>
        /// Creates an enumerable of <see cref="ExportImportDevice"/> from the exported <see cref="Stream"/> data.
        /// </summary>
        /// <param name="inputStream">The exported <see cref="Stream"/> data.</param>
        internal static IEnumerable<ExportImportDevice> BuildExportImportDeviceFromStream(Stream inputStream)
        {
            var exportedDevices = new List<ExportImportDevice>();

            using var streamReader = new StreamReader(inputStream);
            string content = streamReader.ReadToEnd();
            string[] serializedDevices = content.Split(s_newlines, StringSplitOptions.RemoveEmptyEntries);

            foreach (string serializedDeivce in serializedDevices)
            {
                // The first line may be a comment to the user, so skip any lines that don't start with a json object initial character: curly brace
                if (serializedDeivce[0] != '{')
                {
                    continue;
                }

                var device = JsonConvert.DeserializeObject<ExportImportDevice>(serializedDeivce);
                exportedDevices.Add(device);
            }

            return exportedDevices;
        }
    }
}
