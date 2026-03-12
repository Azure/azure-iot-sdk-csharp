// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal static class ImportExportHelpers
    {
        /// <summary>
        /// Makes a stream compatible for writing to a storage blob of serialized, newline-delimited rows of the specified objects.
        /// </summary>
        /// <param name="items">The objects to serialize.</param>
        public static Stream BuildImportStream<T>(IReadOnlyList<T> items)
        {
            var itemsFileSb = new StringBuilder();

            foreach (T item in items)
            {
                itemsFileSb.AppendLine(JsonConvert.SerializeObject(item));
            }

            byte[] itemsFileInBytes = Encoding.Default.GetBytes(itemsFileSb.ToString());
            return new MemoryStream(itemsFileInBytes);
        }
    }
}
