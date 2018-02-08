// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Microsoft.Azure.Devices.Shared")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Microsoft")]
[assembly: AssemblyProduct("Microsoft.Azure.Devices.Shared")]
[assembly: AssemblyCopyright("Copyright ©  2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("9EE0BD29-A2E2-44D4-AAC3-7C0602517343")]

[assembly: AssemblyVersion("1.0.0.0")]

#if (!DEBUG)
[assembly: AssemblyDelaySignAttribute(true)]
[assembly: AssemblyKeyFileAttribute("35MSSharedLib1024.snk")]
#endif

// Version information for an assembly follows semantic versioning 1.0.0 (because
// NuGet didn't support semver 2.0.0 before VS 2015). See semver.org for details.
[assembly: AssemblyInformationalVersion("1.5.0-preview-001")]
