// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public class PropertyCollection : IEnumerable<object>
    {
        private readonly string _propertyJson;
        private readonly IDictionary<string, dynamic> _propertiesList = new Dictionary<string, dynamic>();

        internal PropertyCollection()
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="propertyJson"></param>
        public PropertyCollection(string propertyJson)
        {
            _propertyJson = propertyJson;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public dynamic this[string propertyName]
        {
            get
            {
                return _propertiesList[propertyName];
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public IEnumerator<object> GetEnumerator()
        {
            foreach (object property in _propertiesList)
            {
                yield return property;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal string GetPropertyJson()
        {
            return _propertyJson;
        }

        internal void AddPropertyToCollection(string propertyKey, object propertyValue)
        {
            _propertiesList.Add(propertyKey, propertyValue);
        }
    }
}
