// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public class WritableClientProperty
    {
        /// <summary>
        ///
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        ///
        /// </summary>
        public long Version { get; set; }

        internal PayloadConvention Convention { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public IWritablePropertyResponse AcknowledgeWith(int statusCode, string description = default)
        {
            return Convention.PayloadSerializer.CreateWritablePropertyResponse(Value, statusCode, Version, description);
        }
    }
}
