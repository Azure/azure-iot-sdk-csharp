﻿// Copyright (c) Microsoft. All rights reserved.
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
        private readonly PayloadConvention _payloadConvention;

        internal CommandRequest(string commandName, PayloadConvention payloadConvention, string componentName = default, byte[] data = default)
        {
            Name = commandName;
            ComponentName = componentName;
            _data = data;
            _payloadConvention = payloadConvention;
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
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetData<T>()
        {
            return _payloadConvention.PayloadSerializer.DeserializeToType<T>(DataAsJson);
        }

        /// <summary>
        /// The method data in Json format.
        /// </summary>
        public string DataAsJson => (_data == null || _data.Length == 0) ? null : Encoding.UTF8.GetString(_data);
    }
}
