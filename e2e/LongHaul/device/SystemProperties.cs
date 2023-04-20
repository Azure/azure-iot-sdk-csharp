﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.LongHaul.Device
{
    internal class SystemProperties
    {
        [JsonPropertyName("systemArchitecture")]
        public string SystemArchitecture { get; set; } = RuntimeInformation.OSArchitecture.ToString();

        [JsonPropertyName("osVersion")]
        public string OsVersion { get; set; } = RuntimeInformation.OSDescription;

        [JsonPropertyName("frameworkDescription")]
        public string FrameworkDescription { get; set; } = RuntimeInformation.FrameworkDescription;

        public override bool Equals(object obj)
        {
            return obj is SystemProperties other
                && SystemArchitecture == other.SystemArchitecture
                && OsVersion == other.OsVersion
                && FrameworkDescription == other.FrameworkDescription;
        }

        public override int GetHashCode()
        {
            return SystemArchitecture.GetHashCode() ^ OsVersion.GetHashCode() ^ FrameworkDescription.GetHashCode();
        }
    }
}
