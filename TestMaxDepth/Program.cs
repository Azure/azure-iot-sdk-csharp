// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

namespace TestMaxDepth
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var now = DateTime.Now;
            const string jsonString = @"
{
  ""id"": ""aa"",
  ""schemaVersion"": ""1.0"",
  ""content"": {
    ""modulesContent"": {
        ""$edgeAgent"": {
            ""properties.desired"": {
                ""schemaVersion"": ""1.0""
            }
        }
    }
  }
}";

            try
            {
                JsonConvert.DeserializeObject<Configuration>(jsonString);
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine(ex.Message);
                // The reader's MaxDepth of 2 has been exceeded. Path '[0][0]', line 3, position 12.
            }
        }
    }
}
