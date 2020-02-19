// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class UtilsTests
    {
        [TestMethod]
        public void ConvertDeliveryAckTypeFromStringValidStringPass()
        {
            Assert.AreEqual(DeliveryAcknowledgement.PositiveOnly, Utils.ConvertDeliveryAckTypeFromString("positive"));
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void ConvertDeliveryAckTypeFromStringInvalidStringFail()
        {
            Utils.ConvertDeliveryAckTypeFromString("unknown");
        }

        [TestMethod]
        public void ConvertDeliveryAckTypeToStringValidValuePass()
        {
            Assert.AreEqual("negative", Utils.ConvertDeliveryAckTypeToString(DeliveryAcknowledgement.NegativeOnly));
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void ConvertDeliveryAckTypeToStringInvalidValueFail()
        {
            Utils.ConvertDeliveryAckTypeToString((DeliveryAcknowledgement)100500);
        }
    }
}
