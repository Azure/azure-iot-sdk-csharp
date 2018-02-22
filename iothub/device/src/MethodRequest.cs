// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System.Text;

    public sealed class MethodRequest
    {
        public MethodRequest(string name, byte[] data)
        {
            this.Name = name;
            this.Data = data;
        }

        public string Name { get; private set; }

        public byte[] Data { get; private set; }

        public string DataAsJson
        {
            get { return (Data == null || Data.Length == 0) ? null : Encoding.UTF8.GetString(Data); }
        }
    }
}
