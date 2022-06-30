// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Globalization",
    "CA1303:Do not pass literals as localized parameters",
    Justification = "Not localizing",
    Scope = "module")]
[assembly: SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "SDK hides non-actionable errors from user",
    Scope = "module")]
[assembly: SuppressMessage(
    "Naming",
    "CA1707:Identifiers should not contain underscores",
    Justification = "Public API cannot be changed.",
    Scope = "type",
    Target = "~T:Microsoft.Azure.Devices.Client.ConnectionStatusChangeReason")]
[assembly: SuppressMessage(
    "Naming",
    "CA1707:Identifiers should not contain underscores",
    Justification = "Public API cannot be changed.",
    Scope = "type",
    Target = "~T:Microsoft.Azure.Devices.Client.ConnectionStatus")]
[assembly: SuppressMessage(
    "Style",
    "IDE0011:Add braces",
    Justification = "Agreement in the team to keep this style as is for logging methods only.",
    Scope = "module")]
[assembly: SuppressMessage(
    "CodeQuality",
    "IDE0079:Remove unnecessary suppression",
    Justification = "Each frameworks consider certain suppressions required by other frameworks unnecessary.",
    Scope = "module")]
