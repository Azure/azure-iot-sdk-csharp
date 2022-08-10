// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root
// for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common
{
    [Serializable]
    internal class AssertionFailedException : Exception
    {
        /// <summary>
        /// Empty constructor for serialization
        /// </summary>
        public AssertionFailedException()
        {
        }

        public AssertionFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public AssertionFailedException(string description)
            : base(CommonResources.GetString(CommonResources.ShipAssertExceptionMessage, description))
        {
        }

        protected AssertionFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
