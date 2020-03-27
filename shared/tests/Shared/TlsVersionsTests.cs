using System.Net;
using System.Security.Authentication;
using FluentAssertions;
using Microsoft.Azure.Devices.Shared;
using Xunit;

namespace Microsoft.Azure.Devices.Shared.Tests
{
    // By setting a collection, it causes xunit to not run in parallel, which we need because these test a static object
    [Collection("TlsVersions")]
    [Trait("TestCategory", "Unit")]
    public class TlsVersionsTests
    {
        [Fact]
        public void MinimumTlsVersions_DefaultsToTls12()
        {
            // assert
            new TlsVersions().MinimumTlsVersions.Should().Be(SslProtocols.Tls12);
        }

        [Fact]
        public void Preferred_DefaultsToNone()
        {
            // assert
            new TlsVersions().Preferred.Should().Be(SslProtocols.None);
        }

        [Fact]
        public void CheckCertificationList_DefaultsToFalse()
        {
            // assert
            new TlsVersions().CertificateRevocationCheck.Should().BeFalse();
        }

#if NET451
        [Fact]
        public void SetLegacyAcceptableVersions_Sets()
        {
            // arrange
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;

            // act
            new TlsVersions().SetLegacyAcceptableVersions();

            // assert
            ServicePointManager.SecurityProtocol.Should().Be(SecurityProtocolType.Tls12);
        }
#endif

        [Fact]
        public void SetMinimumTlsVersions_CanSetToNone()
        {
            // arrange
            var tlsVersions = new TlsVersions();

            // Need to change it to something other than none to know that it can be set back
            tlsVersions.SetMinimumTlsVersions(SslProtocols.Tls12);

            // act
            tlsVersions.SetMinimumTlsVersions(SslProtocols.None);

            // assert
            tlsVersions.MinimumTlsVersions.Should().Be(SslProtocols.Tls12);
            tlsVersions.Preferred.Should().Be(SslProtocols.None);
        }

        [Fact]
        public void SetMinimumTlsVersions_CanSetToTls12()
        {
            // arrange
            var tlsVersions = new TlsVersions();
            const SslProtocols expected = SslProtocols.Tls12;

            // act
            tlsVersions.SetMinimumTlsVersions(expected);

            // assert
            tlsVersions.MinimumTlsVersions.Should().Be(expected);
            tlsVersions.Preferred.Should().Be(expected);
        }

        [Theory]
        [InlineData(SslProtocols.Tls)]
        [InlineData(SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12)]
        public void SetMinimumTlsVersions_CanSetToOlderTls(SslProtocols protocol)
        {
            // arrange
            var tlsVersions = new TlsVersions();
            SslProtocols expected = protocol | SslProtocols.Tls12;

            // act
            tlsVersions.SetMinimumTlsVersions(protocol);

            // assert
            tlsVersions.MinimumTlsVersions.Should().Be(expected);
            tlsVersions.Preferred.Should().Be(expected);
        }

#pragma warning disable 0618

        [Theory]
        [InlineData(SslProtocols.Ssl2)]
        [InlineData(SslProtocols.Ssl3)]
        [InlineData(SslProtocols.Ssl2 | SslProtocols.Ssl3)]
        public void SetMinimumTlsVersions_CannotSetOther(SslProtocols protocol)
        {
            // arrange
            var tlsVersions = new TlsVersions();

            // act
            tlsVersions.SetMinimumTlsVersions(protocol);

            // assert
            tlsVersions.MinimumTlsVersions.Should().Be(SslProtocols.Tls12);
            tlsVersions.Preferred.Should().Be(SslProtocols.None);
        }

#pragma warning restore 0618
    }
}
