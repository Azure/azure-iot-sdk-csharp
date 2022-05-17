// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    internal static class FxCop
    {
        public static class Category
        {
            public const string Design = "Microsoft.Design";
            public const string Performance = "Microsoft.Performance";
            public const string ReliabilityBasic = "Reliability";
        }

        public static class Rule
        {
            public const string AvoidUncalledPrivateCode = "CA1811:AvoidUncalledPrivateCode";
            public const string DoNotCatchGeneralExceptionTypes = "CA1031:DoNotCatchGeneralExceptionTypes";
            public const string IsFatalRule = "Reliability108:IsFatalRule";
            public const string MarkMembersAsStatic = "CA1822:MarkMembersAsStatic";
            public const string TypesThatOwnDisposableFieldsShouldBeDisposable = "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable";
        }
    }
}
