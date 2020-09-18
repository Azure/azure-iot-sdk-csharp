// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Serialization
{
    /// <summary>
    /// A utility to create the application/json-patch+json operations payload required for update operations.
    /// </summary>
    public class UpdateOperationsUtility
    {
        private const string Op = "op";
        private const string Add = "add";
        private const string Replace = "replace";
        private const string Remove = "remove";
        private const string Path = "path";
        private const string Value = "value";

        private readonly List<Dictionary<string, object>> _ops = new List<Dictionary<string, object>>();

        /// <summary>
        /// Include an add operation.
        /// </summary>
        /// <param name="path">The path to the property to be added.</param>
        /// <param name="value">The value to update to.</param>
        public void AppendAddOp(string path, object value)
        {
            var op = new Dictionary<string, object>
            {
                { Op, Add },
                { Path, path },
                { Value, value },
            };
            _ops.Add(op);
        }

        /// <summary>
        /// Include a replace operation.
        /// </summary>
        /// <param name="path">The path to the property to be updated.</param>
        /// <param name="value">The value to update to.</param>
        public void AppendReplaceOp(string path, object value)
        {
            var op = new Dictionary<string, object>
            {
                { Op, Replace },
                { Path, path },
                { Value, value },
            };
            _ops.Add(op);
        }

        /// <summary>
        /// Include a remove operation.
        /// </summary>
        /// <param name="path">The path to the property to be added.</param>
        public void AppendRemoveOp(string path)
        {
            var op = new Dictionary<string, object>
            {
                { Op, Remove },
                { Path, path },
            };
            _ops.Add(op);
        }

        /// <summary>
        /// Serialize the constructed payload as json.
        /// </summary>
        /// <returns>A string of the json payload.</returns>
        public string Serialize()
        {
            return JsonConvert.SerializeObject(_ops);
        }
    }
}
