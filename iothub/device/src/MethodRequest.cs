// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Text;

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
            this.Name = name;
            this.Data = data;
            this.ResponseTimeout = responseTimeout;
            this.ConnectionTimeout = connectionTimeout;
        }

        public string Name { get; private set; }

        public byte[] Data { get; private set; }

        public TimeSpan? ResponseTimeout { get; private set; }

        public TimeSpan? ConnectionTimeout { get; private set; }

        public string DataAsJson
        {
            get { return (Data == null || Data.Length == 0) ? null : Encoding.UTF8.GetString(Data); }
        }
    }
}
