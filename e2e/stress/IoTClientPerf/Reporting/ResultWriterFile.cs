// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class ResultWriterFile : ResultWriter
    {
        private const long MaximumFileSize = (long)2 * 1024 * 1024 * 1024;
        private const int FileBufferBytes = 100 * 1024 * 1024;
        private StreamWriter _writer;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private long _fileSize = (long)MaximumFileSize + 1;
        private int _fileCount = 0;
        private string _fileName;

        public ResultWriterFile(string fileName, string header = null) : base(header)
        {
            if (File.Exists(fileName))
            {
                throw new InvalidOperationException($"Output file {fileName} already exists.");
            }

            _fileName = fileName;
        }

        protected override async Task WriteLineAsync(string s)
        {
            try
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);
                _fileSize += s.Length; // Assuming UTF8.

                if (_fileSize > MaximumFileSize)
                {
                    _fileCount++;
                    _fileSize = s.Length;
                    await RotateLogFileAsync().ConfigureAwait(false);
                    if (_header != null)
                    {
                        await _writer.WriteLineAsync(_header).ConfigureAwait(false);
                        _fileSize += _header.Length;
                    }
                }

                await _writer.WriteLineAsync(s).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override async Task FlushAsync()
        {
            try
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);
                await _writer.FlushAsync().ConfigureAwait(false); ;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private string GetFileFullPath()
        {
            if (_fileCount == 1) return _fileName;

            string dir = Path.GetDirectoryName(_fileName);
            string file = Path.GetFileNameWithoutExtension(_fileName);
            string ext = Path.GetExtension(_fileName);

            return Path.Combine(dir, $"{file}_{_fileCount}{ext}");
        }

        private async Task RotateLogFileAsync()
        {
            if (_writer != null)
            {
                await _writer.FlushAsync().ConfigureAwait(false);
                _writer.Dispose();
            }

            _writer = new StreamWriter(GetFileFullPath(), false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), FileBufferBytes);
        }
    }
}
