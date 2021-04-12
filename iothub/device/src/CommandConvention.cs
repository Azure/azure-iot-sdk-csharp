// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public class CommandConvention
    {
        /// <summary>
        ///
        /// </summary>
        public static readonly CommandConvention Instance = new CommandConvention();

        /// <summary>
        ///
        /// </summary>
        public string ContentType { get; } = ObjectSerializer.ApplicationJson;

        /// <summary>
        ///
        /// </summary>
        public Encoding ContentEncoding { get; } = Encoding.UTF8;

        /// <summary>
        ///
        /// </summary>
        public ObjectSerializer PayloadSerializer { get; set; } = ObjectSerializer.Instance;
    }
}
