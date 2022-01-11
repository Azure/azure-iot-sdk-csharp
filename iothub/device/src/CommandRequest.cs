// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The data structure that represents a convention based command request.
    /// </summary>
    public sealed class CommandRequest
    {
        private readonly ReadOnlyCollection<byte> _payload;
        private readonly PayloadConvention _payloadConvention;

        /// <summary>
        /// Public constructor provided only for mocking purposes.
        /// </summary>
        public CommandRequest()
        {
        }

        internal CommandRequest(PayloadConvention payloadConvention, string commandName, string componentName = default, byte[] data = default)
        {
            CommandName = commandName;
            ComponentName = componentName;
            _payload = new ReadOnlyCollection<byte>(data);
            _payloadConvention = payloadConvention;
        }

        /// <summary>
        /// The name of the component that is command is invoked on.
        /// </summary>
        public string ComponentName { get; }

        /// <summary>
        /// The command name.
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// The command request data.
        /// </summary>
        /// <typeparam name="T">The type to cast the command request data to.</typeparam>
        /// <returns>The command request data.</returns>
        public T GetPayload<T>()
        {
            string dataAsJson = GetPayloadAsString();

            return dataAsJson == null
                ? default
                : _payloadConvention.PayloadSerializer.DeserializeToType<T>(dataAsJson);
        }

        /// <summary>
        /// The command request data bytes.
        /// </summary>
        /// <returns>
        /// The command request data bytes.
        /// </returns>
        public ReadOnlyCollection<byte> GetPayloadAsBytes()
        {
            // Need to return a clone of the array so that consumers
            // of this library cannot change its contents.
            return _payload;
        }

        /// <summary>
        /// The command data in Json format.
        /// </summary>
        public string GetPayloadAsString()
        {
            return _payload.Count == 0
                ? null
                : _payloadConvention.PayloadEncoder.ContentEncoding.GetString(GetPayloadAsBytes().ToArray());
        }
    }
}
