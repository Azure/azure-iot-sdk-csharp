//=======================================================================================
// Copyright © daenet GmbH Frankfurt am Main
//
// LICENSED UNDER THE APACHE LICENSE, VERSION 2.0 (THE "LICENSE"); YOU MAY NOT USE THESE
// FILES EXCEPT IN COMPLIANCE WITH THE LICENSE. YOU MAY OBTAIN A COPY OF THE LICENSE AT
// http://www.apache.org/licenses/LICENSE-2.0
// UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN WRITING, SOFTWARE DISTRIBUTED UNDER THE
// LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED. SEE THE LICENSE FOR THE SPECIFIC LANGUAGE GOVERNING
// PERMISSIONS AND LIMITATIONS UNDER THE LICENSE.
//=======================================================================================
using Microsoft.Framework.Configuration.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotHubCommander
{
    /// <summary>
    /// Extensions for CommandLineConfigurationProvider 
    /// </summary>
    public static class Extensions
    {
        public static string GetArgument(this CommandLineConfigurationProvider provider, string name, bool isMandatory = true)
        {
            string returnValue;

            if (provider.TryGet(name, out returnValue))
                return returnValue;
            else if (isMandatory)
                throw new ArgumentException($"'--{name}' command not found. In order to see more details '--help'.");
            else
                return default(String);
        }
    }
}
