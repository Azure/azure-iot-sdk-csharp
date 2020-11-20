// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    public sealed class MethodRequest
    {
        public MethodRequest(string name) : this(name, null, null, null)
        {
        }

        public MethodRequest(string name, byte[] data) : this(name, data, null, null)
        {
        }

        public MethodRequest(string name, TimeSpan? responseTimeout, TimeSpan? connectionTimeout) : this(name, null, responseTimeout, connectionTimeout)
        {
        }

        public MethodRequest(string name, byte[] data, TimeSpan? responseTimeout, TimeSpan? connectionTimeout)
        {
            Name = name;
            Data = data;
            ResponseTimeout = responseTimeout;
            ConnectionTimeout = connectionTimeout;
        }

        public string Name { get; private set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1819:Properties should not return arrays",
            Justification = "Cannot change property types on public classes.")]
        public byte[] Data { get; private set; }

        public TimeSpan? ResponseTimeout { get; private set; }

        public TimeSpan? ConnectionTimeout { get; private set; }

        public string DataAsJson => (Data == null || Data.Length == 0) ? null : Encoding.UTF8.GetString(Data);
    }
}
