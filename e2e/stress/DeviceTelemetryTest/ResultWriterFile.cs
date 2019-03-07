// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class ResultWriterFile : ResultWriter
    {
        private StreamWriter _writer;
        private bool _needsHeader;
        private object _lockObject = new object();

        public ResultWriterFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                _needsHeader = true;
            }

            _writer = File.AppendText(fileName);
            _writer.AutoFlush = true;
        }

        protected override Task<bool> NeedsHeader()
        {
            return Task.FromResult(_needsHeader);
        }

        protected override Task WriteLineAsync(string s)
        {
            lock (_lockObject)
            {
                _writer.WriteLine(s);
            }

            return Task.CompletedTask;
        }
    }
}
