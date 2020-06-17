// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    [Serializable]
    public class ThrottlingException : IotHubException
    {
        public ThrottlingException(string message)
            : base(message)
        {
        }

        public ThrottlingException(ErrorCode code, string message)
            : base(code, message)
        {
        }

        public ThrottlingException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
