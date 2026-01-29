// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.helpers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class WindowsOnly : Attribute
    {
        public string IgnoreCriteriaMethodName { get; }

        public WindowsOnly(string ignoreCriteriaMethodName = "IgnoreIf")
        {
            IgnoreCriteriaMethodName = ignoreCriteriaMethodName;
        }

        internal bool ShouldIgnore(ITestMethod testMethod)
        {
            try
            {
                // Search for the method specified by name in this class or any parent classes.
                return !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            }
            catch (Exception e)
            {
                var message = $"Conditional ignore method {IgnoreCriteriaMethodName} not found. Ensure the method is in the same class as the test method, marked as `static`, returns a `bool`, and doesn't accept any parameters.";
                throw new ArgumentException(message, e);
            }
        }
    }
}
