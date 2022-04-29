// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The data structure that represents a convention-based command request.
    /// </summary>
    public class CommandRequest
    {
        private readonly ReadOnlyCollection<byte> _payload;
        private readonly PayloadConvention _payloadConvention;

        /// <summary>
        /// For internal use only, unless used in mocking for testing.
        /// </summary>
        /// <param name="payloadConvention">The instance of the payload convention to use.</param>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="componentName">The name of the component that is command is invoked on.</param>
        /// <param name="payload">The command payload.</param>
        protected internal CommandRequest(PayloadConvention payloadConvention, string commandName, string componentName = default, byte[] payload = default)
        {
            CommandName = commandName;
            ComponentName = componentName;
            _payload = new ReadOnlyCollection<byte>(payload);
            _payloadConvention = payloadConvention;
        }

        /// <summary>
        /// The name of the component that is command is invoked on.
        /// </summary>
        public string ComponentName { get; }

        /// <summary>
        /// The name of the command.
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// The command request payload.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the command request payload to.</typeparam>
        /// <returns>The command request payload.</returns>
        public T GetPayload<T>()
        {
            string dataAsJson = GetPayloadAsString();

            return dataAsJson == null
                ? default
                : _payloadConvention.PayloadSerializer.DeserializeToType<T>(dataAsJson);
        }

        /// <summary>
        /// The command request payload bytes.
        /// </summary>
        /// <returns>
        /// The command request data payload.
        /// </returns>
        public ReadOnlyCollection<byte> GetPayloadAsBytes()
        {
            // Need to return a clone of the array so that consumers
            // of this library cannot change its contents.
            return _payload;
        }

        /// <summary>
        /// The command payload as a string.
        /// </summary>
        public string GetPayloadAsString()
        {
            return _payload.Count == 0
                ? null
                : _payloadConvention.PayloadEncoder.ContentEncoding.GetString(GetPayloadAsBytes().ToArray());
        }
    }
}
