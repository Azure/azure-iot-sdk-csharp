// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using Microsoft.Azure.Devices.Shared;
    using System.Collections.Generic;

    class PipelineContext: IPipelineContext
    {
        readonly Dictionary<string, object> context = new Dictionary<string, object>();

        public void Set<T>(T value)
        {
            this.Set(typeof(T).Name, value);
        }

        public void Set<T>(string key, T value)
        {
            if (Logging.IsEnabled) Logging.Info(this, $"{key} = {value}");
            this.context[key] = value;
        }

        public T Get<T>() where T : class
        {
            return this.Get<T>(typeof(T).Name);
        }

        public T Get<T>(string key)
        {
            object value;
            if (this.context.TryGetValue(key, out value))
            {
                return (T)value;
            }

            return default(T);
        }

        public bool TryGet<T>(string key, out T value)
        {
            object data;
            if (this.context.TryGetValue(key, out data))
            {
                value = (T)data;
                return true;
            }
            value = default(T);
            return false;
        }
    }
}