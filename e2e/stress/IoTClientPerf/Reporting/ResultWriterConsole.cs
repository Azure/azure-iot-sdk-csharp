// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class ResultWriterConsole : ResultWriter
    {
        protected override Task WriteLineAsync(string s)
        {
            Console.WriteLine(s);
            return Task.CompletedTask;
        }

        public override Task FlushAsync()
        {
            return Task.CompletedTask;
        }
    }
}
