// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class TestConfiguration
    {
        public static class Storage
        {
            private static readonly Regex s_saName = new Regex("(?<=AccountName=).*?(?=;)", RegexOptions.Compiled);
            private static readonly Regex s_saKey = new Regex("(?<=AccountKey=).*?(?=;)", RegexOptions.Compiled);

            public static string ConnectionString { get; }
            public static string Name { get; }
            public static string Key { get; }

            static Storage()
            {
                ConnectionString = TestConfiguration.GetValue("STORAGE_ACCOUNT_CONNECTION_STRING");
                Name = s_saName.Match(ConnectionString).Value;
                Key = s_saKey.Match(ConnectionString).Value;
            }
        }
    }
}
