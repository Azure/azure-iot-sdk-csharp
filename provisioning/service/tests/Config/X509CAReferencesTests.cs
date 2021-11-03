// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class X509CAReferencesTests
    {
        /* SRS_X509_CAREFERENCE_21_002: [The constructor shall store the primary and secondary CA references.] */

        [TestMethod]
        public void X509CAReferencesSucceedOnValidPrimaryReferences()
        {
            // arrange
            string primary = "valid-ca-reference-1";

            // act
            var x509CAReferences = new X509CAReferences(primary);

            // assert
            Assert.AreEqual(primary, x509CAReferences.Primary);
            Assert.IsNull(x509CAReferences.Secondary);
        }

        [TestMethod]
        public void X509CAReferencesSucceedOnValidPrimaryAndSecondaryReferences()
        {
            // arrange
            string primary = "valid-ca-reference-1";
            string secondary = "valid-ca-reference-1";

            // act
            var x509CAReferences = new X509CAReferences(primary, secondary);

            // assert
            Assert.AreEqual(primary, x509CAReferences.Primary);
            Assert.AreEqual(secondary, x509CAReferences.Secondary);
        }
    }
}
