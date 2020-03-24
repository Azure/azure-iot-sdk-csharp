// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed class ExceptionDispatcher
    {
        public static void Throw(Exception exception)
        {
            exception.PrepareForRethrow();
            throw exception;
        }
    }
}
