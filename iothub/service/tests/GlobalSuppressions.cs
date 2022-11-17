// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Design",
    "IDE1006:Naming rule violation: Missing suffix: 'Async'",
    Justification = "We don't use Async suffix on test methods")]

[assembly: SuppressMessage(
    "Design",
    "IDE0079:Remove unnecessary suppression",
    Justification = "Removing suppression brings other unwanted warnings")]
