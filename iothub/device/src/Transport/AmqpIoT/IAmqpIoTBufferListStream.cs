// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal interface IAmqpIoTBufferListStream
    {
        bool CanRead { get; }
        bool CanSeek { get; }
        bool CanWrite { get; }
        long Length { get; }
        long Position { get; set; }

        void Dispose();
        void Flush();
        int Read(byte[] buffer, int offset, int count);
        long Seek(long offset, SeekOrigin origin);
        void SetLength(long value);
        void Write(byte[] buffer, int offset, int count);
    }
}