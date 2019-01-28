// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Test.ConnectionString
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Devices.Client;

    [TestClass]
    [TestCategory("Unit")]
    public class DeviceClientConnectionStringExceptionTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionStringDefaultScopeDefaultCredentialTypeMissingEndpointExceptionTest()
        {
            string connectionString = "SharedAccessKeyName=AllAccessKey;DeviceId=device1;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionStringDefaultScopeDefaultCredentialTypeMissingDeviceIdExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionStringDefaultScopeDefaultCredentialTypeMissingSharedAccessKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1;SharedAccessKeyName=AllAccessKey"; 
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionStringDefaultScopeDefaultCredentialTypeMissingSharedAccessKeyNameAndKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionStringIotHubScopeSharedAccessSignatureCredentialTypeMissingSharedAccessKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1;SharedAccessKeyName=AllAccessKey";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionStringIotHubScopeSharedAccessKeyCredentialTypeMissingSharedAccessKeyNameAndKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionStringDeviceScopeSharedAccessKeyCredentialTypeMissingSharedAccessKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;CredentialType=SharedAccessKey;DeviceId=device1";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionStringDeviceScopeSharedAccessKeyCredentialTypeNotAllowedSharedAccessKeyNameExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=device1";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void DeviceClientConnectionStringDeviceScopeSharedAccessSignatureCredentialTypeInvalidSharedAccessKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKey=INVALID;DeviceId=device1";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        public void DeviceClientMalformedConnectionStringTest()
        {
            string connectionString = "TODO: IoT Hub connection string to connect to";
            try
            {
                var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
            }
            catch (FormatException fe)
            {
                Assert.IsTrue(fe.Message.Contains("Malformed Token"), "Exception should mention 'Malformed Token' Actual :" + fe.Message);             
            }
         }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionStringDeviceScopeImplicitSharedAccessSignatureCredentialTypeInvalidSharedAccessSignatureExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessSignature=INVALID;DeviceId=device1";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeviceClientConnectionStringEmptyConnectionStringExceptionTest()
        {
            string connectionString = "";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeviceClientConnectionStringNullConnectionStringExceptionTest()
        {
            string connectionString = null;
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionStringSASKeyX509CertExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;X509Cert=true;DeviceId=device;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionStringSASSignatureX509ExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1;X509Cert=true;SharedAccessSignature=SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=poifbMLdBGtCJknubF2FW6FLn5vND5k1IKoeQ%2bONgkE%3d&se=87824124985&skn=AllAccessKey";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionStringX509CertFalseTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;X509Cert=false;DeviceId=device";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }
    }
}

