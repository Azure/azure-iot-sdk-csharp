// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    internal class PipelineContext : IPipelineContext
    {
        private readonly Dictionary<string, object> _context = new Dictionary<string, object>();

        public void Set<T>(T value)
        {
            Set(typeof(T).Name, value);
        }

        public void Set<T>(string key, T value)
        {
            if (Logging.IsEnabled)
                Logging.Info(this, $"{key} = {value}");

            _context[key] = value;
        }

        public T Get<T>() where T : class
        {
            return Get<T>(typeof(T).Name);
        }

        public T Get<T>(string key)
        {
            return _context.TryGetValue(key, out object value)
                ? (T)value
                : default;
        }

        public bool TryGet<T>(string key, out T value)
        {
            value = default;

            if (_context.TryGetValue(key, out object data))
            {
                value = (T)data;
                return true;
            }

            return false;
        }
    }
}
