// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class ResultWriterFile : ResultWriter
    {
        private const int MaximumLinesBuffer = 2000;
        private const int FileBufferBytes = 100 * 1024 * 1024;
        private StreamWriter _writer;
        private bool _needsHeader;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private int _bufferedLines = 0;

        public ResultWriterFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                _needsHeader = true;
            }

            _writer = new StreamWriter(fileName, true, new UTF8Encoding(encoderShouldEmitUTF8Identifier:false), FileBufferBytes);
        }

        protected override Task<bool> NeedsHeader()
        {
            return Task.FromResult(_needsHeader);
        }

        protected override async Task WriteLineAsync(string s)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            await _writer.WriteLineAsync(s).ConfigureAwait(false);

            if (++_bufferedLines > MaximumLinesBuffer)
            {
                _bufferedLines = 0;
                await _writer.FlushAsync();
            }

            _semaphore.Release();
        }

        public override Task FlushAsync()
        {
            return _writer.FlushAsync();
        }
    }
}
