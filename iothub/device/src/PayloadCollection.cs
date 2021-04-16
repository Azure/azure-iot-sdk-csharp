// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public abstract class PayloadCollection
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[string key]
        {
            get => Collection[key];
            set => Collection[key] = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public IDictionary<string, object> Collection { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// 
        /// </summary>
        public IPayloadConvention Convention { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="payloadConvention"></param>
        protected PayloadCollection(IPayloadConvention payloadConvention = default)
        {
            Convention = payloadConvention ?? DefaultPayloadConvention.Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetPayloadObjectBytes()
        {
            return Convention.GetObjectBytes(Collection);
        }
    }
}
