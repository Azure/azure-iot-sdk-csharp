// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    public class IndividualEnrollmentTests
    {
        private const string REGISTRATION_ID = "valid-registration-id";
        private const string ENDORSEMENT_KEY =
            "AToAAQALAAMAsgAgg3GXZ0SEs/gakMyNRqXXJP1S124GUgtk8qHaGzMUaaoABgCAAEMAEAgAAAAAAAEAxsj" +
            "2gUScTk1UjuioeTlfGYZrrimExB+bScH75adUMRIi2UOMxG1kw4y+9RW/IVoMl4e620VxZad0ARX2gUqVjY" +
            "O7KPVt3dyKhZS3dkcvfBisBhP1XH9B33VqHG9SHnbnQXdBUaCgKAfxome8UmBKfe+naTsE5fkvjb/do3/dD" +
            "6l4sGBwFCnKRdln4XpM03zLpoHFao8zOwt8l/uP3qUIxmCYv9A7m69Ms+5/pCkTu/rK4mRDsfhZ0QLfbzVI" +
            "6zQFOKF/rwsfBtFeWlWtcuJMKlXdD8TXWElTzgh7JS4qhFzreL0c1mI0GCj+Aws0usZh7dLIVPnlgZcBhgy" +
            "1SSDQMQ==";
        private TpmAttestation TPM_ATTESTATION = new TpmAttestation(ENDORSEMENT_KEY);

        /* SRS_DEVICE_ENROLLMENT_21_001: [The constructor shall store the provided parameters.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void IndividualEnrollment_Constructor_Succeed()
        {
            // arrange - act
            IndividualEnrollment individualEnrollment = new IndividualEnrollment(REGISTRATION_ID, TPM_ATTESTATION);

            // assert
            Assert.AreEqual(REGISTRATION_ID, individualEnrollment.RegistrationId);
            Assert.AreEqual(ENDORSEMENT_KEY, ((TpmAttestation)individualEnrollment.Attestation).EndorsementKey);
        }

        /* SRS_DEVICE_ENROLLMENT_21_002: [The constructor shall throws ArgumentException if one of the provided parameters is null.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void IndividualEnrollment_Constructor_ThrowsOnInvalidParameters()
        {
            // arrange - act - assert
            TestAssert.Throws<ArgumentException>(() => new IndividualEnrollment(null, TPM_ATTESTATION));
            TestAssert.Throws<ArgumentException>(() => new IndividualEnrollment("", TPM_ATTESTATION));
            TestAssert.Throws<ArgumentException>(() => new IndividualEnrollment("Invalid Registration Id", TPM_ATTESTATION));
            TestAssert.Throws<ArgumentException>(() => new IndividualEnrollment(REGISTRATION_ID, null));
        }

        /* SRS_DEVICE_ENROLLMENT_21_002: [The setRegistrationId shall throws ArgumentException if the provided registrationId is null, empty, or invalid.] */
    }
}
