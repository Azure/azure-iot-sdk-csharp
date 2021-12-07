// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Style",
    "IDE1006:Naming Styles",
    Justification = "Missing Async suffix on test method names. Test method names may be misleading when they have the Async suffix. Additionally, not changing test names help to maintain ADO history.",
    Scope = "module")]
[assembly: SuppressMessage(
    "CodeQuality",
    "IDE0079:Remove unnecessary suppression",
    Justification = "Each frameworks consider certain suppressions required by other frameworks unnecessary.",
    Scope = "module")]
