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
    Justification = "Not localizing")]

[assembly: SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "SDK hides non-actionable errors from user")]

[assembly: SuppressMessage(
    "XmlDocumentationComments",
    "RS0010: Avoid using cref tags with a prefix",
    Justification = "We have a lot of documentation pointing to external links.")]

// TODO #177 Remove localization.
[assembly: SuppressMessage(
    "Microsoft.Performance",
    "CA1824:MarkAssembliesWithNeutralResourcesLanguage.",
    Justification = "The SDKs are not localized.")]

[assembly: SuppressMessage(
    "CodeQuality",
    "IDE0079:Remove unnecessary suppression",
    Justification = "Removing this results in warnings we don't want.")]
