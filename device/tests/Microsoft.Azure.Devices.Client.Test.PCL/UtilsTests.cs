// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Test.PCL
{
    using System;
    using Microsoft.Azure.Devices.Client;
    using NUnit.Framework;

    [TestFixture]
    public class UtilsTests
    {
        [Test]
        public void ConvertDeliveryAckTypeFromString_ValidString_Pass()
        {
            Assert.AreEqual(DeliveryAcknowledgement.PositiveOnly, Utils.ConvertDeliveryAckTypeFromString("positive"));
        }

        [Test]
        public void ConvertDeliveryAckTypeFromString_InvalidString_Fail()
        {
            Action action = () => Utils.ConvertDeliveryAckTypeFromString("unknown");
            action.Throws<NotSupportedException>();    
        }

        [Test]
        public void ConvertDeliveryAckTypeToString_ValidValue_Pass()
        {
            Assert.AreEqual("negative", Utils.ConvertDeliveryAckTypeToString(DeliveryAcknowledgement.NegativeOnly));
        }

        [Test]
        public void ConvertDeliveryAckTypeToString_InvalidValue_Fail()
        {
            Action action = () => Utils.ConvertDeliveryAckTypeToString((DeliveryAcknowledgement)100500);
            action.Throws<NotSupportedException>();
        }
    }
}
