// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Test
{
    using System;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class UtilsTests
    {
        [TestMethod]
        public void ConvertDeliveryAckTypeFromStringValidStringPass()
        {
            Assert.AreEqual(DeliveryAcknowledgement.PositiveOnly, Utils.ConvertDeliveryAckTypeFromString("positive"));
        }

        [TestMethod]
        public void ConvertDeliveryAckTypeFromStringInvalidStringFail()
        {
            Action action = () => Utils.ConvertDeliveryAckTypeFromString("unknown");
            TestAssert.Throws<NotSupportedException>(action);
        }

        [TestMethod]
        public void ConvertDeliveryAckTypeToStringValidValuePass()
        {
            Assert.AreEqual("negative", Utils.ConvertDeliveryAckTypeToString(DeliveryAcknowledgement.NegativeOnly));
        }

        [TestMethod]
        public void ConvertDeliveryAckTypeToStringInvalidValueFail()
        {
            Action action = () => Utils.ConvertDeliveryAckTypeToString((DeliveryAcknowledgement)100500);
            TestAssert.Throws<NotSupportedException>(action);
        }
    }
}
