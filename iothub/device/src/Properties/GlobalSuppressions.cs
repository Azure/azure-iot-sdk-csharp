// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "SDK hides non-actionable errors from user",
    Scope = "module")]
[assembly: SuppressMessage(
    "Style",
    "IDE0011:Add braces",
    Justification = "Agreement in the team to keep this style as is for logging methods only.",
    Scope = "module")]
