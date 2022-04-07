// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System.Collections.Generic;

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
            {
                Logging.Info(this, $"{key} = {value}");
            }

            _context[key] = value;
        }

        public T Get<T>() where T : class
        {
            return Get<T>(typeof(T).Name);
        }

        public T Get<T>(string key)
        {
            if (_context.TryGetValue(key, out object value))
            {
                return (T)value;
            }

            return default;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_context.TryGetValue(key, out object data))
            {
                value = (T)data;
                return true;
            }
            value = default;
            return false;
        }
    }
}
