// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public sealed class CommandRequest
    {
        private readonly byte[] _data;

        internal CommandRequest(string commandName, string componentName = default, byte[] data = default)
        {
            Name = commandName;
            ComponentName = componentName;
            _data = data;
        }

        /// <summary>
        ///
        /// </summary>
        public string ComponentName { get; private set; }

        /// <summary>
        /// The method name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The method data.
        /// </summary>
        public byte[] GetData()
        {
            // Need to return a clone of the array so that consumers
            // of this library cannot change its contents
            return (byte[])_data.Clone();
        }

        /// <summary>
        /// The method data in Json format.
        /// </summary>
        public string DataAsJson => (_data == null || _data.Length == 0) ? null : Encoding.UTF8.GetString(_data);
    }
}
