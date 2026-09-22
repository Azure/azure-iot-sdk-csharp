// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Marks a model property as belonging to a preview-only Device Provisioning Service API version.
    /// </summary>
    /// <remarks>
    /// Properties marked with this attribute are only serialized when a preview
    /// <see cref="ServiceVersion"/> is selected. See
    /// <see cref="JsonSerializerSettingsInitializer.GetJsonSerializerSettings(ServiceVersion)"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    internal sealed class PreviewApiOnlyAttribute : Attribute
    {
    }
}
