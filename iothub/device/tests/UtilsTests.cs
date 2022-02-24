// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
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

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MergeDictionaries_OneItemInListIsNull()
        {
            var dict1 = new Dictionary<int, string> { { 1, "One" }, { 2, "Two" }, { 3, "Three" } };

            Utils.MergeDictionaries(new Dictionary<int, string>[] { dict1, null });
        }

        [TestMethod]
        public void MergeDictionaries_TwoDictionaries_NoOverlap()
        {
            var dict1 = new Dictionary<int, string> { { 1, "One" }, { 2, "Two" }, { 3, "Three" } };
            var dict2 = new Dictionary<int, string> { { 4, "Four" }, { 5, "Five" }, { 6, "Six" } };

            var result = Utils.MergeDictionaries(new Dictionary<int, string>[] { dict1, dict2 });

            Assert.AreEqual(6, result.Count, $"Number of items in the merged dictionary should be equal to {dict1.Count + dict2.Count}");
        }

        [TestMethod]
        public void MergeDictionaries_ThreeDictionaries_NoOverlap()
        {
            var dict1 = new Dictionary<int, string> { { 1, "One" }, { 2, "Two" }, { 3, "Three" } };
            var dict2 = new Dictionary<int, string> { { 4, "Four" }, { 5, "Five" }, { 6, "Six" } };
            var dict3 = new Dictionary<int, string> { { 7, "Seven" }, { 8, "Eight" }, { 9, "Nine" } };

            var result = Utils.MergeDictionaries(new Dictionary<int, string>[] { dict1, dict2, dict3 });

            Assert.AreEqual(9, result.Count, $"Number of items in the merged dictionary should be equal to {dict1.Count + dict2.Count + dict3.Count}");
        }

        [TestMethod]
        public void MergeDictionaries_TwoDictionaries_OneOverLap_PicksFirst()
        {
            var dict1 = new Dictionary<int, string> { { 1, "One" }, { 2, "Two" }, { 3, "Three" } };
            var dict2 = new Dictionary<int, string> { { 1, "One1" }, { 4, "Four" }, { 5, "Five" } };

            var result = Utils.MergeDictionaries(new Dictionary<int, string>[] { dict1, dict2 });

            // There is one overlapping pair, we should pick the first one.
            Assert.AreEqual(5, result.Count, $"Number of items in the merged dictionary should be equal to {dict1.Count + dict2.Count - 1}");
            Assert.AreEqual(dict1[1], result[1], $"The first item in the list takes priority");
        }

        [TestMethod]
        public void MergeDictionaries_ThreeDictionaries_OneOverLap_PicksFirst()
        {
            var dict1 = new Dictionary<int, string> { { 1, "One" }, { 2, "Two" }, { 3, "Three" } };
            var dict2 = new Dictionary<int, string> { { 1, "One1" }, { 4, "Four" }, { 5, "Five" } };
            var dict3 = new Dictionary<int, string> { { 1, "One2" }, { 6, "Six" }, { 7, "Seven" } };

            var result = Utils.MergeDictionaries(new Dictionary<int, string>[] { dict1, dict2, dict3 });

            // There is one overlapping pair, we should pick the first one.
            Assert.AreEqual(7, result.Count, $"Number of items in the merged dictionary should be equal to {dict1.Count + dict2.Count + dict3.Count - 2}");
            Assert.AreEqual(dict1[1], result[1], $"The first item in the list takes priority");
        }
    }
}
