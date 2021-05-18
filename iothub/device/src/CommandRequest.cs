// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The data structure that represents a convention based command request.
    /// </summary>
    public sealed class CommandRequest
    {
        private readonly byte[] _data;
        private readonly PayloadConvention _payloadConvention;

        internal CommandRequest(PayloadConvention payloadConvention, string commandName, string componentName = default, byte[] data = default)
        {
            CommandName = commandName;
            ComponentName = componentName;
            _data = data;
            _payloadConvention = payloadConvention;
        }

        /// <summary>
        /// The name of the component that is command is invoked on.
        /// </summary>
        public string ComponentName { get; private set; }

        /// <summary>
        /// The command name.
        /// </summary>
        public string CommandName { get; private set; }

        /// <summary>
        /// The command request data.
        /// </summary>
        /// <typeparam name="T">The type to cast the command request data to.</typeparam>
        /// <returns>The command request data.</returns>
        public T GetData<T>()
        {
            return _payloadConvention.PayloadSerializer.DeserializeToType<T>(DataAsJson);
        }

        /// <summary>
        /// The command data in Json format.
        /// </summary>
        public string DataAsJson => (_data == null || _data.Length == 0) ? null : Encoding.UTF8.GetString(_data);
    }
}
