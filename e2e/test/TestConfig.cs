// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Workers = 32, Scope = ExecutionScope.ClassLevel)]