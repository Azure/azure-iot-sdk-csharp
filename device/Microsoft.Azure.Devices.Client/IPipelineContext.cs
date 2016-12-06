// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    interface IPipelineContext
    {
        void Set<T>(T value);

        void Set<T>(string key, T value);

        T Get<T>() where T : class;

#if WINDOWS_UWP
        [Windows.Foundation.Metadata.DefaultOverload]
#endif
        T Get<T>(string key) where T : class;

        bool TryGet<T>(string key, out T value);
    }
}