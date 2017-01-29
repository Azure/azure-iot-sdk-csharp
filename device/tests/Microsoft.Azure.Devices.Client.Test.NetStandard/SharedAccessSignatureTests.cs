
using System;
using System.Text;
using NUnit.Framework;
using PCLCrypto;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestFixture]
    public class SharedAccessSignatureTests
    {
        [Test]
        public void Test_SharedAccessSignature_Sign_SameAsPCLCrypto()
        {
            byte[] key = Guid.NewGuid().ToByteArray();
            string value = Guid.NewGuid().ToString();

            IMacAlgorithmProvider algorithm = WinRTCrypto.MacAlgorithmProvider.OpenAlgorithm(MacAlgorithm.HmacSha256);
            CryptographicHash hash = algorithm.CreateHash(key);
            hash.Append(Encoding.UTF8.GetBytes(value));
            byte[] mac = hash.GetValueAndReset();
            string expected = Convert.ToBase64String(mac);

            Assert.That(SharedAccessSignature.Sign(key, value), Is.EqualTo(expected));
        }

        [Test]
        public void Test_SharedAccessSignatureBuilder_Sign_SameAsPCLCrypto()
        {
            string key = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            string requestString = Guid.NewGuid().ToString();

            IMacAlgorithmProvider algorithm = WinRTCrypto.MacAlgorithmProvider.OpenAlgorithm(MacAlgorithm.HmacSha256);
            CryptographicHash hash = algorithm.CreateHash(Convert.FromBase64String(key));
            hash.Append(Encoding.UTF8.GetBytes(requestString));
            byte[] mac = hash.GetValueAndReset();
            string expected = Convert.ToBase64String(mac);

            Assert.That(SharedAccessSignatureBuilder.Sign(requestString, key), Is.EqualTo(expected));
        }
    }
}