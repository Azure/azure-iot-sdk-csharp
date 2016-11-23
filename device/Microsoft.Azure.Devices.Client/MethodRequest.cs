// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Text;

    public sealed class MethodRequest
    {
#if NETMF
        public MethodRequest(string name, byte[] data)
#else
        public MethodRequest(string name, [System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArrayAttribute]byte[] data)
#endif
        {
            this.Name = name;
            this.Data = data;
        }

        public string Name { get; private set; }

        public byte[] Data { get; private set; }

#if !PCL
        public string DataAsJson
        {
            get { return Data == null ? null : Encoding.UTF8.GetString(Data); }
        }
#endif
    }
}
