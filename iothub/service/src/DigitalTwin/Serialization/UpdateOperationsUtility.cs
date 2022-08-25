// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Azure.Devices.Extensions;
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
        /// Include an add property operation.
        /// Learn more about managing digital twins here <see href="https://docs.microsoft.com/azure/iot-pnp/howto-manage-digital-twin"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The root-level property patch should be in the following format:
        /// [
        ///     {
        ///         "op": "add",
        ///         "path": "/samplePropertyName",
        ///         "value": 20
        ///     }
        /// ]
        /// </para>
        /// <para>
        /// The component-level property patch should be in the following format:
        /// [
        ///     {
        ///         "op": "add",
        ///         "path": "/sampleComponentName/samplePropertyName",
        ///         "value": 20
        ///     }
        /// ]
        /// </para>
        /// </remarks>
        /// <param name="path">The path to the property to be added.</param>
        /// <param name="value">The value to update to.</param>
        public void AppendAddPropertyOp(string path, object value)
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
        /// Include a replace property operation.
        /// Learn more about managing digital twins here <see href="https://docs.microsoft.com/azure/iot-pnp/howto-manage-digital-twin"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The root-level property patch should be in the following format:
        /// [
        ///     {
        ///         "op": "replace",
        ///         "path": "/samplePropertyName",
        ///         "value": 20
        ///     }
        /// ]
        /// </para>
        /// <para>
        /// The component-level property patch should be in the following format:
        /// [
        ///     {
        ///         "op": "replace",
        ///         "path": "/sampleComponentName/samplePropertyName",
        ///         "value": 20
        ///     }
        /// ]
        /// </para>
        /// </remarks>
        /// <param name="path">The path to the property to be updated.</param>
        /// <param name="value">The value to update to.</param>
        public void AppendReplacePropertyOp(string path, object value)
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
        /// Learn more about managing digital twins here <see href="https://docs.microsoft.com/azure/iot-pnp/howto-manage-digital-twin"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The patch for removing a root-level property should be in the following format:
        /// [
        ///     {
        ///         "op": "remove",
        ///         "path": "/samplePropertyName"
        ///     }
        /// ]
        /// </para>
        /// <para>
        /// The patch for removing a component-level property should be in the following format:
        /// [
        ///     {
        ///         "op": "remove",
        ///         "path": "/sampleComponentName/samplePropertyName"
        ///     }
        /// ]
        /// </para>
        /// <para>
        /// The patch for removing a component should be in the following format:
        /// [
        ///     {
        ///         "op": "remove",
        ///         "path": "/sampleComponentName"
        ///     }
        /// ]
        /// </para>
        /// </remarks>
        /// <param name="path">The path to the property to be removed.</param>
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
        /// Include an add component operation.
        /// Learn more about managing digital twins here <see href="https://docs.microsoft.com/azure/iot-pnp/howto-manage-digital-twin"/>.
        /// </summary>
        /// <remarks>
        /// This utility appends the "$metadata" identifier to the property values,
        /// which helps the service identify this as a component update, and not a root-level property update.
        /// <para>
        /// The component patch should be in the following format:
        /// [
        ///     {
        ///         "op": "add",
        ///         "path": "/sampleComponentName",
        ///         "value": {
        ///             "samplePropertyName": 20,
        ///             "$metadata": {}
        ///         }
        ///     }
        /// ]
        /// </para>
        /// </remarks>
        /// <param name="path">The path to the component to be added.</param>
        /// <param name="propertyValues">The dictionary of property key values pairs to update to.</param>
        public void AppendAddComponentOp(string path, Dictionary<string, object> propertyValues)
        {
            Argument.AssertNotNull(propertyValues, nameof(propertyValues));
            propertyValues.AddComponentUpdateIdentifier();

            var op = new Dictionary<string, object>
            {
                { Op, Add },
                { Path, path },
                { Value, propertyValues },
            };
            _ops.Add(op);
        }

        /// <summary>
        /// Include a replace component operation.
        /// Learn more about managing digital twins here <see href="https://docs.microsoft.com/azure/iot-pnp/howto-manage-digital-twin"/>.
        /// </summary>
        /// <remarks>
        /// This utility appends the "$metadata" identifier to the property values,
        /// which helps the service identify this as a component update, and not a root-level property update.
        /// <para>
        /// The component patch should be in the following format:
        /// [
        ///     {
        ///         "op": "replace",
        ///         "path": "/sampleComponentName",
        ///         "value": {
        ///             "samplePropertyName": 20,
        ///             "$metadata": {}
        ///         }
        ///     }
        /// ]
        /// </para>
        /// </remarks>
        /// <param name="path">The path to the component to be updated.</param>
        /// <param name="propertyValues">The dictionary of property key values pairs to update to.</param>
        public void AppendReplaceComponentOp(string path, Dictionary<string, object> propertyValues)
        {
            Argument.AssertNotNull(propertyValues, nameof(propertyValues));
            propertyValues.AddComponentUpdateIdentifier();

            var op = new Dictionary<string, object>
            {
                { Op, Replace },
                { Path, path },
                { Value, propertyValues },
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
