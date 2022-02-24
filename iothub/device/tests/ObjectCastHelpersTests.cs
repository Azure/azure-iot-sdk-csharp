// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ObjectCastHelpersTests
    {
        [TestMethod]
        public void CanConvertNumericTypes()
        {
            TestNumericConversion<float>(1.001d, true, 1.001f);
            TestNumericConversion<short>(123, true, 123);
            TestNumericConversion<short>(123, true, 123);
            TestNumericConversion<int>(123, true, 123);
            TestNumericConversion<long>(123, true, 123);
            TestNumericConversion<short>("someString", false, 0);
            TestNumericConversion<int>(true, false, 0);
        }

        private void TestNumericConversion<T>(object input, bool canConvertExpected, T resultExpected)
        {
            bool canConvertActual = ObjectConversionHelpers.TryCastNumericTo<T>(input, out T result);

            canConvertActual.Should().Be(canConvertExpected);
            result.Should().Be(resultExpected);
        }
    }
}
